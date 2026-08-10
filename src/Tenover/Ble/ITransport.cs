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

namespace Tenover.Ble;

/// <summary>
/// Transport abstraction — the caller provides the BLE read/write implementation.
/// The library does not depend on any specific BLE crate/package.
/// Implement this interface to bridge to any BLE library (WinRT, BlueZ, btleplug, etc.).
/// </summary>
public interface ITransport
{
  /// <summary>
  /// Read available data from the BLE notification channel.
  /// Non-blocking: returns 0 when no data is available.
  /// Returns the number of bytes written to <paramref name="buffer"/>.
  /// </summary>
  int Read(Span<byte> buffer);

  /// <summary>
  /// Write a chunk of data to the BLE write channel (characteristic 6A4E2820).
  /// <paramref name="data"/> already includes the MultiLink handle byte prefix.
  /// </summary>
  void Write(ReadOnlySpan<byte> data);

  /// <summary>
  /// Write to the MultiLink register/control channel (characteristic 6A4E2810).
  /// Used for REGISTER commands and their responses.
  /// </summary>
  void WriteRegister(ReadOnlySpan<byte> data);
}
