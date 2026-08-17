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

using Google.Protobuf;
using TenOver.Client.Metrics;
using TenOver.Exceptions;
using TenOver.Proto.EventSharing;
using TenOver.Proto.LaunchMonitor;
using BallMetrics = TenOver.Proto.LaunchMonitor.BallMetrics;
// Alias to avoid collision with the TenOver.Proto.SmartEvent.Error subclass
using LmError = TenOver.Proto.LaunchMonitor.Error;
// Alias the generated Smart class (same name as its namespace — must qualify it)
using SmartMsg = TenOver.Proto.Smart.Smart;

namespace TenOver.proto;

/// <summary>
/// Builds and decodes the Smart protobuf container messages used by the Garmin R10.
/// </summary>
public static class SmartDecoder
{
    const float MpsToMph = 2.236936f;
    /// <summary>Build a Subscribe(LAUNCH_MONITOR) Smart message.</summary>
    public static byte[] BuildSubscribeRequest()
    {
        var subscribe = new SubscribeRequest();
        subscribe.Alerts.Add(new AlertMessage { Type = AlertType.LaunchMonitor });

        var es = new EventSharingService { SubscribeRequest = subscribe };
        var smart = new SmartMsg { EventSharing = es };
        return smart.ToByteArray();
    }

    /// <summary>Build a WakeUpRequest Smart message.</summary>
    public static byte[] BuildWakeupRequest()
    {
        var lm = new Service { WakeUpRequest = new WakeUpRequest() };
        var smart = new SmartMsg { LaunchMonitorService = lm };
        return smart.ToByteArray();
    }


    /// <summary>Decode a Smart protobuf byte array and return a high-level <see cref="SmartEvent"/>.</summary>
    /// <exception cref="ProtobufDecodeException">Protobuf deserialization failed.</exception>
    public static SmartEvent Decode(byte[] pbData)
    {
        SmartMsg smart;
        try
        {
            smart = SmartMsg.Parser.ParseFrom(pbData);
        }
        catch (InvalidProtocolBufferException ex)
        {
            throw new ProtobufDecodeException(ex.Message, ex);
        }

        // EventSharing service (field 30)
        if (smart.EventSharing is { } es)
        {
            if (es.SubscribeResponse is { } subResp)
            {
                var first = subResp.AlertStatus.FirstOrDefault();
                bool success = first?.SubscribeStatus ==
                    SubscribeResponse.Types.AlertStatusMessage.Types.Status.Success;
                return new SmartEvent.SubscribeResponse { Success = success };
            }

            if (es.AlertNotification is { } notif)
                return DecodeAlertNotification(notif);

            return new SmartEvent.Unknown();
        }

        // LaunchMonitor service (field 38)
        if (smart.LaunchMonitorService is { } lmSvc)
        {
            if (lmSvc.WakeUpResponse is { } wakeResp)
                return new SmartEvent.WakeUpResponse
                {
                    Status = wakeResp.HasStatus ? (int)wakeResp.Status : 0
                };
            return new SmartEvent.LaunchMonitorResponse();
        }

        return new SmartEvent.Unknown();
    }

    private static SmartEvent DecodeAlertNotification(AlertNotification notif)
    {
        var details = notif.Details;
        if (details is null)
            return new SmartEvent.Unknown();

        // Shot metrics take priority
        if (details.Metrics is { } metrics)
            return new SmartEvent.Shot { Data = DecodeMetrics(metrics) };

        // State change
        if (details.State is { } stateMsg && stateMsg.HasState_)
        {
            var ds = DeviceStateFromProto((int)stateMsg.State_);
            if (ds.HasValue)
                return new SmartEvent.StateChange { State = ds.Value };
        }

        // Device error
        if (details.Error is { } error)
            return new SmartEvent.Error { DeviceError = DecodeError(error) };

        // Tilt calibration status
        if (details.TiltCalibration is { } cal)
            return new SmartEvent.CalibrationStatus
            {
                Status = cal.HasStatus ? (int)cal.Status : 0,
                Result = cal.HasResult ? (int)cal.Result : 0,
            };

        return new SmartEvent.Unknown();
    }

    private static DeviceState? DeviceStateFromProto(int v) => v switch
    {
        0 => DeviceState.Standby,
        1 => DeviceState.InterferenceTest,
        2 => DeviceState.Waiting,
        3 => DeviceState.Recording,
        4 => DeviceState.Processing,
        5 => DeviceState.Error,
        _ => null,
    };

    private static ShotData DecodeMetrics(Metrics m)
    {
        BallData? ball = null;
        if (m.BallMetrics is { } b)
        {
            float spinAxis = b.HasSpinAxis ? b.SpinAxis : 0f;
            float total = b.HasTotalSpin ? b.TotalSpin : 0f;
            float axisRad = spinAxis * MathF.PI / 180f;

            ball = new BallData
            {
                VerticalLaunchAngle = b.HasLaunchAngle ? b.LaunchAngle : 0f,
                HorizontalLaunchDirection = b.HasLaunchDirection ? b.LaunchDirection : 0f,
                BallSpeed = b.HasBallSpeed ? b.BallSpeed * MpsToMph : 0f,
                SpinAxis = spinAxis,
                TotalSpin = total,
                Backspin = total * MathF.Cos(axisRad),
                SideSpin = total * MathF.Sin(axisRad),
                SpinCalcType = b.HasSpinCalculationType
                    ? b.SpinCalculationType switch
                    {
                        BallMetrics.Types.SpinCalculationType.Ratio => SpinCalcType.Ratio,
                        BallMetrics.Types.SpinCalculationType.BallFlight => SpinCalcType.BallFlight,
                        BallMetrics.Types.SpinCalculationType.Measured => SpinCalcType.Measured,
                        _ => SpinCalcType.Other,
                    }
                    : SpinCalcType.Ratio,
            };
        }

        ClubData? club = null;
        if (m.ClubMetrics is { } c)
        {
            club = new ClubData
            {
                ClubHeadSpeed = c.HasClubHeadSpeed ? c.ClubHeadSpeed * MpsToMph : 0f,
                FaceToTarget = c.HasClubAngleFace ? c.ClubAngleFace : 0f,
                PathToTarget = c.HasClubAnglePath ? c.ClubAnglePath : 0f,
                AttackAngle = c.HasAttackAngle ? c.AttackAngle : 0f,
            };
        }

        SwingData? swing = null;
        if (m.SwingMetrics is { } s)
        {
            swing = new SwingData
            {
                BackswingStart = s.HasBackSwingStartTime ? s.BackSwingStartTime : 0u,
                DownswingStart = s.HasDownSwingStartTime ? s.DownSwingStartTime : 0u,
                Impact = s.HasImpactTime ? s.ImpactTime : 0u,
                FollowThroughEnd = s.HasFollowThroughEndTime ? s.FollowThroughEndTime : 0u,
            };
        }

        return new ShotData
        {
            ShotId = m.HasShotId ? m.ShotId : 0u,
            ShotType = m.HasShotType && m.ShotType == Metrics.Types.ShotType.Normal
                       ? ShotType.Normal : ShotType.Practice,
            Ball = ball,
            Club = club,
            Swing = swing,
        };
    }

    private static DeviceError DecodeError(LmError e)
    {
        (float Roll, float Pitch)? tilt = e.DeviceTilt is { } t
            ? (t.HasRoll ? t.Roll : 0f, t.HasPitch ? t.Pitch : 0f)
            : null;

        return new DeviceError
        {
            Code = e.HasCode ? e.Code switch
            {
                LmError.Types.ErrorCode.Overheating => ErrorCode.Overheating,
                LmError.Types.ErrorCode.RadarSaturation => ErrorCode.RadarSaturation,
                LmError.Types.ErrorCode.PlatformTilted => ErrorCode.PlatformTilted,
                _ => ErrorCode.Unknown,
            } : ErrorCode.Unknown,
            Severity = e.HasSeverity ? e.Severity switch
            {
                LmError.Types.Severity.Serious => ErrorSeverity.Serious,
                LmError.Types.Severity.Fatal => ErrorSeverity.Fatal,
                _ => ErrorSeverity.Warning,
            } : ErrorSeverity.Warning,
            Tilt = tilt,
        };
    }
}
