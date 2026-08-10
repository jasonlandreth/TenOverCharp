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
using System.Text;
using TenOver.Exceptions;

namespace TenOver;

// ── Data types ────────────────────────────────────────────────────────────────

// ── GFDI framing layer ────────────────────────────────────────────────────────

/// <summary>
/// GFDI (Garmin Framework Device Interface) framing layer.
/// Handles frame construction, parsing, CRC verification, COBS wrapping,
/// and the 5024/5050 handshake sequence.
///
/// Wire format: [0x00] [COBS(frame)] [0x00]
/// Frame:       [length: u16 LE] [header: 2–4 bytes] [payload] [crc: u16 LE]
/// </summary>
internal static class Gfdi
{
  public const ushort MsgAck = 5000;
  public const ushort MsgDeviceInfo = 5024;
  public const ushort MsgFitDefinition = 5011;
  public const ushort MsgFitData = 5012;
  public const ushort MsgConfiguration = 5050;
  public const ushort MsgProtobufRequest = 5043;
  public const ushort MsgProtobufResponse = 5044;

  /// <summary>Capability bit index for the R10 launch monitor (SwingSensor).</summary>
  public const int CapSwingSensor = 30;

  // ── Stream buffer ─────────────────────────────────────────────────────────

  /// <summary>
  /// Accumulates BLE notification data and extracts complete COBS frames.
  /// GFDI frames are delimited by 0x00 bytes; BLE notifications may split
  /// a frame across multiple chunks.
  /// </summary>
  public sealed class StreamBuffer
  {
    private const int MaxSize = 64 * 1024;
    private readonly List<byte> _buf = [];

    /// <summary>Append raw data (handle byte already stripped).</summary>
    /// <exception cref="TenoverException">Buffer overflow (malformed stream).</exception>
    public void Extend(ReadOnlySpan<byte> data)
    {
      int newSize = _buf.Count + data.Length;
      if (newSize > MaxSize)
      {
        _buf.Clear();
        throw new TenoverException($"Stream buffer overflow ({newSize} bytes); buffer cleared.");
      }
      foreach (byte b in data)
        _buf.Add(b);
    }

    /// <summary>
    /// Extract the next complete COBS-delimited frame from the buffer.
    /// Returns null if no complete frame is available yet.
    /// Incomplete data remains in the buffer for the next call.
    /// </summary>
    /// <exception cref="CobsDecodeException">Malformed COBS data.</exception>
    /// <exception cref="CrcException">CRC mismatch.</exception>
    /// <exception cref="FrameTooShortException">Frame too short.</exception>
    public GfdiFrame? NextFrame()
    {
      while (true)
      {
        // Find opening 0x00
        int start = _buf.IndexOf((byte)0x00);
        if (start < 0)
          return null;

        // Find closing 0x00
        int endIdx = -1;
        for (int j = start + 1; j < _buf.Count; j++)
        {
          if (_buf[j] == (byte)0x00) { endIdx = j; break; }
        }
        if (endIdx < 0)
          return null;

        int cobsLen = endIdx - start - 1;
        var cobsData = _buf.GetRange(start + 1, cobsLen).ToArray();
        _buf.RemoveRange(0, endIdx + 1);

        if (cobsData.Length == 0)
          continue; // empty segment between consecutive 0x00 bytes

        byte[] decoded = Cobs.Decode(cobsData);
        return ParseFrame(decoded);
      }
    }
  }

  // ── Frame parsing ─────────────────────────────────────────────────────────

  /// <summary>Parse a decoded (post-COBS) GFDI frame.</summary>
  /// <exception cref="FrameTooShortException"/>
  /// <exception cref="CrcException"/>
  public static GfdiFrame ParseFrame(ReadOnlySpan<byte> frame)
  {
    if (frame.Length < 6)
      throw new FrameTooShortException(frame.Length, 6);

    ushort crcRecv = BinaryPrimitives.ReadUInt16LittleEndian(frame[(frame.Length - 2)..]);
    ushort crcCalc = Crc.Crc16(frame[..(frame.Length - 2)]);
    if (crcRecv != crcCalc)
      throw new CrcException(crcCalc, crcRecv);

    ushort msgType;
    byte? txnId;
    int payloadStart;

    if (frame.Length >= 4 && (frame[3] & 0x80) != 0)
    {
      // Compressed header with transaction ID
      msgType = (ushort)(frame[2] + 5000);
      txnId = (byte)(frame[3] & 0x7F);
      payloadStart = 4;
    }
    else
    {
      // Standard 4-byte header: [length u16][msgType u16][payload][crc u16]
      msgType = BinaryPrimitives.ReadUInt16LittleEndian(frame[2..]);
      txnId = null;
      payloadStart = 4;
    }

    return new GfdiFrame
    {
      MsgType = msgType,
      TxnId = txnId,
      Payload = frame[payloadStart..(frame.Length - 2)].ToArray(),
    };
  }

  // ── Frame building ────────────────────────────────────────────────────────

  /// <summary>Build a raw GFDI frame with a 4-byte header (no transaction ID).</summary>
  public static byte[] BuildFrame(ushort msgType, ReadOnlySpan<byte> payload)
  {
    int totalLen = 2 + 2 + payload.Length + 2;
    var frame = new byte[totalLen];
    BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(0), (ushort)totalLen);
    BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(2), msgType);
    payload.CopyTo(frame.AsSpan(4));
    ushort crc = Crc.Crc16(frame.AsSpan(0, totalLen - 2));
    BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(totalLen - 2), crc);
    return frame;
  }

  /// <summary>Build an ACK (type 5000) response frame.</summary>
  public static byte[] BuildAck(ushort origMsgType, byte status, ReadOnlySpan<byte> payload)
  {
    var ackPayload = new byte[3 + payload.Length];
    BinaryPrimitives.WriteUInt16LittleEndian(ackPayload.AsSpan(0), origMsgType);
    ackPayload[2] = status;
    payload.CopyTo(ackPayload.AsSpan(3));
    return BuildFrame(MsgAck, ackPayload);
  }

  /// <summary>COBS-encode a frame for transmission: [0x00][COBS(frame)][0x00].</summary>
  public static byte[] WrapCobs(ReadOnlySpan<byte> frame)
  {
    byte[] encoded = Cobs.Encode(frame);
    var out_ = new byte[2 + encoded.Length];
    out_[0] = 0x00;
    encoded.CopyTo(out_.AsSpan(1));
    out_[^1] = 0x00;
    return out_;
  }

  // ── Handshake helpers ─────────────────────────────────────────────────────

  private static string ReadLengthPrefixedString(ReadOnlySpan<byte> data, ref int pos)
  {
    if (pos >= data.Length) return "";
    int len = data[pos++];
    if (pos + len > data.Length) return "";
    string s = Encoding.UTF8.GetString(data.Slice(pos, len));
    pos += len;
    return s;
  }

  /// <summary>Parse device information from a 5024 payload.</summary>
  public static DeviceInfo ParseDeviceInfo(ReadOnlySpan<byte> payload)
  {
    if (payload.Length < 12)
      throw new FrameTooShortException(payload.Length, 12);

    var info = new DeviceInfo
    {
      ProtocolVersion = BinaryPrimitives.ReadUInt16LittleEndian(payload[0..]),
      ProductNumber = BinaryPrimitives.ReadUInt16LittleEndian(payload[2..]),
      UnitId = BinaryPrimitives.ReadUInt32LittleEndian(payload[4..]),
      SoftwareVersion = BinaryPrimitives.ReadUInt16LittleEndian(payload[8..]),
      MaxPacketSize = BinaryPrimitives.ReadUInt16LittleEndian(payload[10..]),
    };
    int pos = 12;
    info.FriendlyName = ReadLengthPrefixedString(payload, ref pos);
    info.DeviceName = ReadLengthPrefixedString(payload, ref pos);
    info.ModelName = ReadLengthPrefixedString(payload, ref pos);
    return info;
  }

  /// <summary>
  /// Build the host response ACK for a 5024 (device info) message.
  /// Identifies the host as a generic desktop client.
  /// </summary>
  public static byte[] BuildDeviceInfoResponse()
  {
    var host = new List<byte>(48);

    void WriteU16(ushort v) { host.Add((byte)(v & 0xFF)); host.Add((byte)(v >> 8)); }
    void WriteU32(uint v)
    {
      host.Add((byte)(v & 0xFF));
      host.Add((byte)((v >> 8) & 0xFF));
      host.Add((byte)((v >> 16) & 0xFF));
      host.Add((byte)(v >> 24));
    }
    void WriteStr(byte[] s) { host.Add((byte)s.Length); host.AddRange(s); }

    WriteU16(150);                               // host_protocol_version
    WriteU16(0xFFFF);                            // product_number
    WriteU32(0xFFFF_FFFF);                       // unit_id
    WriteU16(100);                               // app_version
    WriteU16(0xFFFF);                            // max_packet_size
    WriteStr("divotmaker"u8.ToArray());          // friendly_name
    WriteStr("Windows"u8.ToArray());             // device_name
    WriteStr("Desktop"u8.ToArray());             // model_name
    host.Add(0x01);                              // unknown_flag

    return BuildAck(MsgDeviceInfo, 0, [.. host]);
  }

  /// <summary>Parse capability bitmap from a 5050 payload. Returns active bit indices.</summary>
  public static List<int> ParseCapabilities(ReadOnlySpan<byte> payload)
  {
    var caps = new List<int>();
    if (payload.IsEmpty) return caps;
    int bitmapSize = payload[0];
    var bitmap = payload.Slice(1, Math.Min(bitmapSize, payload.Length - 1));
    for (int byteIdx = 0; byteIdx < bitmap.Length; byteIdx++)
    {
      for (int bit = 0; bit < 8; bit++)
      {
        if ((bitmap[byteIdx] & (1 << bit)) != 0)
          caps.Add(byteIdx * 8 + bit);
      }
    }
    return caps;
  }

  /// <summary>Build a host capabilities 5050 frame with the SwingSensor bit (30) set.</summary>
  public static byte[] BuildHostCapabilities()
  {
    var bitmap = new byte[13];
    bitmap[CapSwingSensor / 8] |= (byte)(1 << (CapSwingSensor % 8));

    var payload = new byte[1 + bitmap.Length];
    payload[0] = (byte)bitmap.Length;
    bitmap.CopyTo(payload.AsSpan(1));
    return BuildFrame(MsgConfiguration, payload);
  }

  // ── Protobuf fragmentation ────────────────────────────────────────────────

  /// <summary>Parse the 14-byte fragmentation header from a 5043/5044 payload.</summary>
  public static (FragHeader header, byte[] pbData) ParseFragHeader(ReadOnlySpan<byte> payload)
  {
    if (payload.Length < 14)
      throw new FrameTooShortException(payload.Length, 14);

    var hdr = new FragHeader
    {
      ReqId = BinaryPrimitives.ReadUInt16LittleEndian(payload[0..]),
      Offset = BinaryPrimitives.ReadUInt32LittleEndian(payload[2..]),
      TotalLen = BinaryPrimitives.ReadUInt32LittleEndian(payload[6..]),
      ChunkSize = BinaryPrimitives.ReadUInt32LittleEndian(payload[10..]),
    };
    int pbLen = Math.Min((int)hdr.ChunkSize, payload.Length - 14);
    return (hdr, payload.Slice(14, pbLen).ToArray());
  }

  /// <summary>
  /// Build a 5043 (protobuf request) frame with fragmentation header.
  /// Assumes single-fragment (no fragmentation needed for R10 messages).
  /// </summary>
  public static byte[] BuildProtobufRequest(ushort reqId, ReadOnlySpan<byte> pbData)
  {
    var payload = new byte[14 + pbData.Length];
    BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(0), reqId);
    BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(2), 0u);               // offset = 0
    BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(6), (uint)pbData.Length); // total_len
    BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(10), (uint)pbData.Length); // chunk_size
    pbData.CopyTo(payload.AsSpan(14));
    return BuildFrame(MsgProtobufRequest, payload);
  }
}
