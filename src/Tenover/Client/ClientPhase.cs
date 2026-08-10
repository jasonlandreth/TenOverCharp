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

namespace Tenover.Client;

/// <summary>Connection phases of the <see cref="Client"/> state machine.</summary>
public enum ClientPhase
{
    Registering,
    WaitDeviceInfo,
    WaitCapabilities,
    WaitCapabilitiesAck,
    Subscribing,
    WakingUp,
    Active,
}