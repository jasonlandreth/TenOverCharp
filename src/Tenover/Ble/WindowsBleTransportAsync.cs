// ============================================================================
// TenOver - Client library for the Garmin R10 launch monitor
// Copyright (c) 2026 Jason Landreth. All rights reserved.
// 
// Portions Copyright (c) divotmaker. All rights reserved.
// This file contains C# code ported from the '10over' Rust project
// created by divotmaker (https://github.com) to connect
// to the Garmin R10 Launch Monitor.
//
// The original Rust code is licensed under the Apache License, Version 2.0.
// You may obtain a copy of the License at: http://apache.org
// ============================================================================

using System.Collections.Concurrent;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace Tenover.Ble;

/// <summary>
/// Windows BLE transport for the Garmin R10 using WinRT BLE APIs.
///
/// The device does NOT need to be pre-paired at the OS level — if the R10
/// requires bonding, this transport pairs it automatically on first connect
/// (see <see cref="EnsurePairedAsync"/>). If it doesn't require bonding,
/// pairing is skipped entirely.
///
/// Connection methods are fully async (<see cref="AutoConnectAsync"/>,
/// <see cref="ConnectAsync(string, CancellationToken)"/>) and return real
/// awaitable Tasks — call them with <c>await</c> from UI event handlers.
/// Do NOT call <c>.GetAwaiter().GetResult()</c> on these from a
/// WinForms/WPF UI thread; that blocks the thread that
/// SynchronizationContext continuations (and any pairing prompt) need to
/// resume on, which deadlocks.
///
/// Internally subscribes to GATT notifications on characteristic 6A4E2810 and
/// queues them for synchronous consumption by <see cref="Client.Poll"/>.
/// </summary>
public sealed class WindowsBleTransportAsync : IBleTransport
{
    /// <summary>UUID of the notify/register-write characteristic (6A4E2810).</summary>
    private static readonly Guid Char2810Uuid = new("6a4e2810-667b-11e3-949a-0800200c9a66");

    /// <summary>UUID of the data-write characteristic (6A4E2820).</summary>
    private static readonly Guid Char2820Uuid = new("6a4e2820-667b-11e3-949a-0800200c9a66");

    /// <summary>Advertised local name of the Garmin Approach R10.</summary>
    private const string R10DeviceName = "Approach R10";

    /// <summary>Garmin's registered Bluetooth SIG company identifier, used as a manufacturer-data fallback match.</summary>
    private const ushort GarminManufacturerId = 0x0087;

    /// <summary>Maximum time, in milliseconds, to wait for an advertisement from the R10 during a scan.</summary>
    private const int ScanTimeoutMs = 10_000;

    /// <summary>Queue of raw notification frames received from the device, drained by <see cref="Read"/>.</summary>
    private readonly ConcurrentQueue<byte[]> _notifyQueue = new();

    /// <summary>The connected BLE device.</summary>
    private readonly BluetoothLEDevice _device;

    /// <summary>The notify/register-write characteristic (6A4E2810).</summary>
    private readonly GattCharacteristic _char2810;

    /// <summary>The data-write characteristic (6A4E2820).</summary>
    private readonly GattCharacteristic _char2820;

    /// <summary>Whether <see cref="Dispose"/> has already run, to make disposal idempotent.</summary>
    private bool _disposed;

    /// <summary>
    /// Constructs a transport around an already-connected device and its two
    /// resolved characteristics, and subscribes to notifications.
    /// </summary>
    /// <param name="device">The connected BLE device.</param>
    /// <param name="char2810">The resolved notify/register-write characteristic.</param>
    /// <param name="char2820">The resolved data-write characteristic.</param>
    private WindowsBleTransportAsync(
        BluetoothLEDevice device,
        GattCharacteristic char2810,
        GattCharacteristic char2820)
    {
        _device = device;
        _char2810 = char2810;
        _char2820 = char2820;
        _char2810.ValueChanged += OnValueChanged;
    }

    // ── Factory methods (async — await these; do not block on them) ────────────

    /// <summary>
    /// Scans and connects to the first Garmin R10 found within 10 seconds.
    /// Pairs automatically if the device requires it.
    /// </summary>
    /// <param name="ct">A token to cancel the scan/connect operation.</param>
    /// <returns>A task that resolves to a connected <see cref="WindowsBleTransportAsync"/>.</returns>
    /// <exception cref="InvalidOperationException">No R10 found, scan timed out, or connection failed.</exception>
    /// <exception cref="OperationCanceledException">The provided token was cancelled.</exception>
    public static Task<WindowsBleTransportAsync> AutoConnectAsync(CancellationToken ct = default)
    {
        return ConnectInternalAsync(ct);
    }

    /// <summary>
    /// Connects to a known device by BLE address (e.g. <c>"F5:D1:88:F6:90:5D"</c>
    /// or <c>"F5D188F6905D"</c>). Pairs automatically if the device requires it.
    /// </summary>
    /// <param name="address">The device's BLE address, with or without colon separators.</param>
    /// <param name="ct">A token to cancel the connect operation.</param>
    /// <returns>A task that resolves to a connected <see cref="WindowsBleTransportAsync"/>.</returns>
    public static Task<WindowsBleTransportAsync> ConnectAsync(string address, CancellationToken ct = default)
    {
        return ConnectByAddressAsync(address, ct);
    }

    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>BLE address of the connected device as a 12-character hex string.</summary>
    public string DeviceAddress
    {
        get { return _device.BluetoothAddress.ToString("X12"); }
    }

    /// <summary>Device name as reported by BLE advertisement.</summary>
    public string DeviceName
    {
        get { return _device.Name; }
    }

    // ── ITransport ────────────────────────────────────────────────────────────
    // Read/Write are intentionally synchronous to satisfy ITransport — call
    // these from a background polling thread, not the UI thread, in a hot loop.

    /// <summary>
    /// Dequeues one received notification frame into <paramref name="buffer"/>,
    /// if any is available.
    /// </summary>
    /// <param name="buffer">The destination buffer to copy frame bytes into.</param>
    /// <returns>The number of bytes written to <paramref name="buffer"/>, or 0 if no frame was queued.</returns>
    public int Read(Span<byte> buffer)
    {
        if (_notifyQueue.TryDequeue(out var data))
        {
            int len = Math.Min(data.Length, buffer.Length);
            data.AsSpan(0, len).CopyTo(buffer);
            return len;
        }
        return 0;
    }

    /// <summary>
    /// Writes data to the device on the data-write characteristic (6A4E2820)
    /// without waiting for a response.
    /// </summary>
    /// <param name="data">The bytes to write.</param>
    public void Write(ReadOnlySpan<byte> data)
    {
        var buf = data.ToArray().AsBuffer();
        _char2820.WriteValueAsync(buf, GattWriteOption.WriteWithoutResponse)
                 .AsTask().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Writes data to the device on the notify/register characteristic
    /// (6A4E2810), waiting for a write response.
    /// </summary>
    /// <param name="data">The bytes to write.</param>
    public void WriteRegister(ReadOnlySpan<byte> data)
    {
        var buf = data.ToArray().AsBuffer();
        _char2810.WriteValueAsync(buf, GattWriteOption.WriteWithResponse)
                 .AsTask().GetAwaiter().GetResult();
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    /// <summary>
    /// Unsubscribes from notifications and disposes the underlying BLE
    /// device. Safe to call more than once.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _char2810.ValueChanged -= OnValueChanged;
        _device.Dispose();
    }

    // ── Private: notification handler ─────────────────────────────────────────

    /// <summary>
    /// Handles an incoming GATT notification by enqueueing its raw bytes for
    /// later consumption by <see cref="Read"/>.
    /// </summary>
    /// <param name="sender">The characteristic that raised the notification.</param>
    /// <param name="args">The notification event data, including the new value.</param>
    private void OnValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
    {
        _notifyQueue.Enqueue(args.CharacteristicValue.ToArray());
    }

    // ── Private: async connection helpers ─────────────────────────────────────

    /// <summary>
    /// Scans for an advertising Garmin R10 by local name or manufacturer
    /// data, then connects to the first match found within
    /// <see cref="ScanTimeoutMs"/>.
    /// </summary>
    /// <param name="ct">A token to cancel the scan.</param>
    /// <returns>A task that resolves to a connected <see cref="WindowsBleTransportAsync"/>.</returns>
    /// <exception cref="InvalidOperationException">No R10 was found within the scan timeout.</exception>
    private static async Task<WindowsBleTransportAsync> ConnectInternalAsync(CancellationToken ct)
    {
        Console.Error.WriteLine("  starting BLE scan (up to 10 s)...");

        var tcs = new TaskCompletionSource<ulong>(TaskCreationOptions.RunContinuationsAsynchronously);
        var watcher = new BluetoothLEAdvertisementWatcher
        {
            ScanningMode = BluetoothLEScanningMode.Active
        };

        watcher.Received += (_, args) =>
        {
            bool isR10 = args.Advertisement.LocalName == R10DeviceName
                    || args.Advertisement.ManufacturerData
                             .Any(m => m.CompanyId == GarminManufacturerId);
            if (isR10)
            {
                tcs.TrySetResult(args.BluetoothAddress);
            }
        };

        using var reg = ct.Register(() => tcs.TrySetCanceled(ct));

        watcher.Start();
        var winner = await Task.WhenAny(tcs.Task, Task.Delay(ScanTimeoutMs, ct));
        watcher.Stop();

        if (winner != tcs.Task)
        {
            throw new InvalidOperationException(
                "No Garmin R10 found — make sure the device is powered on and advertising.");
        }

        ulong addr = await tcs.Task;
        Console.Error.WriteLine($"  found device: {addr:X12}");
        return await ConnectToAddressAsync(addr, ct);
    }

    /// <summary>
    /// Parses a BLE address string and connects to the device at that
    /// address.
    /// </summary>
    /// <param name="address">The device's BLE address, with or without colon separators.</param>
    /// <param name="ct">A token to cancel the connect operation.</param>
    /// <returns>A task that resolves to a connected <see cref="WindowsBleTransportAsync"/>.</returns>
    private static async Task<WindowsBleTransportAsync> ConnectByAddressAsync(string address, CancellationToken ct)
    {
        ulong addr = Convert.ToUInt64(address.Replace(":", ""), 16);
        return await ConnectToAddressAsync(addr, ct);
    }

    /// <summary>
    /// Connects to the device at the given raw BLE address, pairs it if
    /// required, discovers the required GATT characteristics, and enables
    /// notifications.
    /// </summary>
    /// <param name="address">The device's raw 48-bit BLE address.</param>
    /// <param name="ct">A token to cancel the connect operation.</param>
    /// <returns>A task that resolves to a connected <see cref="WindowsBleTransportAsync"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// The device could not be reached, pairing failed, GATT discovery
    /// failed, a required characteristic was not found, or notification
    /// subscription failed.
    /// </exception>
    private static async Task<WindowsBleTransportAsync> ConnectToAddressAsync(ulong address, CancellationToken ct)
    {
        var device = await BluetoothLEDevice.FromBluetoothAddressAsync(address);
        if (device is null)
        {
            throw new InvalidOperationException(
                $"Failed to connect to BLE device {address:X12}.");
        }

        Console.Error.WriteLine($"  connected: {device.Name} ({address:X12})");

        // Pair automatically if the device requires bonding. No-ops if the
        // device is already paired or doesn't need bonding at all.
        await EnsurePairedAsync(device);

        ct.ThrowIfCancellationRequested();

        // Discover GATT services
        var svcResult = await device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
        if (svcResult.Status != GattCommunicationStatus.Success)
        {
            throw new InvalidOperationException(
                $"GATT service discovery failed: {svcResult.Status}");
        }

        GattCharacteristic? char2810 = null;
        GattCharacteristic? char2820 = null;

        foreach (var svc in svcResult.Services)
        {
            if (char2810 is null)
            {
                var r = await svc.GetCharacteristicsForUuidAsync(
                    Char2810Uuid, BluetoothCacheMode.Uncached);
                if (r.Status == GattCommunicationStatus.Success && r.Characteristics.Count > 0)
                {
                    char2810 = r.Characteristics[0];
                }
            }

            if (char2820 is null)
            {
                var r = await svc.GetCharacteristicsForUuidAsync(
                    Char2820Uuid, BluetoothCacheMode.Uncached);
                if (r.Status == GattCommunicationStatus.Success && r.Characteristics.Count > 0)
                {
                    char2820 = r.Characteristics[0];
                }
            }

            if (char2810 is not null && char2820 is not null)
            {
                break;
            }
        }

        if (char2810 is null)
        {
            throw new InvalidOperationException(
                "Characteristic 6A4E2810 not found. If this persists, the device " +
                "may require authenticated pairing — check DeviceAddress in " +
                "Settings → Bluetooth to confirm the bond state.");
        }
        if (char2820 is null)
        {
            throw new InvalidOperationException(
                "Characteristic 6A4E2820 not found — is the R10 in range and powered on?");
        }

        // Enable NOTIFY on 2810
        var notifyStatus = await char2810.WriteClientCharacteristicConfigurationDescriptorAsync(
            GattClientCharacteristicConfigurationDescriptorValue.Notify);
        if (notifyStatus != GattCommunicationStatus.Success)
        {
            throw new InvalidOperationException(
                $"Failed to subscribe to BLE notifications: {notifyStatus}");
        }

        return new WindowsBleTransportAsync(device, char2810, char2820);
    }

    /// <summary>
    /// Ensures the device is paired at the OS level, pairing it
    /// automatically if needed.
    ///
    /// - If the device is already paired, this is a no-op.
    /// - If the device doesn't require bonding to expose its GATT services,
    ///   pairing is skipped.
    /// - If the device requires bonding, this pairs using ConfirmOnly/None,
    ///   which auto-accepts without a PIN prompt. Devices that require a
    ///   passkey or numeric comparison need a UI thread to show the prompt —
    ///   that scenario isn't handled here and will surface as a pairing
    ///   failure.
    /// </summary>
    /// <param name="device">The device to ensure is paired.</param>
    private static async Task EnsurePairedAsync(BluetoothLEDevice device)
    {
        var pairing = device.DeviceInformation.Pairing;

        if (pairing.IsPaired)
        {
            return;
        }

        if (!pairing.CanPair)
        {
            // Device doesn't support/require pairing — proceed unauthenticated.
            return;
        }

        var custom = pairing.Custom;

        void OnPairingRequested(DeviceInformationCustomPairing s, DevicePairingRequestedEventArgs args)
        {
            if (args.PairingKind == DevicePairingKinds.ConfirmOnly)
            {
                args.Accept();
            }
            // DisplayPin/ProvidePin/ConfirmPinMatch require showing/collecting
            // a PIN on a UI thread. Left unaccepted here; PairAsync below
            // will report failure for those cases.
        }

        custom.PairingRequested += OnPairingRequested;
        try
        {
            var result = await custom.PairAsync(
                DevicePairingKinds.ConfirmOnly,
                DevicePairingProtectionLevel.None);

            if (result.Status != DevicePairingResultStatus.Paired
                && result.Status != DevicePairingResultStatus.AlreadyPaired)
            {
                throw new InvalidOperationException(
                    $"Failed to pair with device: {result.Status}. " +
                    "If the device requires a PIN or numeric comparison, pair it " +
                    "manually once via Settings → Bluetooth.");
            }
        }
        finally
        {
            custom.PairingRequested -= OnPairingRequested;
        }
    }
}