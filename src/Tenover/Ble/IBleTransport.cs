using System;

namespace Tenover.Ble;

/// <summary>
/// Common surface shared by both BLE transport backends
/// (<see cref="WindowsBleTransport"/> and <see cref="UniversalBleTransport"/>),
/// so calling code can work with either without caring which one was picked.
/// </summary>
public interface IBleTransport : ITransport, IDisposable
{
    /// <summary>Address or OS tracking identifier of the connected device.</summary>
    string DeviceAddress { get; }

    /// <summary>Device name as reported by BLE advertisement.</summary>
    string DeviceName { get; }
}
