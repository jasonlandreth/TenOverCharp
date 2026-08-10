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
using Tenover.Ble;
using Tenover.Exceptions;
using Tenover.proto;

namespace Tenover.Client;

// ── Events ────────────────────────────────────────────────────────────────────

// ── Phase ─────────────────────────────────────────────────────────────────────

// ── Client ────────────────────────────────────────────────────────────────────

/// <summary>
/// Poll-based Garmin R10 client.
///
/// The caller provides a <see cref="ITransport"/> implementation; this class handles
/// MultiLink registration, GFDI handshake, protobuf subscribe/wakeup, and shot
/// data decoding.
///
/// Usage:
/// <code>
///   var client = new Client(transport, mtu: 20);
///   client.Start();
///   while (true)
///   {
///       var ev = client.Poll();
///       if (ev is ClientEvent.Shot s) Console.WriteLine(s.Data);
///       else if (ev is null) Thread.Sleep(5);
///   }
/// </code>
/// </summary>
public sealed class Client
{
  private readonly ITransport _transport;
  private readonly int _mtu;
  private byte _handle;
  private ClientPhase _phase;
  private readonly Gfdi.StreamBuffer _streamBuf;
  private ushort _reqIdCounter;
  private readonly List<(uint ShotId, DateTime Time)> _recentShots;
  private DeviceState? _lastState;
  private readonly byte[] _readBuf;
  private ClientEvent? _pendingEvent;

  /// <summary>How long (seconds) to remember shot IDs for deduplication.</summary>
  private const int ShotDedupWindowSecs = 60;

  /// <summary>
  /// Create a new client.
  /// <paramref name="mtu"/> is the BLE ATT_MTU; the effective payload is capped at 20
  /// for R10 compatibility (MTU 23 → 20 usable).
  /// Call <see cref="Start"/> to begin the MultiLink registration, then poll in a loop.
  /// </summary>
  public Client(ITransport transport, int mtu = 20)
  {
    _transport = transport;
    _mtu = Math.Min(mtu, 20);
    _handle = 0;
    _phase = ClientPhase.Registering;
    _streamBuf = new Gfdi.StreamBuffer();
    _reqIdCounter = 1;
    _recentShots = [];
    _readBuf = new byte[512];
  }

  /// <summary>Current connection phase.</summary>
  public ClientPhase Phase => _phase;

  // ── Public API ────────────────────────────────────────────────────────────

  /// <summary>Send the MultiLink REGISTER command to begin the connection sequence.</summary>
  /// <exception cref="TenoverException">Transport write failed.</exception>
  public void Start()
  {
    byte[] cmd = Multilink.BuildRegister(1, Multilink.ServiceId.Gfdi);
    _transport.WriteRegister(cmd);
    _phase = ClientPhase.Registering;
  }

  /// <summary>
  /// Poll for the next event. Returns null if no data is currently available.
  /// The client advances through all connection phases automatically.
  /// </summary>
  /// <exception cref="CrcException">CRC mismatch in received frame.</exception>
  /// <exception cref="NakException">Device returned a NAK.</exception>
  /// <exception cref="FrameTooShortException">Malformed frame.</exception>
  /// <exception cref="CobsDecodeException">COBS decode failure.</exception>
  /// <exception cref="ProtobufDecodeException">Protobuf decode failure.</exception>
  /// <exception cref="MultiLinkRegisterException">Registration rejected.</exception>
  public ClientEvent? Poll()
  {
    // Drain any pending event before reading more data.
    if (_pendingEvent is { } pending)
    {
      _pendingEvent = null;
      return pending;
    }

    int n = _transport.Read(_readBuf);

    if (n > 0)
    {
      var chunk = _readBuf.AsSpan(0, n);

      // In the registration phase the very first response is the REGISTER_RESPONSE.
      if (_phase == ClientPhase.Registering)
        return HandleRegisterResponse(chunk);

      // Normal path: strip handle byte and feed to the stream buffer.
      if (Multilink.TryStripHandle(chunk, _handle, out var stripped))
        _streamBuf.Extend(stripped);
    }

    // Process one complete GFDI frame per poll (avoids event loss on early return).
    if (_streamBuf.NextFrame() is { } frame)
      return DispatchFrame(frame);

    return null;
  }

  // ── Frame dispatch ────────────────────────────────────────────────────────

  private ClientEvent? HandleRegisterResponse(ReadOnlySpan<byte> chunk)
  {
    var parsed = Multilink.ParseRegisterResponse(chunk);
    if (parsed.HasValue)
    {
      var (status, handle, _flags) = parsed.Value;
      if (status == Multilink.RegisterStatus.Success)
      {
        _handle = handle;
        _phase = ClientPhase.WaitDeviceInfo;
        return new ClientEvent.Registered { Handle = handle };
      }
      throw new MultiLinkRegisterException((byte)status);
    }

    // The device sometimes sends the 5024 device-info immediately after
    // the REGISTER_RESPONSE on the same characteristic. Feed it to the
    // stream buffer so we don't lose it.
    if (Multilink.TryStripHandle(chunk, _handle, out var stripped))
      _streamBuf.Extend(stripped);
    return null;
  }

  private ClientEvent? DispatchFrame(GfdiFrame frame) => frame.MsgType switch
  {
    Gfdi.MsgDeviceInfo => HandleDeviceInfo(frame.Payload),
    Gfdi.MsgConfiguration => HandleConfiguration(frame.Payload),
    Gfdi.MsgAck => HandleAck(frame.Payload),
    Gfdi.MsgFitDefinition or Gfdi.MsgFitData
                          => HandleFit(frame.MsgType),
    Gfdi.MsgProtobufRequest or Gfdi.MsgProtobufResponse
                          => HandleProtobuf(frame.Payload, frame.MsgType),
    _ => null,
  };

  // ── Handshake handlers ────────────────────────────────────────────────────

  private ClientEvent? HandleDeviceInfo(byte[] payload)
  {
    Gfdi.ParseDeviceInfo(payload); // validates payload length
    SendFrame(Gfdi.BuildDeviceInfoResponse());
    _phase = ClientPhase.WaitCapabilities;
    return null;
  }

  private ClientEvent? HandleConfiguration(byte[] payload)
  {
    Gfdi.ParseCapabilities(payload);
    SendAck(Gfdi.MsgConfiguration, 0, []);
    SendFrame(Gfdi.BuildHostCapabilities());
    _phase = ClientPhase.WaitCapabilitiesAck;
    return null;
  }

  private ClientEvent? HandleAck(byte[] payload)
  {
    if (payload.Length < 3) return null;

    ushort origType = BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(0));
    byte status = payload[2];

    if (status != 0)
      throw new NakException(origType, status);

    // Handshake completes when the device ACKs our 5050 host-capabilities frame.
    if (origType == Gfdi.MsgConfiguration && _phase == ClientPhase.WaitCapabilitiesAck)
    {
      _phase = ClientPhase.Subscribing;
      SendSubscribe();
      return new ClientEvent.HandshakeComplete();
    }
    return null;
  }

  private static ClientEvent? HandleFit(ushort msgType) => null;
  // FIT messages are ACKed inline in DispatchFrame

  // ── Protobuf handler ──────────────────────────────────────────────────────

  private ClientEvent? HandleProtobuf(byte[] payload, ushort msgType)
  {
    var (_, pbData) = Gfdi.ParseFragHeader(payload);
    var smartEvent = SmartDecoder.Decode(pbData);

    // ACK the received message type (could be 5043 or 5044).
    SendAck(msgType, 0, []);

    return smartEvent switch
    {
      SmartEvent.SubscribeResponse r => OnSubscribeResponse(r),
      SmartEvent.WakeUpResponse r => OnWakeUpResponse(r),
      SmartEvent.StateChange sc => OnStateChange(sc.State),
      SmartEvent.Shot s => OnShot(s.Data),
      SmartEvent.Error e => new ClientEvent.DeviceError { Error = e.DeviceError },
      _ => null,
    };
  }

  private ClientEvent? OnSubscribeResponse(SmartEvent.SubscribeResponse r)
  {
    if (_phase == ClientPhase.Subscribing)
    {
      _phase = ClientPhase.WakingUp;
      SendWakeup();
    }
    return new ClientEvent.Subscribed { Success = r.Success };
  }

  private ClientEvent? OnWakeUpResponse(SmartEvent.WakeUpResponse r)
  {
    if (_phase == ClientPhase.WakingUp)
      _phase = ClientPhase.Active;

    // status 1 = ALREADY_AWAKE: the device is already in WAITING from a
    // previous session and won't send state transitions again. Synthesise
    // a Ready event for the next poll() call.
    if (r.Status == 1)
    {
      _lastState = DeviceState.Waiting;
      _pendingEvent = new ClientEvent.Ready();
    }
    return new ClientEvent.WakeUpResponse { Status = r.Status };
  }

  private ClientEvent? OnStateChange(DeviceState state)
  {
    bool changed = _lastState != state;
    _lastState = state;
    if (!changed) return null;

    if (state == DeviceState.Waiting)
      return new ClientEvent.Ready();

    // Device went standby while active — re-arm it.
    if (state == DeviceState.Standby && _phase == ClientPhase.Active)
      SendWakeup();

    return new ClientEvent.StateChange { State = state };
  }

  private ClientEvent? OnShot(ShotData shot)
  {
    var now = DateTime.UtcNow;
    // Prune shots older than the dedup window.
    _recentShots.RemoveAll(s => (now - s.Time).TotalSeconds >= ShotDedupWindowSecs);

    // R10 replays shots at 6× — suppress duplicates.
    if (_recentShots.Any(s => s.ShotId == shot.ShotId))
      return null;

    _recentShots.Add((shot.ShotId, now));
    return new ClientEvent.Shot { Data = shot };
  }

  // ── Send helpers ──────────────────────────────────────────────────────────

  private void SendSubscribe()
  {
    byte[] pb = SmartDecoder.BuildSubscribeRequest();
    byte[] frame = Gfdi.BuildProtobufRequest(NextReqId(), pb);
    SendFrame(frame);
  }

  private void SendWakeup()
  {
    byte[] pb = SmartDecoder.BuildWakeupRequest();
    byte[] frame = Gfdi.BuildProtobufRequest(NextReqId(), pb);
    SendFrame(frame);
  }

  private void SendAck(ushort origType, byte status, byte[] payload)
      => SendFrame(Gfdi.BuildAck(origType, status, payload));

  private void SendFrame(byte[] frame)
  {
    byte[] cobs = Gfdi.WrapCobs(frame);
    var chunks = Multilink.ChunkWithHandle(cobs, _handle, _mtu);
    foreach (var chunk in chunks)
      _transport.Write(chunk);
  }

  private ushort NextReqId()
  {
    ushort id = _reqIdCounter;
    unchecked { _reqIdCounter++; }
    return id;
  }
}
