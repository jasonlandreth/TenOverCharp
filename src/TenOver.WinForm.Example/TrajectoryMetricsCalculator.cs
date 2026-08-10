using System;
using System.Collections.Generic;
using System.Text;

namespace TenOver.WinForm.Example
{
 

    public static class TrajectoryMetricsCalculator
    {
        /// <summary>
        /// Calculates Apex, Flight Time, and Descent Angle from trajectory points.
        /// </summary>
        public static (float ApexMeters, float FlightTimeSeconds, float DescentAngleDegrees) CalculateTrajectoryMetrics(
            List<TrajectoryPoint> points,
            float timeStepSeconds = 0.01f)
        {
            if (points == null || points.Count < 2)
                return (0f, 0f, 0f);

            float maxApexMeters = 0f;

            // 1. Calculate Apex Height
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].Y > maxApexMeters)
                {
                    maxApexMeters = points[i].Y;
                }
            }

            // 2. Calculate Flight Time
            // If TrajectoryPoint has a TimeSeconds property:
            float flightTimeSeconds = points[points.Count - 1].TimeSeconds;

            // Alternative if using fixed time steps:
            // float flightTimeSeconds = (points.Count - 1) * timeStepSeconds;

            // 3. Calculate Descent Angle at Impact
            var pPrev = points[points.Count - 2];
            var pLast = points[points.Count - 1];

            float deltaY = pLast.Y - pPrev.Y;
            float deltaX = pLast.X - pPrev.X;
            float deltaZ = pLast.Z - pPrev.Z;

            // Total 2D horizontal distance covered between the last two frames
            float horizontalDistance = (float)Math.Sqrt((deltaX * deltaX) + (deltaZ * deltaZ));

            // Calculate descent angle in radians and convert to degrees
            double descentRadians = Math.Atan2(Math.Abs(deltaY), horizontalDistance);
            float descentAngleDegrees = (float)(descentRadians * (180.0 / Math.PI));

            return (maxApexMeters, flightTimeSeconds, descentAngleDegrees);
        }
    }
}
