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


// tenover-example — Connect to a Garmin R10 and print shots.
//
// Usage:
//   dotnet run --project Tenover.Example
//
// Auto-discovers a paired R10 and connects. The device must be paired at the OS level
// (Windows Settings → Bluetooth → Add a device → Approach R10).

using TenOver;
using TenOver.Ble;
using TenOver.Client;
using TenOver.Exceptions;

const float MsToMph = 2.237f;

Console.Error.WriteLine("searching for Garmin R10...");

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

  int shotCount = 0;
  bool readyPrinted = false;

  while (true)
  {
    try
    {
      var ev = client.Poll();
      if (ev is null)
      {
        Thread.Sleep(5);
        continue;
      }

      switch (ev)
      {
        case ClientEvent.Registered r:
          Console.Error.WriteLine($"registered  handle={r.Handle}");
          break;

        case ClientEvent.HandshakeComplete:
          Console.Error.WriteLine("handshake complete");
          break;

        case ClientEvent.Subscribed:
        case ClientEvent.WakeUpResponse:
          // No user-visible output for these protocol events.
          break;

        case ClientEvent.Ready:
          if (!readyPrinted)
          {
            Console.Error.WriteLine("READY — waiting for shot");
            readyPrinted = true;
          }
          break;

        case ClientEvent.StateChange:
          readyPrinted = false;
          break;

        case ClientEvent.DeviceError de:
          Console.Error.WriteLine($"DEVICE ERROR: {de.Error.Code} ({de.Error.Severity})");
          if (de.Error.Tilt is { } tilt)
            Console.Error.WriteLine($"  tilt: roll={tilt.Roll:F1}°  pitch={tilt.Pitch:F1}°");
          break;

        case ClientEvent.Shot s:
          {
            readyPrinted = false;
            shotCount++;
            Console.WriteLine($"\n── Shot #{shotCount} (id={s.Data.ShotId}) ──");

            if (s.Data.Ball is { } b)
            {
              Console.WriteLine(
                  $"  Ball: {b.BallSpeed * MsToMph,6:F1} mph  " +
                  $"LA {b.LaunchAngle,5:F1}°  Dir {b.LaunchDirection,5:F1}°");
              Console.WriteLine(
                  $"  Spin: {b.TotalSpin,6:F0} RPM  axis {b.SpinAxis,5:F1}°  " +
                  $"(back {b.Backspin,6:F0}, side {b.Sidespin,5:F0})  [{b.SpinCalcType}]");
            }

            if (s.Data.Club is { } c)
            {
              Console.WriteLine(
                  $"  Club: {c.ClubHeadSpeed * MsToMph,6:F1} mph  " +
                  $"face {c.FaceAngle,5:F1}°  path {c.PathAngle,5:F1}°  AoA {c.AttackAngle,5:F1}°");
            }

            if (s.Data.Swing is { } sw)
            {
              uint tempo = sw.DownswingStart >= sw.BackswingStart
                  ? sw.DownswingStart - sw.BackswingStart : 0u;
              uint down = sw.Impact >= sw.DownswingStart
                  ? sw.Impact - sw.DownswingStart : 0u;
              Console.WriteLine($"  Tempo: backswing {tempo} ms  downswing {down} ms");
            }
            break;
          }
      }
    }
    catch (TenoverException ex)
    {
      // Non-fatal protocol warnings (CRC glitch, unknown frame, etc.)
      Console.Error.WriteLine($"warning: {ex.Message}");
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine($"fatal: {ex.Message}");
      return 1;
    }
  }
}
