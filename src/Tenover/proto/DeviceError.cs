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

/// <summary>R10 device error report.</summary>
public sealed class DeviceError
{
    public ErrorCode Code { get; init; }
    public ErrorSeverity Severity { get; init; }
    /// <summary>Device tilt (roll°, pitch°) if the error includes tilt data.</summary>
    public (float Roll, float Pitch)? Tilt { get; init; }
}