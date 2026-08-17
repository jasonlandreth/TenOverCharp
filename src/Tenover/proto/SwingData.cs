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

/// <summary>Swing timing data (absolute device timestamps in milliseconds).</summary>
public class SwingData
{
    /// <summary>
    /// The timestamp when the backswing starts, in milliseconds since the device started.
    /// </summary>
    public uint BackswingStart { get; init; }
    /// <summary>
    /// The timestamp when the downswing starts, in milliseconds since the device started.
    /// </summary>
    public uint DownswingStart { get; init; }
    /// <summary>
    /// The timestamp when the impact occurs, in milliseconds since the device started.
    /// </summary>
    public uint Impact { get; init; }
    /// <summary>
    /// The timestamp when the follow-through ends, in milliseconds since the device started.
    /// </summary>
    public uint FollowThroughEnd { get; init; }
}