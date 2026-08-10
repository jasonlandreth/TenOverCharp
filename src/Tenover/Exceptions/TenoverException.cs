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

/// <summary>Base exception for all Tenover protocol errors.</summary>
public class TenoverException : Exception
{
    public TenoverException(string message) : base(message) { }
    public TenoverException(string message, Exception inner) : base(message, inner) { }
}