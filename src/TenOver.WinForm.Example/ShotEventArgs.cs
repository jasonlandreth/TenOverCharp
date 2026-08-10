namespace TenOver.WinForm.Example;

public class ShotEventArgs : EventArgs
{
    public float BallSpeedMph { get; }
    public float LaunchAngleDeg { get; }
    public float TotalSpinRpm { get; }
    public float ClubPathDeg { get; }
    public float FaceAngleDeg { get; }

    public ShotEventArgs(float ballSpeed, float launchAngle, float spin, float clubPath, float faceAngle)
    {
        BallSpeedMph = ballSpeed;
        LaunchAngleDeg = launchAngle;
        TotalSpinRpm = spin;
        ClubPathDeg = clubPath;
        FaceAngleDeg = faceAngle;
    }
}