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

namespace Tenover.Exceptions;

/// <summary>Device returned a NAK for a GFDI message.</summary>
public sealed class NakException : TenoverException
{
    public ushort MsgType { get; }
    public byte   Status  { get; }

    public NakException(ushort msgType, byte status)
        : base($"GFDI NAK for message {msgType}: status {status}")
    {
        MsgType = msgType;
        Status  = status;
    }
}