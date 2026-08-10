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
using Windows.Storage.Streams;

namespace Tenover.Ble;

/// <summary>
/// Windows BLE transport for the Garmin R10 using WinRT BLE APIs.
///
/// The device must be pre-paired at the OS level (Settings → Bluetooth → Add device).
/// Internally subscribes to GATT notifications on characteristic 6A4E2810 and
/// queues them for synchronous consumption by <see cref="Client.Poll"/>.
/// </summary>
public sealed class WindowsBleTransport : ITransport, IDisposable
{
  // MultiLink BLE characteristics
  private static readonly Guid Char2810Uuid = new("6a4e2810-667b-11e3-949a-0800200c9a66");
  private static readonly Guid Char2820Uuid = new("6a4e2820-667b-11e3-949a-0800200c9a66");

  private const string R10DeviceName = "Approach R10";
  private const ushort GarminManufacturerId = 0x0087;
  private const int ScanTimeoutMs = 10_000;

  private readonly ConcurrentQueue<byte[]> _notifyQueue = new();
  private readonly BluetoothLEDevice _device;
  private readonly GattCharacteristic _char2810;  // notify + register writes
  private readonly GattCharacteristic _char2820;  // data writes
  private bool _disposed;

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
  /// Scan and connect to the first paired Garmin R10 found within 10 seconds.
  /// </summary>
  /// <exception cref="InvalidOperationException">No R10 found or connection failed.</exception>
  public static WindowsBleTransport AutoConnect()
      => ConnectAsync().GetAwaiter().GetResult();

  /// <summary>
  /// Connect to a known device by BLE address (e.g. <c>"F5:D1:88:F6:90:5D"</c>
  /// or <c>"F5D188F6905D"</c>).
  /// </summary>
  public static WindowsBleTransport Connect(string address)
      => ConnectByAddressAsync(address).GetAwaiter().GetResult();

  // ── Properties ────────────────────────────────────────────────────────────

  /// <summary>BLE address of the connected device as a 12-character hex string.</summary>
  public string DeviceAddress => _device.BluetoothAddress.ToString("X12");

  /// <summary>Device name as reported by BLE advertisement.</summary>
  public string DeviceName => _device.Name;

  // ── ITransport ────────────────────────────────────────────────────────────

  /// <inheritdoc/>
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

  /// <inheritdoc/>
  public void Write(ReadOnlySpan<byte> data)
  {
    var buf = data.ToArray().AsBuffer();
    _char2820.WriteValueAsync(buf, GattWriteOption.WriteWithoutResponse)
             .AsTask().GetAwaiter().GetResult();
  }

  /// <inheritdoc/>
  public void WriteRegister(ReadOnlySpan<byte> data)
  {
    var buf = data.ToArray().AsBuffer();
    _char2810.WriteValueAsync(buf, GattWriteOption.WriteWithResponse)
             .AsTask().GetAwaiter().GetResult();
  }

  // ── IDisposable ───────────────────────────────────────────────────────────

  public void Dispose()
  {
    if (_disposed) return;
    _disposed = true;
    _char2810.ValueChanged -= OnValueChanged;
    _device.Dispose();
  }

  // ── Private: notification handler ─────────────────────────────────────────

  private void OnValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
      => _notifyQueue.Enqueue(args.CharacteristicValue.ToArray());

  // ── Private: async connection helpers ─────────────────────────────────────

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
        tcs.TrySetResult(args.BluetoothAddress);
    };

    watcher.Start();
    var winner = await Task.WhenAny(tcs.Task, Task.Delay(ScanTimeoutMs));
    watcher.Stop();

    if (winner != tcs.Task)
      throw new InvalidOperationException(
          "No Garmin R10 found — pair the device in Windows Settings first.");

    ulong addr = await tcs.Task;
    Console.Error.WriteLine($"  found device: {addr:X12}");
    return await ConnectToAddressAsync(addr);
  }

  private static async Task<WindowsBleTransport> ConnectByAddressAsync(string address)
  {
    ulong addr = Convert.ToUInt64(address.Replace(":", ""), 16);
    return await ConnectToAddressAsync(addr);
  }

  private static async Task<WindowsBleTransport> ConnectToAddressAsync(ulong address)
  {
    var device = await BluetoothLEDevice.FromBluetoothAddressAsync(address)
        ?? throw new InvalidOperationException(
            $"Failed to connect to BLE device {address:X12}.");

    Console.Error.WriteLine($"  connected: {device.Name} ({address:X12})");

    // Discover GATT services
    var svcResult = await device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
    if (svcResult.Status != GattCommunicationStatus.Success)
      throw new InvalidOperationException(
          $"GATT service discovery failed: {svcResult.Status}");

    GattCharacteristic? char2810 = null;
    GattCharacteristic? char2820 = null;

    foreach (var svc in svcResult.Services)
    {
      if (char2810 is null)
      {
        var r = await svc.GetCharacteristicsForUuidAsync(
            Char2810Uuid, BluetoothCacheMode.Uncached);
        if (r.Status == GattCommunicationStatus.Success && r.Characteristics.Count > 0)
          char2810 = r.Characteristics[0];
      }

      if (char2820 is null)
      {
        var r = await svc.GetCharacteristicsForUuidAsync(
            Char2820Uuid, BluetoothCacheMode.Uncached);
        if (r.Status == GattCommunicationStatus.Success && r.Characteristics.Count > 0)
          char2820 = r.Characteristics[0];
      }

      if (char2810 is not null && char2820 is not null)
        break;
    }

    if (char2810 is null)
      throw new InvalidOperationException(
          "Characteristic 6A4E2810 not found — is the R10 properly paired?");
    if (char2820 is null)
      throw new InvalidOperationException(
          "Characteristic 6A4E2820 not found — is the R10 properly paired?");

    // Enable NOTIFY on 2810
    var notifyStatus = await char2810.WriteClientCharacteristicConfigurationDescriptorAsync(
        GattClientCharacteristicConfigurationDescriptorValue.Notify);
    if (notifyStatus != GattCommunicationStatus.Success)
      throw new InvalidOperationException(
          $"Failed to subscribe to BLE notifications: {notifyStatus}");

    return new WindowsBleTransport(device, char2810, char2820);
  }
}
