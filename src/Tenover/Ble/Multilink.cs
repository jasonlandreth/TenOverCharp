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

using System.Buffers.Binary;

namespace TenOver.Ble;

/// <summary>
/// MultiLink transport multiplexer.
/// The R10 uses MultiLink service 6A4E2800 instead of a dedicated GFDI service.
/// GFDI is service ID 1 within MultiLink.
/// A handle byte is prepended to every BLE write/notification chunk for routing.
/// </summary>
internal static class Multilink
{
  /// <summary>MultiLink service IDs.</summary>
  public enum ServiceId : ushort
  {
    Gfdi = 1,
    Nfc = 2,
    RealTimeHr = 6,
    Echo = 15,
    KeepAlive = 22,
  }

  /// <summary>MultiLink register status codes.</summary>
  public enum RegisterStatus : byte
  {
    Success = 0,
    InvalidServiceId = 1,
    PendingAuth = 2,
    AlreadyInUse = 3,
    Rejected = 4,
  }

  /// <summary>
  /// Build a REGISTER command for a MultiLink service.
  /// 13 bytes: [0x00][0x00][txnId: u64 LE][svcId: u16 LE][flags: u8]
  /// Written to characteristic 6A4E2810 (bidirectional control + data).
  /// </summary>
  public static byte[] BuildRegister(ulong txnId, ServiceId svcId)
  {
    var buf = new byte[13];
    // buf[0] = 0x00 (reserved), buf[1] = 0x00 (REGISTER command)
    BinaryPrimitives.WriteUInt64LittleEndian(buf.AsSpan(2), txnId);
    BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(10), (ushort)svcId);
    // buf[12] = 0x00 (unreliable mode)
    return buf;
  }

  /// <summary>
  /// Parse a REGISTER_RESPONSE notification from characteristic 6A4E2810.
  /// Returns (status, handle, flags) on success, or null if not a valid response.
  /// </summary>
  public static (RegisterStatus status, byte handle, byte flags)? ParseRegisterResponse(
      ReadOnlySpan<byte> data)
  {
    // Minimum 15 bytes: [reserved][0x01][txnId 8B][svcId 2B][status][handle][flags]
    if (data.Length < 15 || data[1] != 0x01)
      return null;
    if (!Enum.IsDefined(typeof(RegisterStatus), data[12]))
      return null;
    return ((RegisterStatus)data[12], data[13], data[14]);
  }

  /// <summary>
  /// Strip the handle byte from a BLE notification chunk.
  /// Returns true and sets <paramref name="stripped"/> if the handle matches.
  /// </summary>
  public static bool TryStripHandle(
      ReadOnlySpan<byte> chunk, byte expectedHandle, out ReadOnlySpan<byte> stripped)
  {
    if (chunk.Length > 0 && chunk[0] == expectedHandle)
    {
      stripped = chunk[1..];
      return true;
    }
    stripped = default;
    return false;
  }

  /// <summary>
  /// Prepend the handle byte and split data into MTU-sized chunks for writing.
  /// Each BLE write: [handle][up to mtu-1 bytes of data].
  /// Written to characteristic 6A4E2820 (write-only data channel).
  /// </summary>
  public static List<byte[]> ChunkWithHandle(
      ReadOnlySpan<byte> data, byte handle, int mtu)
  {
    int payloadPerChunk = Math.Max(0, mtu - 1);
    var chunks = new List<byte[]>();
    if (payloadPerChunk == 0)
      return chunks;

    for (int offset = 0; offset < data.Length; offset += payloadPerChunk)
    {
      int len = Math.Min(payloadPerChunk, data.Length - offset);
      var chunk = new byte[1 + len];
      chunk[0] = handle;
      data.Slice(offset, len).CopyTo(chunk.AsSpan(1));
      chunks.Add(chunk);
    }
    return chunks;
  }
}
