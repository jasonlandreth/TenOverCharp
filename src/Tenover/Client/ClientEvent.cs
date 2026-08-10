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

using Tenover.proto;

namespace Tenover.Client;

/// <summary>Events emitted by <see cref="Client.Poll"/>.</summary>
public abstract class ClientEvent
{
    private ClientEvent() { }

    /// <summary>MultiLink registration succeeded. GFDI handshake starting.</summary>
    public sealed class Registered : ClientEvent { public byte Handle { get; init; } }
    /// <summary>GFDI handshake complete. Protobuf session starting.</summary>
    public sealed class HandshakeComplete : ClientEvent { }
    /// <summary>Device is armed and waiting for a shot.</summary>
    public sealed class Ready : ClientEvent { }
    /// <summary>Shot data received.</summary>
    public sealed class Shot : ClientEvent { public ShotData Data { get; init; } = null!; }
    /// <summary>Device state changed.</summary>
    public sealed class StateChange : ClientEvent { public DeviceState State { get; init; } }
    /// <summary>Device reported an error.</summary>
    public sealed class DeviceError : ClientEvent { public proto.DeviceError Error { get; init; } = null!; }
    /// <summary>Subscribe response received.</summary>
    public sealed class Subscribed : ClientEvent { public bool Success { get; init; } }
    /// <summary>WakeUp response received.</summary>
    public sealed class WakeUpResponse : ClientEvent { public int Status { get; init; } }
}