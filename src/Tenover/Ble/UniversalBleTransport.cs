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
/// Cross-platform BLE transport for the Garmin R10 using InTheHand.Bluetooth.
/// Compiles and runs on macOS, Windows, and Linux.
///
/// This class is intentionally synchronous at the call site (mirrors
/// <see cref="WindowsBleTransport"/>'s AutoConnect/Connect shape) — it's meant
/// for quick console testing. Blocking with .GetAwaiter().GetResult() is safe
/// here because a console app has no SynchronizationContext to deadlock
/// against. Do NOT use this class from a WinForms/WPF UI thread; use
/// <see cref="UniversalBleTransportAsync"/> there instead.
///
/// Internally subscribes to GATT notifications on characteristic 6A4E2810 and
/// queues them for synchronous consumption by <see cref="Client.Poll"/>.
/// </summary>
public sealed class UniversalBleTransport : ITransport, IDisposable
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
    private UniversalBleTransport(
        BluetoothDevice device,
        GattCharacteristic char2810,
        GattCharacteristic char2820)
    {
        _device = device;
        _char2810 = char2810;
        _char2820 = char2820;
        _char2810.CharacteristicValueChanged += OnCharacteristicValueChanged;
    }

    // ── Factory methods ───────────────────────────────────────────────────────

    /// <summary>
    /// Scans and connects to the first Garmin R10 found. Tries Option 1
    /// first (no prior pairing required); if that fails and the device has
    /// genuinely never been paired to Windows, automatically opens Windows
    /// Bluetooth Settings, waits for the user to pair the device there, then
    /// connects — without needing to close or restart this app. See
    /// <see cref="ConnectWithSettingsFallbackAsync"/> for the full flow, and
    /// <see cref="ConnectViaRequestDeviceAsync"/> / <see cref="ConnectViaPairedDeviceAsync"/>
    /// if you want to force one strategy only for testing (call them
    /// directly with <c>.GetAwaiter().GetResult()</c> instead of using this
    /// method).
    /// </summary>
    /// <returns>A connected <see cref="UniversalBleTransport"/>.</returns>
    /// <exception cref="InvalidOperationException">No R10 found, pairing timed out, or connection failed.</exception>
    public static UniversalBleTransport AutoConnect()
    {
        return ConnectWithSettingsFallbackAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Connects using ONLY Option 2 (paired-devices lookup), bypassing the
    /// scan/settings-fallback flow entirely. Useful for testing that
    /// specific path in isolation — fails immediately if the device has
    /// never been paired to Windows, rather than opening Settings for you.
    /// </summary>
    /// <returns>A connected <see cref="UniversalBleTransport"/>.</returns>
    /// <exception cref="InvalidOperationException">The device is not found among Windows's already-paired devices.</exception>
    public static UniversalBleTransport ConnectPairedOnly()
    {
        return ConnectViaPairedDeviceAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Connects to a known device by BLE ID or address string.
    /// </summary>
    /// <param name="idOrAddress">The device's OS tracking identifier or address.</param>
    /// <returns>A connected <see cref="UniversalBleTransport"/>.</returns>
    public static UniversalBleTransport Connect(string idOrAddress)
    {
        return ConnectByIdAsync(idOrAddress).GetAwaiter().GetResult();
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
    /// (polling, without blocking or closing this app) for the device to
    /// appear as paired, and then connects automatically. If the device IS
    /// already paired but the connection still failed for some other reason
    /// (out of range, powered off, etc.), the original error is rethrown
    /// as-is instead of opening Settings, since pairing isn't the problem.
    /// </summary>
    /// <returns>A task that resolves to a connected <see cref="UniversalBleTransport"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// The device could not be connected, or the user did not finish pairing
    /// within <see cref="PairingWaitTimeoutMs"/>.
    /// </exception>
    private static async Task<UniversalBleTransport> ConnectWithSettingsFallbackAsync()
    {
        InvalidOperationException primaryFailure;
        try
        {
            return await ConnectViaRequestDeviceAsync();
        }
        catch (InvalidOperationException ex)
        {
            primaryFailure = ex;
        }

        Console.Error.WriteLine("  initial connect failed — checking whether the device has ever been paired...");

        var pairedDevices = await Bluetooth.GetPairedDevicesAsync();
        bool alreadyPaired = pairedDevices.Any(d => d.Name == R10DeviceName);
        if (alreadyPaired)
        {
            // The device is already known to Windows, so pairing isn't the
            // issue — surface the original failure instead of opening
            // Settings, which wouldn't help here.
            throw primaryFailure;
        }

        Console.Error.WriteLine("  this device has not been connected to Windows before.");
        Console.Error.WriteLine("  opening Windows Bluetooth settings so you can pair it — this app will keep running...");
        OpenWindowsBluetoothSettings();

        var pairedDevice = await WaitForPairingAsync(TimeSpan.FromMilliseconds(PairingWaitTimeoutMs));
        if (pairedDevice is null)
        {
            throw new InvalidOperationException(
                $"Timed out after {PairingWaitTimeoutMs / 1000} s waiting for the " +
                $"Garmin R10 to be paired in Windows Settings. Pair it there, then " +
                "call AutoConnect() again.",
                primaryFailure);
        }

        Console.Error.WriteLine("  device paired — connecting...");
        return await ConnectToDeviceAsync(pairedDevice);
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
            Console.Error.WriteLine($"  could not open Bluetooth settings automatically: {ex.Message}");
            Console.Error.WriteLine("  open Settings → Bluetooth & devices manually to pair the R10.");
        }
    }

    /// <summary>
    /// Polls the paired-devices list until the Garmin R10 shows up as
    /// paired, or the given timeout elapses. Uses non-blocking delays, so it
    /// does not freeze the calling thread while waiting.
    /// </summary>
    /// <param name="timeout">How long to keep polling before giving up.</param>
    /// <returns>The paired device once found, or <c>null</c> if the timeout elapsed first.</returns>
    private static async Task<BluetoothDevice?> WaitForPairingAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            var pairedDevices = await Bluetooth.GetPairedDevicesAsync();
            var match = pairedDevices.FirstOrDefault(d => d.Name == R10DeviceName);
            if (match is not null)
            {
                return match;
            }

            await Task.Delay(PairingPollIntervalMs);
        }

        return null;
    }

    /// <summary>
    /// OPTION 1: scans for the device and connects without requiring any
    /// prior OS-level pairing. Works the first time a device is ever seen.
    /// </summary>
    /// <returns>A task that resolves to a connected <see cref="UniversalBleTransport"/>.</returns>
    private static async Task<UniversalBleTransport> ConnectViaRequestDeviceAsync()
    {
        Console.Error.WriteLine("  starting BLE scan (up to 10 s)...");

        using var cts = new CancellationTokenSource(ScanTimeoutMs);

        var filter = new RequestDeviceOptions
        {
            Filters = { new BluetoothLEScanFilter { Name = R10DeviceName } }
        };

        BluetoothDevice? device;
        try
        {
            device = await Bluetooth.RequestDeviceAsync(filter).WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"No Garmin R10 found within {ScanTimeoutMs / 1000} s — " +
                "make sure the device is powered on and advertising.");
        }

        if (device is null)
        {
            throw new InvalidOperationException("No Garmin R10 chosen or discovered.");
        }

        Console.Error.WriteLine($"  found device: {device.Name} ({device.Id})");
        return await ConnectToDeviceAsync(device);
    }

    /// <summary>
    /// OPTION 2: looks the device up among devices Windows already has a
    /// bond with, skipping the scan/picker step entirely. Fails with a clear
    /// error if the device has never been paired.
    /// </summary>
    /// <returns>A task that resolves to a connected <see cref="UniversalBleTransport"/>.</returns>
    private static async Task<UniversalBleTransport> ConnectViaPairedDeviceAsync()
    {
        Console.Error.WriteLine("  looking for a previously paired Garmin R10...");

        var pairedDevices = await Bluetooth.GetPairedDevicesAsync();
        var device = pairedDevices.FirstOrDefault(d => d.Name == R10DeviceName);
        if (device is null)
        {
            throw new InvalidOperationException(
                $"No paired \"{R10DeviceName}\" found. Pair it first via " +
                "Windows Settings → Bluetooth → Add device, or switch " +
                "AutoConnect() to Option 1 (ConnectViaRequestDeviceAsync), " +
                "which doesn't require pre-pairing.");
        }

        Console.Error.WriteLine($"  found paired device: {device.Name} ({device.Id})");
        return await ConnectToDeviceAsync(device);
    }

    /// <summary>
    /// Resolves a device by its OS tracking identifier or address and
    /// connects to it.
    /// </summary>
    /// <param name="idOrAddress">The device's OS tracking identifier or address.</param>
    /// <returns>A task that resolves to a connected <see cref="UniversalBleTransport"/>.</returns>
    private static async Task<UniversalBleTransport> ConnectByIdAsync(string idOrAddress)
    {
        var device = await BluetoothDevice.FromIdAsync(idOrAddress);
        if (device is null)
        {
            throw new InvalidOperationException(
                $"Failed to recall BLE device with identifier: {idOrAddress}");
        }

        return await ConnectToDeviceAsync(device);
    }

    /// <summary>
    /// Connects to the given device's GATT server, discovers the required
    /// characteristics, and enables notifications.
    /// </summary>
    /// <param name="device">The device to connect to.</param>
    /// <returns>A task that resolves to a connected <see cref="UniversalBleTransport"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// The GATT connection timed out, or a required characteristic was not
    /// found.
    /// </exception>
    private static async Task<UniversalBleTransport> ConnectToDeviceAsync(BluetoothDevice device)
    {
        using var cts = new CancellationTokenSource(ConnectTimeoutMs);

        try
        {
            await device.Gatt.ConnectAsync().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"GATT connect timed out after {ConnectTimeoutMs / 1000} s — " +
                "is the R10 in range and powered on?");
        }

        Console.Error.WriteLine($"  connected: {device.Name}");

        GattCharacteristic? char2810 = null;
        GattCharacteristic? char2820 = null;

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
            throw new InvalidOperationException(
                "Characteristic 6A4E2810 not found — is the R10 in range and powered on?");
        }
        if (char2820 is null)
        {
            throw new InvalidOperationException(
                "Characteristic 6A4E2820 not found — is the R10 in range and powered on?");
        }

        // Construct the transport (and subscribe its ValueChanged handler)
        // BEFORE enabling notifications on the device. Starting notifications
        // while nothing is listening can result in the underlying platform
        // never actually wiring up the notification pipe, even though the
        // call itself reports success. Subscribing first guarantees the
        // listener is ready by the time the device starts sending data.
        var transport = new UniversalBleTransport(device, char2810, char2820);

        try
        {
            await char2810.StartNotificationsAsync();
        }
        catch
        {
            transport.Dispose();
            throw;
        }

        Console.Error.WriteLine("  subscribed to notifications");

        return transport;
    }
}