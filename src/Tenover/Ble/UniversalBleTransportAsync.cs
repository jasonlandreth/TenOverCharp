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
using System.Diagnostics;
using System.Linq;
using InTheHand.Bluetooth;

namespace Tenover.Ble;

/// <summary>
/// Cross-platform BLE transport for the Garmin R10 using universal Ble.Net APIs.
/// This file compiles and runs natively on macOS, Windows, and Linux.
///
/// Connection methods are fully async (<see cref="AutoConnectAsync"/>,
/// <see cref="ConnectAsync(string, CancellationToken)"/>) — call them with
/// <c>await</c> from UI event handlers. Do NOT call <c>.GetAwaiter().GetResult()</c>
/// on these from a WinForms/WPF UI thread; that blocks the thread the
/// underlying picker/pairing UI and SynchronizationContext continuations need,
/// which deadlocks.
/// </summary>
internal sealed class UniversalBleTransportAsync : IBleTransport
{
    /// <summary>UUID of the notify/register-write characteristic (6A4E2810).</summary>
    private static readonly Guid Char2810Uuid = new("6a4e2810-667b-11e3-949a-0800200c9a66");

    /// <summary>UUID of the data-write characteristic (6A4E2820).</summary>
    private static readonly Guid Char2820Uuid = new("6a4e2820-667b-11e3-949a-0800200c9a66");

    /// <summary>Advertised local name of the Garmin Approach R10.</summary>
    private const string R10DeviceName = "Approach R10";

    /// <summary>Maximum time, in milliseconds, to wait for the scan/picker to find a device.</summary>
    private const int ScanTimeoutMs = 10_000;

    /// <summary>Maximum time, in milliseconds, to wait for the GATT connection to complete.</summary>
    private const int ConnectTimeoutMs = 10_000;

    /// <summary>Queue of raw notification frames received from the device, drained by <see cref="Read"/>.</summary>
    private readonly ConcurrentQueue<byte[]> _notifyQueue = new();

    /// <summary>The connected BLE device.</summary>
    private readonly BluetoothDevice _device;

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
    private UniversalBleTransportAsync(
        BluetoothDevice device,
        GattCharacteristic char2810,
        GattCharacteristic char2820)
    {
        _device = device;
        _char2810 = char2810;
        _char2820 = char2820;
        _char2810.CharacteristicValueChanged += OnCharacteristicValueChanged;
    }

    // ── Factory methods (async — await these from UI event handlers) ───────────

    /// <summary>
    /// Scans and connects to the first Garmin R10 found. Tries Option 1
    /// first (no prior pairing required); if that fails and the device has
    /// genuinely never been paired to Windows, automatically opens Windows
    /// Bluetooth Settings, waits for the user to pair the device there, then
    /// connects — without needing to close or restart this app. Safe to
    /// await directly from a WinForms UI event handler; the wait for
    /// pairing uses non-blocking delays, so the UI stays responsive the
    /// whole time. See <see cref="ConnectWithSettingsFallbackAsync"/> for
    /// the full flow, and <see cref="ConnectViaRequestDeviceAsync"/> /
    /// <see cref="ConnectViaPairedDeviceAsync"/> if you want to force one
    /// strategy only for testing.
    /// </summary>
    /// <param name="ct">A token to cancel the scan/connect/pairing-wait operation.</param>
    /// <returns>A task that resolves to a connected <see cref="UniversalBleTransportAsync"/>.</returns>
    /// <exception cref="InvalidOperationException">No R10 found, pairing timed out, or connection failed.</exception>
    /// <exception cref="OperationCanceledException">The provided token was cancelled.</exception>
    public static Task<UniversalBleTransportAsync> AutoConnectAsync(CancellationToken ct = default)
    {
        return ConnectWithSettingsFallbackAsync(ct);
    }

    /// <summary>
    /// Connects using ONLY Option 2 (paired-devices lookup), bypassing the
    /// scan/settings-fallback flow entirely. Useful for testing that
    /// specific path in isolation — fails immediately if the device has
    /// never been paired to Windows, rather than opening Settings for you.
    /// Safe to await directly from a WinForms UI event handler.
    /// </summary>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task that resolves to a connected <see cref="UniversalBleTransportAsync"/>.</returns>
    /// <exception cref="InvalidOperationException">The device is not found among Windows's already-paired devices.</exception>
    public static Task<UniversalBleTransportAsync> ConnectPairedOnlyAsync(CancellationToken ct = default)
    {
        return ConnectViaPairedDeviceAsync(ct);
    }

    /// <summary>
    /// Connects to a known device by BLE ID or address string.
    /// Safe to await directly from a WinForms UI event handler.
    /// </summary>
    /// <param name="idOrAddress">The device's OS tracking identifier or address.</param>
    /// <param name="ct">A token to cancel the connect operation.</param>
    /// <returns>A task that resolves to a connected <see cref="UniversalBleTransportAsync"/>.</returns>
    public static Task<UniversalBleTransportAsync> ConnectAsync(string idOrAddress, CancellationToken ct = default)
    {
        return ConnectByIdAsync(idOrAddress, ct);
    }

    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>Unique OS tracking identifier for the connected device.</summary>
    public string DeviceAddress
    {
        get { return _device.Id; }
    }

    /// <summary>Device name as reported by BLE advertisement.</summary>
    public string DeviceName
    {
        get { return _device.Name; }
    }

    // ── ITransport ────────────────────────────────────────────────────────────
    // Read/Write stay synchronous to satisfy ITransport, and they're fast,
    // non-UI-blocking operations (queue drain / fire-and-wait GATT write) that
    // are fine to call from a background polling thread. Do not call these
    // from the UI thread on a hot loop.

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
        // WriteWithoutResponse matches your original GattWriteOption layout
        _char2820.WriteValueWithoutResponseAsync(data.ToArray())
                 .GetAwaiter().GetResult();
    }

    /// <summary>
    /// Writes data to the device on the notify/register characteristic
    /// (6A4E2810), waiting for a write response.
    /// </summary>
    /// <param name="data">The bytes to write.</param>
    public void WriteRegister(ReadOnlySpan<byte> data)
    {
        // WriteWithResponse matches your original GattWriteOption layout
        _char2810.WriteValueWithResponseAsync(data.ToArray())
                 .GetAwaiter().GetResult();
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    /// <summary>
    /// Unsubscribes from notifications and disconnects the underlying GATT
    /// connection. Safe to call more than once.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _char2810.CharacteristicValueChanged -= OnCharacteristicValueChanged;
        _device.Gatt.Disconnect();
    }

    // ── Private: notification handler ─────────────────────────────────────────

    /// <summary>
    /// Handles an incoming GATT notification by enqueueing its raw bytes for
    /// later consumption by <see cref="Read"/>.
    /// </summary>
    /// <param name="sender">The characteristic that raised the notification.</param>
    /// <param name="e">Unused event data (the library does not provide typed args here).</param>
    private void OnCharacteristicValueChanged(object? sender, EventArgs e)
    {
        if (sender is GattCharacteristic characteristic)
        {
            // Enqueue the incoming byte frame array for Client.Poll() to read
            _notifyQueue.Enqueue(characteristic.Value);
        }
    }

    // ── Private: async connection helpers ─────────────────────────────────────

    /// <summary>
    /// Maximum time, in milliseconds, to wait for the user to finish pairing
    /// the device in Windows Settings before giving up.
    /// </summary>
    private const int PairingWaitTimeoutMs = 120_000;

    /// <summary>
    /// How often, in milliseconds, to re-check whether the device has been
    /// paired while waiting for the user to finish in Windows Settings.
    /// </summary>
    private const int PairingPollIntervalMs = 2_000;

    /// <summary>
    /// Tries Option 1 (<see cref="ConnectViaRequestDeviceAsync"/>) first. If
    /// that fails and the device is not found among Windows's already-paired
    /// devices, this opens Windows Bluetooth Settings for the user, waits
    /// (via non-blocking polling, so the UI thread stays responsive) for the
    /// device to appear as paired, and then connects automatically. If the
    /// device IS already paired but the connection still failed for some
    /// other reason (out of range, powered off, etc.), the original error is
    /// rethrown as-is instead of opening Settings, since pairing isn't the
    /// problem.
    /// </summary>
    /// <param name="ct">A token to cancel the scan/connect/pairing-wait operation.</param>
    /// <returns>A task that resolves to a connected <see cref="UniversalBleTransportAsync"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// The device could not be connected, or the user did not finish pairing
    /// within <see cref="PairingWaitTimeoutMs"/>.
    /// </exception>
    private static async Task<UniversalBleTransportAsync> ConnectWithSettingsFallbackAsync(CancellationToken ct)
    {
        InvalidOperationException primaryFailure;
        try
        {
            return await ConnectViaRequestDeviceAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            primaryFailure = ex;
        }

        Console.Error.WriteLine("  Initial connect failed — checking whether the device has ever been paired...");

        var pairedDevices = await Bluetooth.GetPairedDevicesAsync();
        bool alreadyPaired = pairedDevices.Any(d => d.Name == R10DeviceName);
        if (alreadyPaired)
        {
            // The device is already known to Windows, so pairing isn't the
            // issue — surface the original failure instead of opening
            // Settings, which wouldn't help here.
            throw primaryFailure;
        }

        Console.Error.WriteLine("  This device has not been connected to Windows before.");
        Console.Error.WriteLine("  Opening Windows Bluetooth settings so you can pair it — this app will keep running...");
        OpenWindowsBluetoothSettings();

        var pairedDevice = await WaitForPairingAsync(TimeSpan.FromMilliseconds(PairingWaitTimeoutMs), ct);
        if (pairedDevice is null)
        {
            throw new InvalidOperationException(
                $"Timed out after {PairingWaitTimeoutMs / 1000} s waiting for the " +
                $"Garmin R10 to be paired in Windows Settings. Pair it there, then " +
                "try connecting again.",
                primaryFailure);
        }

        Console.Error.WriteLine("  Device paired — connecting...");
        return await ConnectToDeviceInstanceAsync(pairedDevice, ct);
    }

    /// <summary>
    /// Opens the Windows Settings app directly to the Bluetooth & devices
    /// page, via the <c>ms-settings:bluetooth</c> URI. Settings opens as its
    /// own separate process/window; this app is not closed or blocked.
    /// </summary>
    private static void OpenWindowsBluetoothSettings()
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:bluetooth")
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"  Could not open Bluetooth settings automatically: {ex.Message}");
            Console.Error.WriteLine("  Open Settings → Bluetooth & devices manually to pair the R10.");
        }
    }

    /// <summary>
    /// Polls the paired-devices list until the Garmin R10 shows up as
    /// paired, or the given timeout elapses. Uses non-blocking delays, so it
    /// does not freeze the UI thread while waiting.
    /// </summary>
    /// <param name="timeout">How long to keep polling before giving up.</param>
    /// <param name="ct">A token to cancel the wait early.</param>
    /// <returns>The paired device once found, or <c>null</c> if the timeout elapsed first.</returns>
    private static async Task<BluetoothDevice?> WaitForPairingAsync(TimeSpan timeout, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            var pairedDevices = await Bluetooth.GetPairedDevicesAsync();
            var match = pairedDevices.FirstOrDefault(d => d.Name == R10DeviceName);
            if (match is not null)
            {
                return match;
            }

            await Task.Delay(PairingPollIntervalMs, ct);
        }

        return null;
    }

    /// <summary>
    /// OPTION 1: scans for the device and connects without requiring any
    /// prior OS-level pairing. Works the first time a device is ever seen.
    /// </summary>
    /// <param name="ct">A token to cancel the scan.</param>
    /// <returns>A task that resolves to a connected <see cref="UniversalBleTransportAsync"/>.</returns>
    private static async Task<UniversalBleTransportAsync> ConnectViaRequestDeviceAsync(CancellationToken ct)
    {
        Console.Error.WriteLine("  Starting cross-platform BLE scan (up to 10 s)...");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ScanTimeoutMs);

        // Scan targeting the specific local name string profile of the Approach R10
        var filter = new RequestDeviceOptions
        {
            Filters = { new BluetoothLEScanFilter { Name = R10DeviceName } }
        };

        BluetoothDevice? device;
        try
        {
            // This launches a native OS picker or scans programmatically depending
            // on platform constraints. Wrapped with a timeout so an unattended or
            // unresponsive picker fails loudly instead of hanging forever.
            device = await Bluetooth.RequestDeviceAsync(filter).WaitAsync(cts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new InvalidOperationException(
                $"BLE scan timed out after {ScanTimeoutMs / 1000} s — no Garmin R10 " +
                "found or picker was not completed.");
        }

        if (device is null)
        {
            throw new InvalidOperationException("No Garmin R10 chosen or discovered.");
        }

        Console.Error.WriteLine($"  Found device: {device.Name} (ID: {device.Id})");
        return await ConnectToDeviceInstanceAsync(device, ct);
    }

    /// <summary>
    /// OPTION 2: looks the device up among devices Windows already has a
    /// bond with, skipping the scan/picker step entirely. Fails with a clear
    /// error if the device has never been paired.
    /// </summary>
    /// <param name="ct">A token to cancel the connect operation.</param>
    /// <returns>A task that resolves to a connected <see cref="UniversalBleTransportAsync"/>.</returns>
    private static async Task<UniversalBleTransportAsync> ConnectViaPairedDeviceAsync(CancellationToken ct)
    {
        Console.Error.WriteLine("  Looking for a previously paired Garmin R10...");

        var pairedDevices = await Bluetooth.GetPairedDevicesAsync();
        var device = pairedDevices.FirstOrDefault(d => d.Name == R10DeviceName);
        if (device is null)
        {
            throw new InvalidOperationException(
                $"No paired \"{R10DeviceName}\" found. Pair it first via " +
                "Windows Settings → Bluetooth → Add device, or switch " +
                "AutoConnectAsync() to Option 1 (ConnectViaRequestDeviceAsync), " +
                "which doesn't require pre-pairing.");
        }

        Console.Error.WriteLine($"  Found paired device: {device.Name} (ID: {device.Id})");
        return await ConnectToDeviceInstanceAsync(device, ct);
    }

    /// <summary>
    /// Resolves a device by its OS tracking identifier or address and
    /// connects to it.
    /// </summary>
    /// <param name="idOrAddress">The device's OS tracking identifier or address.</param>
    /// <param name="ct">A token to cancel the connect operation.</param>
    /// <returns>A task that resolves to a connected <see cref="UniversalBleTransportAsync"/>.</returns>
    private static async Task<UniversalBleTransportAsync> ConnectByIdAsync(string idOrAddress, CancellationToken ct)
    {
        var device = await BluetoothDevice.FromIdAsync(idOrAddress);
        if (device is null)
        {
            throw new InvalidOperationException($"Failed to recall BLE device with system identifier: {idOrAddress}");
        }

        return await ConnectToDeviceInstanceAsync(device, ct);
    }

    /// <summary>
    /// Connects to the given device's GATT server, discovers the required
    /// characteristics, and enables notifications.
    /// </summary>
    /// <param name="device">The device to connect to.</param>
    /// <param name="ct">A token to cancel the connect operation.</param>
    /// <returns>A task that resolves to a connected <see cref="UniversalBleTransportAsync"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// The GATT connection timed out, or a required characteristic was not
    /// found.
    /// </exception>
    private static async Task<UniversalBleTransportAsync> ConnectToDeviceInstanceAsync(BluetoothDevice device, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ConnectTimeoutMs);

        try
        {
            // Connect to the underlying GATT Server architecture natively across macOS or Windows
            await device.Gatt.ConnectAsync().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new InvalidOperationException(
                $"GATT connect timed out after {ConnectTimeoutMs / 1000} s — is the R10 in range and powered on?");
        }

        Console.Error.WriteLine($"  Connected GATT pipeline: {device.Name}");

        GattCharacteristic? char2810 = null;
        GattCharacteristic? char2820 = null;

        // Discover services on the connected device instance
        var services = await device.Gatt.GetPrimaryServicesAsync();
        foreach (var svc in services)
        {
            if (char2810 is null)
            {
                char2810 = await svc.GetCharacteristicAsync(Char2810Uuid);
            }

            if (char2820 is null)
            {
                char2820 = await svc.GetCharacteristicAsync(Char2820Uuid);
            }

            if (char2810 is not null && char2820 is not null)
            {
                break;
            }
        }

        if (char2810 is null)
        {
            throw new InvalidOperationException("MultiLink notify characteristic 6A4E2810 not found.");
        }
        if (char2820 is null)
        {
            throw new InvalidOperationException("MultiLink data write characteristic 6A4E2820 not found.");
        }

        // Construct the transport (and subscribe its CharacteristicValueChanged
        // handler) BEFORE starting notifications on the device. Starting
        // notifications while nothing is listening to the value-changed event
        // can result in the underlying platform never actually wiring up the
        // notification pipe, even though the call itself reports success.
        // Attaching the listener first guarantees it's ready by the time the
        // device starts sending data.
        var transport = new UniversalBleTransportAsync(device, char2810, char2820);

        // Enable NOTIFY stream packets universally
        try
        {
            await char2810.StartNotificationsAsync();
        }
        catch
        {
            transport.Dispose();
            throw;
        }
        Console.Error.WriteLine("  Successfully registered cross-platform characteristic listeners!");

        return transport;
    }
}