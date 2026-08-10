# TenOverCharp

A C# client library for the Garmin R10 launch monitor — connects over Bluetooth Low Energy, handles the device's connection/handshake protocol, and exposes shot data (ball speed, launch angle, spin, club data, etc.) through a simple, poll-based API.

This is a **C# port** of [`divotmaker/10over`](https://github.com/divotmaker/10over), a Rust library that reverse-engineered the R10's BLE protocol for the purpose of interoperability. See [Acknowledgments](#acknowledgments--credits) below — none of this would exist without that original work.

## Disclaimer

This project is **not affiliated with or endorsed by Garmin Ltd.** Garmin, Approach, and R10 are trademarks of their respective owner.

This library exists solely to enable interoperability between the Garmin R10 and third-party golf simulation software, under the reverse-engineering interoperability exception (17 U.S.C. § 1201(f)). It must not be used to circumvent licensing or subscription requirements on Garmin products, unlock paid features without purchase, or bypass access controls on Garmin software or services.

## Status

**Early / hobby project.** This is a personal project I'm building and learning on in the open — expect rough edges, and expect the API to change as things get fleshed out. Not recommended for production use yet.

## What's in this repo

| Project | Description |
|---|---|
| `Tenover` | Core client — `Client`, protocol/state handling, shot event types. Transport-agnostic; works with anything implementing `ITransport`. |
| `Tenover.Ble` | BLE transport implementations — connects to the R10 and feeds raw frames to `Client`. |
| `TenOver.Winform.Example` | A WinForms example app (shot simulator/visualizer) showing the library wired up end-to-end. |

### BLE transports

There are a few transport variants depending on platform and how you're calling them:

| Class | Platform | Call style | Use when |
|---|---|---|---|
| `WindowsBleTransport` | Windows (WinRT BLE) | Synchronous (`AutoConnect()`/`Connect()`) | Console apps / quick testing |
| `WindowsBleTransportAsync` | Windows (WinRT BLE) | Async (`AutoConnectAsync()`/`ConnectAsync()`) | WinForms/WPF UI apps — safe to `await` from a UI thread |
| `UniversalBleTransport` | Windows/macOS/Linux (via `InTheHand.Bluetooth`) | Synchronous | Console apps / quick testing |
| `UniversalBleTransportAsync` | Windows/macOS/Linux (via `InTheHand.Bluetooth`) | Async | WinForms/WPF UI apps |

The Windows-specific transports pair automatically if the device requires bonding — no need to pre-pair via Settings first. The cross-platform (`Universal*`) transports will try connecting without prior pairing, and if that fails because the device has genuinely never been paired to Windows, they'll open Bluetooth Settings for you and pick up automatically once you finish pairing there.

All transports implement a common `IBleTransport` interface, so calling code doesn't need to know which one it's holding.

## Getting Started

### Prerequisites

- .NET 8 SDK or later
- Windows 10/11 (required for `WindowsBleTransport`/`WindowsBleTransportAsync`; the `Universal*` transports also run on macOS/Linux)
- A Garmin Approach R10

### Quick start (console)

```csharp
using Tenover;
using Tenover.Ble;

WindowsBleTransport transport;
try
{
    transport = WindowsBleTransport.AutoConnect();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"connection failed: {ex.Message}");
    return 1;
}

Console.Error.WriteLine($"connected  {transport.DeviceAddress}  ({transport.DeviceName})");

using (transport)
{
    var client = new Client(transport, mtu: 20);
    client.Start();

    while (true)
    {
        var ev = client.Poll();
        if (ev is ClientEvent.Shot s)
        {
            Console.WriteLine($"Shot #{s.Data.ShotId}");
        }
    }
}
```

### Quick start (WinForms / async)

```csharp
private IBleTransport? _transport;

private async void ConnectButton_Click(object sender, EventArgs e)
{
    try
    {
        _transport = await WindowsBleTransportAsync.AutoConnectAsync();
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message, "Connection failed");
    }
}
```

See `TenOver.Winform.Example` for a full working example, including a background polling loop that keeps the UI responsive while listening for shot data.

## Acknowledgments & Credits

The Bluetooth protocol logic in this library — the GATT characteristics, MultiLink registration, GFDI handshake, and shot-data framing — is a **C# port of [`10over`](https://github.com/divotmaker/10over)** by **[divotmaker](https://github.com/divotmaker)** (Eric Thill), originally written in Rust.

All credit for reverse-engineering the R10's protocol belongs to the original author. This repository would not exist without that work. If you're working with the R10's protocol directly, or want the Rust original, go there first.

The original project's legal basis (reverse-engineering under the DMCA § 1201(f) interoperability exception) and acceptable-use terms apply equally to this port — see [`LICENSE`](./LICENSE) and [`NOTICE`](./NOTICE).

## License

See [`LICENSE`](./LICENSE) for the terms this project is distributed under, and [`NOTICE`](./NOTICE) for required attribution to the original `10over` project. The original Apache License, Version 2.0 text (referenced by `LICENSE`) is included in full in [`LICENSE-APACHE.txt`](./LICENSE-APACHE.txt).

## Contributing

This is a personal hobby project I'm actively learning on, but issues and pull requests are welcome — see [`CONTRIBUTING.md`](./CONTRIBUTING.md) for ground rules before opening one.
