 
using System.Drawing.Drawing2D; 
using TenOver.Ble;
using TenOver.Client;
using TenOver.Exceptions;
using TenOver.proto;

namespace TenOver.WinForm.Example
{
    public partial class SimulatorForm : Form
    {
        // 1. Declare as a Nullable Struct to represent "no shot present yet"
        private SimulationResult ? _currentShot = null;

        private int _frameIndex = 0;
        private bool _isAnimating = false; 

        public SimulatorForm()
        {
            InitializeComponent();
            SetupSimulatorEngine();
        }

        private void SetupSimulatorEngine()
        {
            // Force panelSim to use DoubleBuffering to eliminate GDI+ screen flickering
            typeof(Panel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic,
                null, panelSim, new object[] { true }); 
        }

        private void SimulatorForm_Load(object sender, EventArgs e)
        {
            SetStatus("Connection Status: Disconnected");
        }

        // ── Hardware Connection & Event Handling ─────────────────────────────

        private void btnConnect_ButtonClick(object sender, EventArgs e)
        {
            // Run BLE bluetooth background thread without blocking the UI loop
            Task.Run(() => ConnectToGarminR10());
        }

        /// <summary>
        /// Main hardware event hook. Receives Garmin R10 ShotData and triggers calculations & rendering.
        /// </summary>
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
                             $"Launch Angle: {shot.Ball.LaunchAngle:F1}° | True Spin Axis: {shot.Ball.SpinAxis:F1}°\n" +
                             $"---------------------------------------------------------------------------------\n" +
                             $"Carry Distance : {currentShot.CarryYards:F1} yds\n" +
                             $"Rollout Distance: {currentShot.RolloutYards:F1} yds\n" +
                             $"Total Distance  : {currentShot.TotalYards:F1} yds\n" +
                             $"Side Deviation  : {Math.Abs(currentShot.DeviationYards):F1} yds " +
                             $"({(currentShot.DeviationYards >= 0 ? "Right" : "Left")})";

            SetStatus(metrics);
            // 3. Calculate conversions
            float ballSpeedMph = shot.Ball.BallSpeed * 2.23694f; // m/s to MPH
            float clubSpeedMph = shot.Club != null ? shot.Club.ClubHeadSpeed * 2.23694f : 0f;
            float smashFactor = clubSpeedMph > 0 ? ballSpeedMph / clubSpeedMph : 0f;
            float faceToPath = shot.Club.FaceAngle - shot.Club.PathAngle; // degrees

            // 4. Update each Metric Tile using OnValueUpdated with MetricValueEventArgs
            tileFaceToPath.OnValueUpdated(this, new MetricValueEventArgs($"{faceToPath:F2}"));
            tileClubSpeed.OnValueUpdated(this, new MetricValueEventArgs($"{ballSpeedMph:F1}"));
            tileBallSpeed.OnValueUpdated(this, new MetricValueEventArgs($"{ballSpeedMph:F1}"));
            tileSmashFactor.OnValueUpdated(this, new MetricValueEventArgs($"{smashFactor:F2}"));
            tileCarry.OnValueUpdated(this, new MetricValueEventArgs($"{currentShot.CarryYards:F1}"));
            tileTotalYards.OnValueUpdated(this, new MetricValueEventArgs($"{currentShot.TotalYards:F1}"));

            // 3. Kick off async rendering loop
            await AnimateGarminShotAsync();
        }

        private async Task AnimateGarminShotAsync()
        {
            // Check .HasValue before accessing TrajectoryPoints
            if (_isAnimating || !_currentShot.HasValue || _currentShot.Value.TrajectoryPoints == null)
                return;

            _isAnimating = true;
            _frameIndex = 0;

            var currentShot = _currentShot.Value;

            while (_frameIndex < currentShot.TrajectoryPoints.Count)
            {
                _frameIndex += 3; // Step multiplier for tracer speed
                panelSim.Invalidate(); // Triggers canvas repaint

                await Task.Delay(16); // ~60 FPS delay loop
            }

            _isAnimating = false;
        }

        // ── 3D Perspective Projection & Rendering Engine ───────────────────

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

        private void panelSim_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 1. Guard against null struct or uninitialized trajectory points
            if (!_currentShot.HasValue) return;

            var currentShot = _currentShot.Value;
            if (currentShot.TrajectoryPoints == null || currentShot.TrajectoryPoints.Count == 0) return;

            // --- Calibration Registry (Image reference base: 1920x1080) ---
            const float rawImageWidth = 1920f;
            const float rawImageHeight = 1080f;
            const float rawTeeX = 960f;
            const float rawTeeY = 960f;
            const float raw150Y = 540f;

            // --- Dynamic Scaling for Responsive Resizing ---
            float ratioX = panelSim.Width / rawImageWidth;
            float ratioY = panelSim.Height / rawImageHeight;

            float currentTeeX = rawTeeX * ratioX;
            float currentTeeY = rawTeeY * ratioY;
            float current150Y = raw150Y * ratioY;

            float pixelDistanceTo150 = currentTeeY - current150Y;
            float pixelsPerYard = pixelDistanceTo150 / 150f;

            // --- Draw Ball Flight Arc Line ---
            using (Pen ballPen = new Pen(Color.White, 3f))
            {
                int maxIndex = Math.Min(_frameIndex, currentShot.TrajectoryPoints.Count);
                for (int i = 1; i < maxIndex; i++)
                {
                    var pt1 = currentShot.TrajectoryPoints[i - 1];
                    var pt2 = currentShot.TrajectoryPoints[i];

                    PointF p1 = ProjectToScreen(pt1.X * 1.09361f, pt1.Y, pt1.Z * 1.09361f, currentTeeX, currentTeeY, pixelsPerYard);
                    PointF p2 = ProjectToScreen(pt2.X * 1.09361f, pt2.Y, pt2.Z * 1.09361f, currentTeeX, currentTeeY, pixelsPerYard);

                    g.DrawLine(ballPen, p1, p2);
                }
            }

            // --- Draw Animated Ball Indicator ---
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
            SetStatus("Searching for Garmin R10...");

            WindowsBleTransport transport;
            try
            {
                transport = WindowsBleTransport.AutoConnect();
            }
            catch (Exception ex)
            {
                SetStatus($"Connection failed: {ex.Message}");
                return;
            }

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
                                SetStatus($"Registered handle={r.Handle}");
                                break;

                            case ClientEvent.HandshakeComplete:
                                SetStatus("Handshake complete");
                                break;

                            case ClientEvent.Subscribed:
                            case ClientEvent.WakeUpResponse:
                                break;

                            case ClientEvent.Ready:
                                if (!readyPrinted)
                                {
                                    SetStatus("READY — waiting for shot");
                                    readyPrinted = true;
                                }
                                break;

                            case ClientEvent.StateChange:
                                readyPrinted = false;
                                break;

                            case ClientEvent.DeviceError de:
                                SetStatus($"DEVICE ERROR: {de.Error.Code} ({de.Error.Severity})");
                                if (de.Error.Tilt is { } tilt)
                                    SetStatus($"Tilt: roll={tilt.Roll:F1}° pitch={tilt.Pitch:F1}°");
                                break;

                            case ClientEvent.Shot s:
                                HandleGarminShotData(s.Data);
                                readyPrinted = false;
                                shotCount++;
                                SetStatus($"\n── Shot #{shotCount} (id={s.Data.ShotId}) ──");
                                break;
                        }
                    }
                    catch (TenoverException ex)
                    {
                        SetStatus($"Warning: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        SetStatus($"Fatal error: {ex.Message}");
                        return;
                    }
                }
            }
        }

        /// <summary>
        ///     Thread-safe method to update the status label on the UI. If called from a non-UI thread, it will marshal the call to the UI thread using BeginInvoke.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="backColor"></param>
        private void SetStatus(string text, Color? backColor = null)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SetStatus(text, backColor)));
                return;
            }

            lblStatus.Text = text;
            if (backColor.HasValue)
                lblStatus.BackColor = backColor.Value;
        }
    }
}