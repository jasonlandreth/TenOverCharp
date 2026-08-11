using System;
using System.Collections.Generic;
using TenOver.proto;

namespace TenOver.WinForm.Example
{
    /// <summary>
    /// Represents a 3D vector or spatial coordinate point (X = Downrange, Y = Height, Z = Lateral).
    /// </summary>
    public struct Vector3D
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vector3D(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3D operator -(Vector3D a, Vector3D b)
        {
            return new Vector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Vector3D operator +(Vector3D a, Vector3D b)
        {
            return new Vector3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }
    }

    /// <summary>
    /// Represents a single 3D spatial coordinate and timestamp during ball flight.
    /// </summary>
    public struct TrajectoryPoint
    {
        /// <summary>3D position vector in space (X = Downrange meters, Y = Elevation meters, Z = Lateral meters).</summary>
        public Vector3D Position { get; set; }

        /// <summary>Elapsed time from impact in seconds.</summary>
        public float TimeSeconds { get; set; }

        // Convenience pass-through properties for quick component access
        public float X => Position.X;
        public float Y => Position.Y;
        public float Z => Position.Z;

        public TrajectoryPoint(Vector3D position, float timeSeconds)
        {
            Position = position;
            TimeSeconds = timeSeconds;
        }

        public TrajectoryPoint(float x, float y, float z, float timeSeconds)
        {
            Position = new Vector3D(x, y, z);
            TimeSeconds = timeSeconds;
        }
    }

    /// <summary>
    /// Contains complete trajectory points and derived metric results calculated by GolfBallFlightEngine.
    /// </summary>
    public struct SimulationResult
    {
        // ── Distance & Deviation Metrics ───────────────────────────────────

        /// <summary>Carry distance in yards (landing location at ground contact Y=0).</summary>
        public float CarryYards { get; set; }

        /// <summary>Calculated rollout distance after first impact in yards.</summary>
        public float RolloutYards { get; set; }

        /// <summary>Total distance in yards (CarryYards + RolloutYards).</summary>
        public float TotalYards => CarryYards + RolloutYards;

        /// <summary>Lateral offset off center line in yards at rest. Positive = Right, Negative = Left.</summary>
        public float DeviationYards { get; set; }


        // ── Trajectory Geometry & Altitude Metrics ─────────────────────────

        /// <summary>Maximum peak height reached during flight in meters.</summary>
        public float ApexMeters { get; set; }

        /// <summary>Maximum peak height reached during flight in yards.</summary>
        public float ApexYards => ApexMeters * 1.09361f;

        /// <summary>Maximum peak height reached during flight in feet.</summary>
        public float ApexFeet => ApexMeters * 3.28084f;


        // ── Time & Flight Angle Metrics ────────────────────────────────────

        /// <summary>Total air time / hang time from tee impact to first ground contact in seconds.</summary>
        public float FlightTimeSeconds { get; set; }

        /// <summary>Impact angle relative to horizontal ground as the ball hits the ground in degrees.</summary>
        public float DescentAngleDegrees { get; set; }


        // ── Full Flight Path Data ──────────────────────────────────────────

        /// <summary>Ordered list of 3D trajectory points representing ball flight path.</summary>
        public List<TrajectoryPoint> TrajectoryPoints { get; set; }


        // ── Helper Calculation Method ─────────────────────────────────────

        /// <summary>
        /// Analyzes a populated array of trajectory points to auto-calculate Apex, Flight Time, and Descent Angle.
        /// </summary>
        /// <param name="points">Processed 3D trajectory points.</param>
        public void CalculateDerivedMetricsFromPoints(List<TrajectoryPoint> points)
        {
            TrajectoryPoints = points ?? new List<TrajectoryPoint>();

            if (TrajectoryPoints.Count < 2)
            {
                ApexMeters = 0f;
                FlightTimeSeconds = 0f;
                DescentAngleDegrees = 0f;
                return;
            }

            float maxApex = 0f;

            // 1. Calculate Peak Altitude (Apex)
            for (int i = 0; i < TrajectoryPoints.Count; i++)
            {
                if (TrajectoryPoints[i].Y > maxApex)
                {
                    maxApex = TrajectoryPoints[i].Y;
                }
            }

            ApexMeters = maxApex;

            // 2. Flight Time (Timestamp of final impact point)
            FlightTimeSeconds = TrajectoryPoints[TrajectoryPoints.Count - 1].TimeSeconds;

            // 3. Descent Angle (Angle formed by last two trajectory frames)
            var pPrev = TrajectoryPoints[TrajectoryPoints.Count - 2];
            var pLast = TrajectoryPoints[TrajectoryPoints.Count - 1];

            float deltaY = pLast.Y - pPrev.Y;
            float deltaX = pLast.X - pPrev.X;
            float deltaZ = pLast.Z - pPrev.Z;

            float horizontalDist = (float)Math.Sqrt((deltaX * deltaX) + (deltaZ * deltaZ));
            double radians = Math.Atan2(Math.Abs(deltaY), horizontalDist);

            DescentAngleDegrees = (float)(radians * (180.0 / Math.PI));
        }
    }

    public static class GolfBallFlightEngine
    {
        /// <summary>
        /// Main entry point: Accepts raw Garmin R10 ShotData, executes 3D trajectory integration,
        /// and returns a populated SimulationResult containing carry, rollout, apex, and flight metrics.
        /// </summary>
        public static SimulationResult ProcessGarminShot(ShotData shot)
        {
            if (shot?.Ball == null)
            {
                return new SimulationResult();
            }

            // 1. Convert Garmin metric inputs (m/s to mph)
            float speedMph = shot.Ball.BallSpeed * 2.23694f;
            float launchAngleDeg = shot.Ball.LaunchAngle;
            float launchDirectionDeg = shot.Ball.LaunchDirection;
            float totalSpinRpm = shot.Ball.TotalSpin;
            float spinAxisDeg = shot.Ball.SpinAxis;

            // 2. Run numerical trajectory simulation to generate 3D flight path points
            List<TrajectoryPoint> trajectory = SimulateFlightPath(
                speedMph,
                launchAngleDeg,
                launchDirectionDeg,
                totalSpinRpm,
                spinAxisDeg
            );

            // 3. Extract landing point (ground contact Y = 0)
            TrajectoryPoint impactPoint = trajectory[trajectory.Count - 1];
            float carryYards = impactPoint.Position.X * 1.09361f; // meters to yards
            float deviationYards = impactPoint.Position.Z * 1.09361f;

            // 4. Estimate rollout based on landing angle and ball speed
            float rolloutYards = EstimateRollout(carryYards, speedMph, launchAngleDeg);

            // 5. Construct result and compute derived metrics (Apex, FlightTime, DescentAngle)
            var result = new SimulationResult
            {
                CarryYards = carryYards,
                RolloutYards = rolloutYards,
                DeviationYards = deviationYards
            };

            // Calculates ApexMeters, FlightTimeSeconds, and DescentAngleDegrees from trajectory
            result.CalculateDerivedMetricsFromPoints(trajectory);

            return result;
        }

        /// <summary>
        /// Numerical flight integrator generating 3D TrajectoryPoints.
        /// </summary>
        private static List<TrajectoryPoint> SimulateFlightPath(float speedMph, float launchAngleDeg,
            float launchDirDeg, float spinRpm, float spinAxisDeg)
        {
            var points = new List<TrajectoryPoint>();

            // Basic physics constants & conversion to m/s
            float v0 = speedMph * 0.44704f;
            double launchRad = launchAngleDeg * (Math.PI / 180.0);
            double dirRad = launchDirDeg * (Math.PI / 180.0);

            // Initial velocity components
            float vx = (float)(v0 * Math.Cos(launchRad) * Math.Cos(dirRad));
            float vy = (float)(v0 * Math.Sin(launchRad));
            float vz = (float)(v0 * Math.Cos(launchRad) * Math.Sin(dirRad));

            // Position state
            float px = 0f, py = 0f, pz = 0f;
            float t = 0f;
            float dt = 0.016f; // ~60 Hz simulation resolution

            // Spin axis tilt force factor
            double spinAxisRad = spinAxisDeg * (Math.PI / 180.0);
            float liftCoeff = (float)(spinRpm * 0.000015f);
            float sideForceCoeff = (float)(liftCoeff * Math.Sin(spinAxisRad));

            // Start at origin (Tee)
            points.Add(new TrajectoryPoint(new Vector3D(px, py, pz), t));

            // Numerical Euler integration loop until ground contact (py < 0)
            while (py >= 0f && t < 15.0f) // 15-second sanity cutoff limit
            {
                t += dt;

                // Gravitational drag & aerodynamic deceleration approximation
                vy -= 9.81f * dt;
                vx *= (1.0f - (0.0015f * dt));
                vz += sideForceCoeff * dt;

                px += vx * dt;
                py += vy * dt;
                pz += vz * dt;

                // Force impact clamp to ground level Y = 0
                if (py < 0f) py = 0f;

                points.Add(new TrajectoryPoint(new Vector3D(px, py, pz), t));

                if (py == 0f) break; // Impact reached
            }

            return points;
        }

        private static float EstimateRollout(float carryYards, float speedMph, float launchAngleDeg)
        {
            // Simple heuristic estimate for rollout distance based on launch condition
            float baseFactor = launchAngleDeg > 18f ? 0.05f : 0.12f;
            return carryYards * baseFactor;
        }
    }
}