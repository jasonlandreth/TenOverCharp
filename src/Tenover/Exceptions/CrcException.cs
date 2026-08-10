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

namespace TenOver.Exceptions;

/// <summary>CRC mismatch in a received GFDI frame.</summary>
public sealed class CrcException : TenoverException
{
    public ushort Expected { get; }
    public ushort Actual   { get; }

    public CrcException(ushort expected, ushort actual)
        : base($"CRC mismatch: expected 0x{expected:X4}, got 0x{actual:X4}")
    {
        Expected = expected;
        Actual   = actual;
    }
}