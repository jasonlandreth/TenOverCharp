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

namespace TenOver.proto;

/// <summary>High-level event decoded from a Smart protobuf message received from the R10.</summary>
public abstract class SmartEvent
{
    private SmartEvent() { }

    public sealed class SubscribeResponse : SmartEvent { public bool Success { get; init; } }
    public sealed class WakeUpResponse : SmartEvent { public int Status { get; init; } }
    public sealed class StateChange : SmartEvent { public DeviceState State { get; init; } }
    public sealed class Shot : SmartEvent { public ShotData Data { get; init; } = null!; }
    public sealed class Error : SmartEvent { public DeviceError DeviceError { get; init; } = null!; }
    public sealed class CalibrationStatus : SmartEvent { public int Status { get; init; } public int Result { get; init; } }
    public sealed class LaunchMonitorResponse : SmartEvent { }
    public sealed class Unknown : SmartEvent { }
}