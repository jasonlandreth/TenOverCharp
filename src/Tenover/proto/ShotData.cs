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

// ── Shot data ─────────────────────────────────────────────────────────────────

/// <summary>Decoded shot data from the R10.</summary>
public sealed class ShotData
{
  public uint ShotId { get; init; }
  public ShotType ShotType { get; init; }
  public BallData? Ball { get; init; }
  public ClubData? Club { get; init; }
  public SwingData? Swing { get; init; }
}

// ── Device state / error ──────────────────────────────────────────────────────

// ── Smart events ──────────────────────────────────────────────────────────────