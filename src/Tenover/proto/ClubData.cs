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

/// <summary>Club head metrics at impact.</summary>
public sealed class ClubData
{
    /// <summary>Club head velocity at impact (m/s).</summary>
    public float ClubHeadSpeed { get; init; }
    /// <summary>Face angle at impact (degrees).</summary>
    public float FaceAngle { get; init; }
    /// <summary>Club path angle (degrees).</summary>
    public float PathAngle { get; init; }
    /// <summary>Angle of attack (degrees).</summary>
    public float AttackAngle { get; init; }
}