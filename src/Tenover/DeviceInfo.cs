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

namespace TenOver;

/// <summary>Device information from the 5024 handshake message.</summary>
internal sealed class DeviceInfo
{
    public ushort ProtocolVersion { get; set; }
    public ushort ProductNumber { get; set; }
    public uint UnitId { get; set; }
    public ushort SoftwareVersion { get; set; }
    public ushort MaxPacketSize { get; set; }
    public string FriendlyName { get; set; } = "";
    public string DeviceName { get; set; } = "";
    public string ModelName { get; set; } = "";
}