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

namespace TenOver.Ble;

/// <summary>
/// Windows BLE transport for the Garmin R10 using WinRT BLE APIs.
///
/// This class is intentionally synchronous at the call site (its factory
/// methods block internally with <c>.GetAwaiter().GetResult()</c>) — it's
/// meant for quick console testing, where blocking is safe because a
/// console app has no SynchronizationContext to deadlock against. Do NOT
/// use this class from a WinForms/WPF UI thread; use
/// <see cref="WindowsBleTransportAsync"/> there instead.
///
/// The device must be pre-paired at the OS level (Settings → Bluetooth →
/// Add device). Internally subscribes to GATT notifications on
/// characteristic 6A4E2810 and queues them for synchronous consumption by
/// <see cref="Client.Poll"/>.
/// </summary>
public sealed class WindowsBleTransport : ITransport, IDisposable
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
    private WindowsBleTransport(
        BluetoothLEDevice device,
        GattCharacteristic char2810,
        GattCharacteristic char2820)
    {
        _device = device;
        _char2810 = char2810;
        _char2820 = char2820;
        _char2810.ValueChanged += OnValueChanged;
    }

    // ── Factory methods ───────────────────────────────────────────────────────

    /// <summary>
    /// Scans and connects to the first paired Garmin R10 found within 10
    /// seconds.
    /// </summary>
    /// <returns>A connected <see cref="WindowsBleTransport"/>.</returns>
    /// <exception cref="InvalidOperationException">No R10 found or connection failed.</exception>
    public static WindowsBleTransport AutoConnect()
    {
        return ConnectAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Connects to a known device by BLE address (e.g. <c>"F5:D1:88:F6:90:5D"</c>
    /// or <c>"F5D188F6905D"</c>).
    /// </summary>
    /// <param name="address">The device's BLE address, with or without colon separators.</param>
    /// <returns>A connected <see cref="WindowsBleTransport"/>.</returns>
    public static WindowsBleTransport Connect(string address)
    {
        return ConnectByAddressAsync(address).GetAwaiter().GetResult();
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
    /// <returns>A connected <see cref="WindowsBleTransport"/>.</returns>
    /// <exception cref="InvalidOperationException">No R10 was found within the scan timeout.</exception>
    private static async Task<WindowsBleTransport> ConnectAsync()
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

        watcher.Start();
        var winner = await Task.WhenAny(tcs.Task, Task.Delay(ScanTimeoutMs));
        watcher.Stop();

        if (winner != tcs.Task)
        {
            throw new InvalidOperationException(
                "No Garmin R10 found — pair the device in Windows Settings first.");
        }

        ulong addr = await tcs.Task;
        Console.Error.WriteLine($"  found device: {addr:X12}");
        return await ConnectToAddressAsync(addr);
    }

    /// <summary>
    /// Parses a BLE address string and connects to the device at that
    /// address.
    /// </summary>
    /// <param name="address">The device's BLE address, with or without colon separators.</param>
    /// <returns>A connected <see cref="WindowsBleTransport"/>.</returns>
    private static async Task<WindowsBleTransport> ConnectByAddressAsync(string address)
    {
        ulong addr = Convert.ToUInt64(address.Replace(":", ""), 16);
        return await ConnectToAddressAsync(addr);
    }

    /// <summary>
    /// Connects to the device at the given raw BLE address, discovers the
    /// required GATT characteristics, and enables notifications.
    /// </summary>
    /// <param name="address">The device's raw 48-bit BLE address.</param>
    /// <returns>A connected <see cref="WindowsBleTransport"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// The device could not be reached, GATT discovery failed, a required
    /// characteristic was not found, or notification subscription failed.
    /// </exception>
    private static async Task<WindowsBleTransport> ConnectToAddressAsync(ulong address)
    {
        var device = await BluetoothLEDevice.FromBluetoothAddressAsync(address);
        if (device is null)
        {
            throw new InvalidOperationException(
                $"Failed to connect to BLE device {address:X12}.");
        }

        Console.Error.WriteLine($"  connected: {device.Name} ({address:X12})");

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
                "Characteristic 6A4E2810 not found — is the R10 properly paired?");
        }
        if (char2820 is null)
        {
            throw new InvalidOperationException(
                "Characteristic 6A4E2820 not found — is the R10 properly paired?");
        }

        // Construct the transport (and subscribe its ValueChanged handler)
        // BEFORE enabling notifications on the device. On Windows, writing
        // the CCCD notify descriptor while nothing is listening to
        // ValueChanged can result in notifications silently never arriving,
        // even though the descriptor write itself reports success.
        var transport = new WindowsBleTransport(device, char2810, char2820);

        // Enable NOTIFY on 2810
        var notifyStatus = await char2810.WriteClientCharacteristicConfigurationDescriptorAsync(
            GattClientCharacteristicConfigurationDescriptorValue.Notify);
        if (notifyStatus != GattCommunicationStatus.Success)
        {
            transport.Dispose();
            throw new InvalidOperationException(
                $"Failed to subscribe to BLE notifications: {notifyStatus}");
        }

        return transport;
    }
}