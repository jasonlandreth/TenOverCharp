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

using System;

namespace Tenover.Ble;

/// <summary>
/// Common surface shared by both BLE transport backends
/// (<see cref="WindowsBleTransportAsync"/> and <see cref="UniversalBleTransportAsync"/>),
/// so calling code can work with either without caring which one was picked.
///
/// This interface, along with <see cref="BleTransportFactory"/>, is the
/// entire public API surface of this library's BLE layer. The concrete
/// transport classes are internal — callers get an instance only through
/// the factory and interact with it only through this interface.
/// </summary>
public interface IBleTransport : ITransport, IDisposable
{
    /// <summary>Address or OS tracking identifier of the connected device.</summary>
    string DeviceAddress { get; }

    /// <summary>Device name as reported by BLE advertisement.</summary>
    string DeviceName { get; }
}