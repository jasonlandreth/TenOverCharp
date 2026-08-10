using System.Runtime.InteropServices;

namespace TenOver.Ble;

/// <summary>
/// Single entry point for connecting to the Garmin R10 without the caller
/// needing to know which transport backend is in play.
///
/// This is one of only two public types in this library — the other is
/// <see cref="IBleTransport"/> itself. The concrete transport classes
/// (WindowsBleTransportAsync, UniversalBleTransportAsync, etc.) are
/// internal implementation details; callers only ever see the interface.
///
/// - On Windows, uses <see cref="WindowsBleTransportAsync"/> (native WinRT
///   BLE, includes automatic pairing).
/// - On macOS/Linux, uses <see cref="UniversalBleTransportAsync"/>.
///
/// Both backends return <see cref="IBleTransport"/>, so calling code only
/// ever needs to know about this one type.
/// </summary>
public static class BleTransportFactory
{
    /// <summary>
    /// Scan and connect to the first Garmin R10 found. Safe to await directly
    /// from a WinForms UI event handler — do not wrap in Task.Run or call
    /// .GetAwaiter().GetResult() on the UI thread.
    /// </summary>
    /// <param name="ct">A token to cancel the scan/connect operation.</param>
    /// <returns>A task that resolves to a connected <see cref="IBleTransport"/>.</returns>
    public static Task<IBleTransport> AutoConnectAsync(CancellationToken ct = default)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ConnectWindowsAsync(ct);
        }

        return ConnectUniversalAsync(ct);
    }

    /// <summary>
    /// Connect to a known device by platform-appropriate identifier:
    /// a BLE address (e.g. "F5:D1:88:F6:90:5D") on Windows, or the OS
    /// tracking id string on macOS/Linux. Safe to await directly from a
    /// WinForms UI event handler.
    /// </summary>
    /// <param name="idOrAddress">The device's platform-appropriate identifier.</param>
    /// <param name="ct">A token to cancel the connect operation.</param>
    /// <returns>A task that resolves to a connected <see cref="IBleTransport"/>.</returns>
    public static Task<IBleTransport> ConnectAsync(string idOrAddress, CancellationToken ct = default)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ConnectWindowsAsync(idOrAddress, ct);
        }

        return ConnectUniversalAsync(idOrAddress, ct);
    }

    /// <summary>Connects using the Windows-native transport, with no address specified.</summary>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task that resolves to a connected <see cref="IBleTransport"/>.</returns>
    private static async Task<IBleTransport> ConnectWindowsAsync(CancellationToken ct)
    {
        return await WindowsBleTransportAsync.AutoConnectAsync(ct);
    }

    /// <summary>Connects using the Windows-native transport, to a specific address.</summary>
    /// <param name="address">The device's BLE address.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task that resolves to a connected <see cref="IBleTransport"/>.</returns>
    private static async Task<IBleTransport> ConnectWindowsAsync(string address, CancellationToken ct)
    {
        return await WindowsBleTransportAsync.ConnectAsync(address, ct);
    }

    /// <summary>Connects using the cross-platform transport, with no id specified.</summary>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task that resolves to a connected <see cref="IBleTransport"/>.</returns>
    private static async Task<IBleTransport> ConnectUniversalAsync(CancellationToken ct)
    {
        return await UniversalBleTransportAsync.AutoConnectAsync(ct);
    }

    /// <summary>Connects using the cross-platform transport, to a specific id.</summary>
    /// <param name="idOrAddress">The device's OS tracking identifier or address.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task that resolves to a connected <see cref="IBleTransport"/>.</returns>
    private static async Task<IBleTransport> ConnectUniversalAsync(string idOrAddress, CancellationToken ct)
    {
        return await UniversalBleTransportAsync.ConnectAsync(idOrAddress, ct);
    }
}