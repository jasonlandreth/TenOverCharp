using System.Diagnostics;
using System.Drawing.Drawing2D;
using log4net;
using TenOver.Ble;
using TenOver.Client;
using TenOver.Exceptions;
using TenOver.proto;

namespace TenOver.WinForm.Example
{
    public partial class SimulatorForm : Form
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SimulatorForm));

        // 1. Declare as a Nullable Struct to represent "no shot present yet"
        private SimulationResult ? _currentShot = null;

        private int _frameIndex = 0;
        private bool _isAnimating = false; 

        /// <summary>
        /// Initializes a new instance of the <see cref="SimulatorForm"/> class and configures the simulation panel.
        /// </summary>
        public SimulatorForm()
        {
            InitializeComponent();
            SetupSimulatorEngine();
        }

        /// <summary>
        /// Enables double buffering on the simulation panel to eliminate GDI+ flicker during animation.
        /// </summary>
        private void SetupSimulatorEngine()
        {
            // Force panelSim to use DoubleBuffering to eliminate GDI+ screen flickering
            typeof(Panel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic,
                null, panelSim, new object[] { true });
        }

        /// <summary>
        /// Handles the form's Load event by setting the initial connection status text.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="e">Event arguments.</param>
        private void SimulatorForm_Load(object sender, EventArgs e)
        {
            SetStatus("Connection Status: Disconnected");
        }

        // ── Hardware Connection & Event Handling ─────────────────────────────

        /// <summary>
        /// Handles the Connect button click by launching the Garmin R10 BLE connection worker on a background thread.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="e">Event arguments.</param>
        private void btnConnect_ButtonClick(object sender, EventArgs e)
        {
            ShotData shotData = new ShotData
            {
                ShotId = 1,
                ShotType = ShotType.Normal,
                Ball = new BallData
                {
                    BallSpeed = 70.0f, // m/s
                    TotalSpin = 3000.0f, // RPM
                    VerticalLaunchAngle = 12.0f, // degrees
                    HorizontalLaunchDirection = 0.0f, // degrees
                    SpinAxis = 5.0f // degrees
                },
                Club = new ClubData
                {
                    ClubHeadSpeed = 90.0f, // m/s
                    FaceToTarget = 2.0f, // degrees
                    PathToTarget = -1.0f // degrees
                },
                Swing = new SwingData
                {
                   // SwingTempo = 3.0f, // seconds
                   // SwingPlaneAngle = 45.0f // degrees
                }
            };
            // Run BLE bluetooth background thread without blocking the UI loop
            Task.Run(() => ConnectToGarminR10());
        }

        /// <summary>
        /// Main hardware event hook. Receives Garmin R10 ShotData and triggers calculations & rendering.
        /// </summary>
        /// <param name="shot">The shot data received from the Garmin R10 device.</param>
        public async void HandleGarminShotData(ShotData shot)
        {
            // Verify safe Cross-Thread execution handshakes
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => HandleGarminShotData(shot)));
                return;
            }

            if (shot?.Ball == null)
            {
                log.Warn("Malformed Garmin data packet received: Missing Ball metrics.");
                SetStatus("Malformed Garmin data packet received: Missing Ball metrics.");
                return;
            }

            // 1. Process physics and store inside nullable struct
            _currentShot = GolfBallFlightEngine.ProcessGarminShot(shot);

            // 2. Extract inner struct safely
            var currentShot = _currentShot.Value;
            float speedMph = shot.Ball.BallSpeed * 2.23694f; // m/s to MPH

            string metrics = $"[Garmin R10 Event Handled - Shot ID: {shot.ShotId}]\n" +
                             $"Ball Speed: {shot.Ball.BallSpeed:F1} m/s ({speedMph:F0} MPH) | Total Spin: {shot.Ball.TotalSpin:F0} RPM\n" +
                             $"Launch Angle: {shot.Ball.VerticalLaunchAngle:F1}° | True Spin Axis: {shot.Ball.SpinAxis:F1}°\n" +
                             $"---------------------------------------------------------------------------------\n" +
                             $"Carry Distance : {currentShot.CarryYards:F1} yds\n" +
                             $"Rollout Distance: {currentShot.RolloutYards:F1} yds\n" +
                             $"Total Distance  : {currentShot.TotalYards:F1} yds\n" +
                             $"Side Deviation  : {Math.Abs(currentShot.DeviationYards):F1} yds " +
                             $"({(currentShot.DeviationYards >= 0 ? "Right" : "Left")})";

            log.Info($"Shot {shot.ShotId} processed: carry={currentShot.CarryYards:F1}yds, total={currentShot.TotalYards:F1}yds, ballSpeed={speedMph:F0}mph");
            SetStatus(metrics);
            // 3. Calculate conversions
            if (shot?.Club == null)
            {
                log.Warn("Missing Club metrics in Garmin shot data.");
                SetStatus("Warning: Missing Club metrics in Garmin shot data.");
                return;
            }
            float ballSpeedMph = shot.Ball.BallSpeed * 2.23694f; // m/s to MPH
            float clubSpeedMph = shot.Club != null ? shot.Club.ClubHeadSpeed * 2.23694f : 0f;
            float smashFactor = clubSpeedMph > 0 ? ballSpeedMph / clubSpeedMph : 0f;
            if (shot.Club != null)
            {
                float faceToPath = shot.Club.FaceToTarget - shot.Club.PathToTarget; // degrees

                log.Debug($"Calculated Metrics: FaceToPath={faceToPath:F2}°, ClubSpeed={clubSpeedMph:F1} MPH, BallSpeed={ballSpeedMph:F1} MPH, SmashFactor={smashFactor:F2}");
                // 4. Update each Metric Tile using OnValueUpdated with MetricValueEventArgs
                tileFaceToPath.OnValueUpdated(this, new MetricValueEventArgs($"{faceToPath:F2}"));
            }
            var awesome =MetricCalculator.ProcessGarminShotToAwesomeMetrics(shot, currentShot);
            log.Debug($"speedMph={speedMph}, launchAngleDeg={shot.Ball.VerticalLaunchAngle}, launchDirectionDeg={shot.Ball.HorizontalLaunchDirection}, totalSpinRpm={shot.Ball.TotalSpin}");
            tileClubSpeed.OnValueUpdated(this, new MetricValueEventArgs($"{ballSpeedMph:F1}"));
            tileBallSpeed.OnValueUpdated(this, new MetricValueEventArgs($"{ballSpeedMph:F1}"));
            tileSmashFactor.OnValueUpdated(this, new MetricValueEventArgs($"{smashFactor:F2}"));
            tileCarry.OnValueUpdated(this, new MetricValueEventArgs($"{currentShot.CarryYards:F1}"));
            tileTotalYards.OnValueUpdated(this, new MetricValueEventArgs($"{currentShot.TotalYards:F1}")); 
            tileBackSpin.OnValueUpdated(this, new MetricValueEventArgs($"{awesome.BackSpin:F1}"));
            tileOfflineYds.OnValueUpdated(this, new MetricValueEventArgs($"{awesome.OfflineYards:F1}"));
            tileApex.OnValueUpdated(this, new MetricValueEventArgs($"{awesome.ApexHeightFeet:F1}"));
            tileLaunchAnlge.OnValueUpdated(this, new MetricValueEventArgs($"{awesome.LaunchAngle:F2}"));
            tileSpinAxis.OnValueUpdated(this, new MetricValueEventArgs($"{awesome.SpinAxis:F1}"));
            tileSideSpin.OnValueUpdated(this, new MetricValueEventArgs($"{awesome.SideSpin:F1}"));
            // 3. Kick off async rendering loop
            await AnimateGarminShotAsync();
        }

        /// <summary>
        /// Advances <see cref="_frameIndex"/> through the current shot's trajectory points at roughly 60 FPS,
        /// invalidating the simulation panel each step so <see cref="panelSim_Paint"/> redraws the animated ball flight.
        /// </summary>
        /// <returns>A task that completes once the animation reaches the final trajectory point.</returns>
        private async Task AnimateGarminShotAsync()
        {
            if (_isAnimating || !_currentShot.HasValue || _currentShot.Value.TrajectoryPoints == null)
            {
                return;
            }

            _isAnimating = true;
            _frameIndex = 0;

            var currentShot = _currentShot.Value;
            int lastIndex = currentShot.TrajectoryPoints.Count - 1;

            while (_frameIndex < lastIndex)
            {
                _frameIndex = Math.Min(_frameIndex + 3, lastIndex); // clamp so it always lands exactly on the final point
                panelSim.Invalidate();
                await Task.Delay(16);
            }

            _isAnimating = false;
        }
        // ── 3D Perspective Projection & Rendering Engine ───────────────────

        /// <summary>
        /// Projects a trajectory point's downrange distance, height, and side deviation into 2D screen coordinates,
        /// applying a simple distance-based perspective scale.
        /// </summary>
        /// <param name="distanceYds">Downrange distance from the tee, in yards.</param>
        /// <param name="heightMeters">Height of the ball above the ground, in meters.</param>
        /// <param name="deviationYds">Side deviation from the target line, in yards.</param>
        /// <param name="currentTeeX">The tee's X pixel position for the current panel size.</param>
        /// <param name="currentTeeY">The tee's Y pixel position for the current panel size.</param>
        /// <param name="pixelsPerYard">The current scale factor, in pixels per yard.</param>
        /// <returns>The projected screen-space point.</returns>
        public PointF ProjectToScreen(float distanceYds, float heightMeters, float deviationYds, float currentTeeX, float currentTeeY, float pixelsPerYard)
        {
            // Clamped perspective scale factor
            float perspectiveScale = Math.Max(0.15f, 1.0f - (distanceYds * 0.0020f));

            // Downrange travel in pixels
            float forwardPixels = distanceYds * pixelsPerYard;

            // X Position
            float screenX = currentTeeX + (deviationYds * pixelsPerYard * perspectiveScale);

            // Y Position
            float heightYards = heightMeters * 1.09361f;
            float heightPixels = heightYards * pixelsPerYard * 1.8f * perspectiveScale;
            float screenY = currentTeeY - forwardPixels - heightPixels;

            return new PointF(screenX, screenY);
        }

        /// <summary>
        /// Paints the ball flight trail and animated ball indicator onto the simulation panel,
        /// using the calibrated tee position and yard-to-pixel scale for the current background image.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="e">Paint event arguments containing the drawing surface.</param>
        private void panelSim_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (!_currentShot.HasValue)
            {
                return;
            }

            var currentShot = _currentShot.Value;
            if (currentShot.TrajectoryPoints == null || currentShot.TrajectoryPoints.Count == 0)
            {
                return;
            }

            // --- Calibration Registry (measured directly from background art at 1920x1080) ---
            const float rawImageWidth = 1920f;
            const float rawImageHeight = 1080f;
            const float rawTeeX = 821f;
            const float rawTeeY = 866f;
            const float raw150Y = 418f;

            float ratioX = panelSim.Width / rawImageWidth;
            float ratioY = panelSim.Height / rawImageHeight;

            float currentTeeX = rawTeeX * ratioX;
            float currentTeeY = rawTeeY * ratioY;
            float current150Y = raw150Y * ratioY;

            float pixelDistanceTo150 = currentTeeY - current150Y;
            float pixelsPerYard = pixelDistanceTo150 / 150f;

            using (Pen ballPen = new Pen(Color.White, 3f))
            {
                int maxIndex = Math.Min(_frameIndex, currentShot.TrajectoryPoints.Count - 1);
                for (int i = 1; i <= maxIndex; i++)
                {
                    var pt1 = currentShot.TrajectoryPoints[i - 1];
                    var pt2 = currentShot.TrajectoryPoints[i];

                    PointF p1 = ProjectToScreen(pt1.X * 1.09361f, pt1.Y, pt1.Z * 1.09361f, currentTeeX, currentTeeY, pixelsPerYard);
                    PointF p2 = ProjectToScreen(pt2.X * 1.09361f, pt2.Y, pt2.Z * 1.09361f, currentTeeX, currentTeeY, pixelsPerYard);

                    g.DrawLine(ballPen, p1, p2);
                }
            }

            if (_frameIndex < currentShot.TrajectoryPoints.Count)
            {
                var pos = currentShot.TrajectoryPoints[_frameIndex];
                float distYds = pos.X * 1.09361f;

                PointF ballPt = ProjectToScreen(distYds, pos.Y, pos.Z * 1.09361f, currentTeeX, currentTeeY, pixelsPerYard);

                float scale = Math.Max(0.2f, 1.0f - (distYds * 0.0020f));
                float ballSize = Math.Max(4f, 12f * scale);

                g.FillEllipse(Brushes.Crimson, ballPt.X - (ballSize / 2f), ballPt.Y - (ballSize / 2f), ballSize, ballSize);
            }
        }

        /// <summary>
        /// Reserved paint handler for the metrics panel. Currently unused.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="e">Paint event arguments containing the drawing surface.</param>
        private void panelMetrics_Paint(object sender, PaintEventArgs e)
        {
            // Optional custom metric panel painting
        }

        /// <summary>
        ///    Bluetooth Connection Worker
        ///    Connects to the Garmin R10 device via Bluetooth Low Energy (BLE) using the Windows BLE transport. 
        ///    This method runs in a background thread and handles the connection lifecycle, 
        ///    including searching for the device, establishing a connection, and processing incoming events such as shot data.
        ///    It updates the UI with status messages and handles exceptions gracefully.
        /// </summary>
        private void ConnectToGarminR10()
        {
            log.Info("Searching for Garmin R10...");
            SetStatus("Searching for Garmin R10...");

            WindowsBleTransport transport;
            try
            {
                transport = WindowsBleTransport.AutoConnect();
            }
            catch (Exception ex)
            {
                log.Error("Connection to Garmin R10 failed", ex);
                SetStatus($"Connection failed: {ex.Message}");
                return;
            }

            log.Info($"Connected: {transport.DeviceAddress} ({transport.DeviceName})");
            SetStatus($"Connected: {transport.DeviceAddress} ({transport.DeviceName})");

            using (transport)
            {
                var client = new Client.Client(transport, mtu: 20);
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
                                log.Debug($"Registered handle={r.Handle}");
                                SetStatus($"Registered handle={r.Handle}");
                                break;

                            case ClientEvent.HandshakeComplete:
                                log.Info("Handshake complete");
                                SetStatus("Handshake complete");
                                break;

                            case ClientEvent.Subscribed:
                            case ClientEvent.WakeUpResponse:
                                break;

                            case ClientEvent.Ready:
                                if (!readyPrinted)
                                {
                                    log.Info("READY — waiting for shot");
                                    SetStatus("READY — waiting for shot");
                                    readyPrinted = true;
                                }
                                break;

                            case ClientEvent.StateChange:
                                readyPrinted = false;
                                break;

                            case ClientEvent.DeviceError de:
                                log.Error($"DEVICE ERROR: {de.Error.Code} ({de.Error.Severity})");
                                SetStatus($"DEVICE ERROR: {de.Error.Code} ({de.Error.Severity})");
                                if (de.Error.Tilt is { } tilt)
                                {
                                    log.Warn($"Tilt: roll={tilt.Roll:F1}° pitch={tilt.Pitch:F1}°");
                                    SetStatus($"Tilt: roll={tilt.Roll:F1}° pitch={tilt.Pitch:F1}°");
                                }
                                break;

                            case ClientEvent.Shot s:
                                HandleGarminShotData(s.Data);
                                readyPrinted = false;
                                shotCount++;
                                log.Info($"Shot #{shotCount} (id={s.Data.ShotId})");
                                SetStatus($"\n── Shot #{shotCount} (id={s.Data.ShotId}) ──");
                                break;
                        }
                    }
                    catch (TenoverException ex)
                    {
                        log.Warn($"Recoverable error: {ex.Message}", ex);
                        SetStatus($"Warning: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        log.Fatal("Fatal error in Garmin R10 connection loop", ex);
                        SetStatus($"Fatal error: {ex.Message}");
                        return;
                    }
                }
            }
        }

        /// <summary>
        ///     Thread-safe method to update the status label on the UI. If called from a non-UI thread, it will marshal the call to the UI thread using BeginInvoke.
        /// </summary>
        /// <param name="text">The status message to display.</param>
        /// <param name="backColor">Optional background color to apply to the status label.</param>
        private void SetStatus(string text, Color? backColor = null)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SetStatus(text, backColor)));
                return;
            }

            lblStatus.Text = text;
            if (backColor.HasValue)
            {
                lblStatus.BackColor = backColor.Value;
            }
        }

        private void metricTileControl2_Load(object sender, EventArgs e)
        {
            throw new System.NotImplementedException();
        }
    }
}
