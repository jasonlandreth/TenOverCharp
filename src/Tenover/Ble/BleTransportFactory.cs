using System.Runtime.InteropServices;

namespace Tenover.Ble;

/// <summary>
/// Single entry point for connecting to the Garmin R10 without the caller
/// needing to know which transport backend is in play.
///
/// - On Windows, uses <see cref="WindowsBleTransport"/> (native WinRT BLE,
///   includes automatic pairing).
/// - On macOS/Linux, uses <see cref="UniversalBleTransport"/>.
///
/// Both backends return <see cref="IBleTransport"/>, so UI code only ever
/// needs to know about this one type.
/// </summary>
public static class BleTransportFactory
{
    /// <summary>
    /// Scan and connect to the first Garmin R10 found. Safe to await directly
    /// from a WinForms UI event handler — do not wrap in Task.Run or call
    /// .GetAwaiter().GetResult() on the UI thread.
    /// </summary>
    public static Task<IBleTransport> AutoConnectAsync(CancellationToken ct = default)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return ConnectWindowsAsync(ct);

        return ConnectUniversalAsync(ct);
    }

    /// <summary>
    /// Connect to a known device by platform-appropriate identifier:
    /// a BLE address (e.g. "F5:D1:88:F6:90:5D") on Windows, or the OS
    /// tracking id string on macOS/Linux. Safe to await directly from a
    /// WinForms UI event handler.
    /// </summary>
    public static Task<IBleTransport> ConnectAsync(string idOrAddress, CancellationToken ct = default)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return ConnectWindowsAsync(idOrAddress, ct);

        return ConnectUniversalAsync(idOrAddress, ct);
    }

    private static async Task<IBleTransport> ConnectWindowsAsync(CancellationToken ct)
        => await WindowsBleTransportAsync.AutoConnectAsync(ct);

    private static async Task<IBleTransport> ConnectWindowsAsync(string address, CancellationToken ct)
        => await WindowsBleTransportAsync.ConnectAsync(address, ct);

    private static async Task<IBleTransport> ConnectUniversalAsync(CancellationToken ct)
        => await UniversalBleTransportAsync.AutoConnectAsync(ct);

    private static async Task<IBleTransport> ConnectUniversalAsync(string idOrAddress, CancellationToken ct)
        => await UniversalBleTransportAsync.ConnectAsync(idOrAddress, ct);
}