namespace TenOver.proto;

/// <summary>Ball flight metrics. Speeds in m/s, angles in degrees, spin in RPM.</summary>
public sealed class BallData
{
    /// <summary>Vertical launch angle (degrees).</summary>
    public float LaunchAngle { get; init; }
    /// <summary>Horizontal launch direction (degrees).</summary>
    public float LaunchDirection { get; init; }
    /// <summary>Initial ball velocity (m/s).</summary>
    public float BallSpeed { get; init; }
    /// <summary>Spin axis tilt (degrees).</summary>
    public float SpinAxis { get; init; }
    /// <summary>Total spin rate (RPM).</summary>
    public float TotalSpin { get; init; }
    /// <summary>Backspin component (RPM). Computed: total_spin * cos(spin_axis).</summary>
    public float Backspin { get; init; }
    /// <summary>
    /// Sidespin component (RPM). Computed: total_spin * sin(spin_axis).
    /// </summary>
    public float Sidespin { get; init; }
    public SpinCalcType SpinCalcType { get; init; }
}