namespace TenOver.proto;

/// <summary>
/// Ball flight metrics. Speeds in m/s, angles in degrees, spin in RPM.
/// </summary>
public sealed class BallData
{
    /// <summary>
    /// Vertical launch angle (degrees).
    /// </summary>
    public float VerticalLaunchAngle { get; init; }
    /// <summary>
    /// Horizontal launch direction (degrees).
    /// </summary>
    public float HorizontalLaunchDirection { get; init; }
    /// <summary>
    /// Initial ball velocity (mph).
    /// </summary>
    public float BallSpeed { get; init; }
    /// <summary>
    /// Spin axis tilt (degrees).
    /// </summary>
    public float SpinAxis { get; init; }
    /// <summary>
    /// Total spin rate (RPM).
    /// </summary>
    public float TotalSpin { get; init; }
    /// <summary>
    /// Backspin component (RPM). Computed: total_spin * cos(spin_axis).
    /// </summary>
    public float Backspin { get; init; }
    /// <summary>
    /// Sidespin component (RPM). Computed: total_spin * sin(spin_axis).
    /// </summary>
    public float SideSpin { get; init; }
    /// <summary>
    /// Spin calculation method used to derive spin metrics.
    /// </summary>
    public SpinCalcType SpinCalcType { get; init; }
    /// <summary>
    /// Calculates the shot shape based on launch angle and spin axis.
    /// </summary>
    public ShotShape Shape => ShotShapeClassifier.GetShotShape(VerticalLaunchAngle, SpinAxis);
}