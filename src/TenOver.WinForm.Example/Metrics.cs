using System;
using System.Collections.Generic;
using System.Text;
using Tenover.proto;

namespace TenOver.WinForm.Example
{
    public class AwesomeGolfMetrics
    {
        // Club Metrics
        public float ClubSpeedMph { get; set; }
        public float AttackAngle { get; set; }
        public float ClubPath { get; set; }
        public float FaceAngle { get; set; }
        public float FaceToPath { get; set; }
        public float SmashFactor { get; set; }

        // Ball Launch Metrics
        public float BallSpeedMph { get; set; }
        public float LaunchAngle { get; set; }
        public float LaunchDirection { get; set; }
        public float TotalSpin { get; set; }
        public float SpinAxis { get; set; }
        public float BackSpin { get; set; }
        public float SideSpin { get; set; }

        // Flight & Result Metrics
        public float CarryYards { get; set; }
        public float RollYards { get; set; }
        public float TotalYards { get; set; }
        public float OfflineYards { get; set; }
        public float ApexHeightFeet { get; set; }
        public float FlightTimeSeconds { get; set; }
        public float DescentAngle { get; set; }
    }

    public static class MetricCalculator
    {
        public static AwesomeGolfMetrics ProcessGarminShotToAwesomeMetrics(ShotData shot, SimulationResult flightResult)
        {
            //record AwesomeMetrics
            var metrics = new AwesomeGolfMetrics();

            // 1. Ball Launch Data
            if (shot?.Ball != null)
            {
                metrics.BallSpeedMph = shot.Ball.BallSpeed * 2.23694f;
                metrics.LaunchAngle = shot.Ball.LaunchAngle;
                metrics.LaunchDirection = shot.Ball.LaunchDirection;
                metrics.TotalSpin = shot.Ball.TotalSpin;
                metrics.SpinAxis = shot.Ball.SpinAxis;

                // Convert spin components
                double rad = shot.Ball.SpinAxis * (Math.PI / 180.0);
                metrics.BackSpin = (float)(shot.Ball.TotalSpin * Math.Cos(rad));
                metrics.SideSpin = (float)(shot.Ball.TotalSpin * Math.Sin(rad));
            }

            // 2. Club Data
            if (shot?.Club != null)
            {
                metrics.ClubSpeedMph = shot.Club.ClubHeadSpeed * 2.23694f;
                metrics.AttackAngle = shot.Club.AttackAngle;
                metrics.ClubPath = shot.Club.PathAngle;
                metrics.FaceAngle = shot.Club.FaceAngle;

                // Derived Club Calculations
                metrics.FaceToPath = metrics.FaceAngle - metrics.ClubPath;
                metrics.SmashFactor = metrics.ClubSpeedMph > 0 ? metrics.BallSpeedMph / metrics.ClubSpeedMph : 0f;
            }

            // 3. Simulated Flight Data
            metrics.CarryYards = flightResult.CarryYards;
            metrics.RollYards = flightResult.RolloutYards;
            metrics.TotalYards = flightResult.TotalYards;
            metrics.OfflineYards = flightResult.DeviationYards;
            metrics.ApexHeightFeet = flightResult.ApexMeters * 3.28084f;
            metrics.FlightTimeSeconds = flightResult.FlightTimeSeconds;
            metrics.DescentAngle = flightResult.DescentAngleDegrees;

            return metrics;
        }
    }
}