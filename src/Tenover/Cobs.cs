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

using TenOver.Exceptions;

namespace TenOver;

/// <summary>
/// COBS (Consistent Overhead Byte Stuffing) codec.
/// Eliminates 0x00 bytes from data so they can serve as frame delimiters.
/// GFDI frames on the wire: [0x00] [COBS-encoded data] [0x00].
/// </summary>
public static class Cobs
{
  /// <summary>Encode <paramref name="data"/> using COBS. The output contains no 0x00 bytes.</summary>
  public static byte[] Encode(ReadOnlySpan<byte> data)
  {
    var output = new List<byte>(data.Length + data.Length / 254 + 1);
    int i = 0;
    while (i < data.Length)
    {
      int start = i;
      while (i < data.Length && data[i] != 0x00 && (i - start) < 0xFE)
        i++;
      output.Add((byte)(i - start + 1));
      for (int k = start; k < i; k++)
        output.Add(data[k]);
      if (i < data.Length && data[i] == 0x00)
        i++;
    }
    return [.. output];
  }

  /// <summary>Decode COBS-encoded <paramref name="data"/> back to the original bytes.</summary>
  /// <exception cref="CobsDecodeException">Malformed input.</exception>
  public static byte[] Decode(ReadOnlySpan<byte> data)
  {
    var output = new List<byte>(data.Length);
    int i = 0;
    while (i < data.Length)
    {
      byte code = data[i];
      if (code == 0)
        break;
      i++;
      for (int k = 1; k < code; k++)
      {
        if (i >= data.Length)
          throw new CobsDecodeException();
        output.Add(data[i++]);
      }
      if (code < 0xFF && i < data.Length)
        output.Add(0x00);
    }
    // Strip trailing zero added by the block boundary
    if (output.Count > 0 && output[^1] == 0x00)
      output.RemoveAt(output.Count - 1);
    return [.. output];
  }
}
