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

// ── Shot data ─────────────────────────────────────────────────────────────────

/// <summary>Decoded shot data from the R10.</summary>
public class ShotData
{
    /// <summary>
    /// Unique identifier for the shot. This is a monotonically increasing value that increments with each shot taken.
    /// </summary>
    public uint ShotId { get; init; }
    /// <summary>
    /// Type of shot taken (e.g., full swing, chip, putt, etc.).
    /// </summary>
    public ShotType ShotType { get; init; }
    /// <summary>
    /// Data about the ball's trajectory and performance.
    /// </summary>
    public BallData? Ball { get; init; }
    /// <summary>
    /// Data about the club used for the shot.
    /// </summary>
    public ClubData? Club { get; init; }
    /// <summary>
    /// Data about the swing mechanics.
    /// </summary>
    public SwingData? Swing { get; init; }
}