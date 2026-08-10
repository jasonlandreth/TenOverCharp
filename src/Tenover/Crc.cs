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

namespace Tenover;

/// <summary>
/// CRC-16/ARC (also CRC-16/LHA, CRC-IBM).
/// Nibble-based lookup implementation matching the Garmin GFDI CRC.
/// Poly 0x8005 reflected, init=0, no final XOR.
/// Standard check value for "123456789" is 0xBB3D.
/// </summary>
public static class Crc
{
  private static readonly ushort[] Table =
  [
      0x0000, 0xCC01, 0xD801, 0x1400, 0xF001, 0x3C00, 0x2800, 0xE401,
        0xA001, 0x6C00, 0x7800, 0xB401, 0x5000, 0x9C01, 0x8801, 0x4400,
    ];

  /// <summary>Compute CRC-16/ARC over <paramref name="data"/>.</summary>
  public static ushort Crc16(ReadOnlySpan<byte> data)
  {
    ushort crc = 0;
    foreach (byte b in data)
    {
      ushort tmp = (ushort)(((crc >> 4) & 0x0FFF) ^ Table[crc & 0xF] ^ Table[b & 0xF]);
      crc = (ushort)(((tmp >> 4) & 0x0FFF) ^ Table[tmp & 0xF] ^ Table[b >> 4]);
    }
    return crc;
  }
}
