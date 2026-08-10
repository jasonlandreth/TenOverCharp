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

namespace Tenover.proto;

/// <summary>Swing timing data (absolute device timestamps in milliseconds).</summary>
public sealed class SwingData
{
    public uint BackswingStart { get; init; }
    public uint DownswingStart { get; init; }
    public uint Impact { get; init; }
    public uint FollowThroughEnd { get; init; }
}