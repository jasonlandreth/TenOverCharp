using System.Diagnostics;
using System.Drawing.Drawing2D;
using TenOver.Ble;
using TenOver.Client;
using TenOver.Exceptions;
using TenOver.proto;

namespace TenOver.WinForm.Example
{
    public partial class SimulatorFormAsync : Form
    {
        // 1. Declare as a Nullable Struct to represent "no shot present yet"
        private SimulationResult? _currentShot = null;

        private int _frameIndex = 0;
        private bool _isAnimating = false;

        private IBleTransport? _transport;
        private CancellationTokenSource? _pollCts;
        private Task? _pollTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimulatorFormAsync"/> class and configures the simulation panel.
        /// </summary>
        public SimulatorFormAsync()
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
        /// Handles the Connect button click by awaiting the Garmin R10 connection directly on the UI thread.
        /// The connect itself is a real async method, so no Task.Run is needed here — only the background
        /// poll loop started inside <see cref="ConnectToGarminR10Async"/> runs off the UI thread.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="e">Event arguments.</param>
        private async void btnConnect_ButtonClick(object sender, EventArgs e)
        {
            // Await the connect directly on the UI thread — this is what
            // keeps the form responsive, since WindowsBleTransportAsync /
            // UniversalBleTransportAsync are real async methods (no
            // blocking .GetAwaiter().GetResult() inside them). Only the
            // Poll() loop below gets pushed onto a background task; the
            // connect itself doesn't need Task.Run.
            btnConnect.Enabled = false;
            await ConnectToGarminR10Async();
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
            // 4. Update each Metric Tile using the new UpdateValue wrapper which
            // ensures UI-thread marshalling. Use correct metric values (club vs ball).
            Debug.WriteLine($"Calculated Metrics: FaceToPath={faceToPath:F2}°, ClubSpeed={clubSpeedMph:F1} MPH, BallSpeed={ballSpeedMph:F1} MPH, SmashFactor={smashFactor:F2}");
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
        ///    Bluetooth Connection Worker (async).
        ///    Connects to the Garmin R10 device via Bluetooth Low Energy (BLE),
        ///    then hands the actual Poll() loop off to a background task so
        ///    the UI thread stays responsive. Await this directly from a UI
        ///    event handler — do not wrap it in Task.Run.
        /// </summary>
        /// <returns>A task that completes once the connection has closed and cleanup has finished.</returns>
        private async Task ConnectToGarminR10Async()
        {
            SetStatus("Searching for Garmin R10...");

            try
            {
                // ── Pick ONE of the two lines below ──────────────────────
                // Windows-native transport (WinRT BLE, automatic pairing):
                //_transport = await WindowsBleTransportAsync.AutoConnectAsync();

                // Cross-platform transport (Windows/macOS/Linux via
                // InTheHand.Bluetooth), with automatic Settings-opening
                // fallback if the device has never been paired:
                //
                //_transport = await UniversalBleTransportAsync.AutoConnectAsync();
            }
            catch (Exception ex)
            {
                SetStatus($"Connection failed: {ex.Message}");
                btnConnect.Enabled = true;
                return;
            }

            SetStatus($"Connected: {_transport.DeviceAddress} ({_transport.DeviceName})");

            var client = new TenOver.Client.Client(_transport, mtu: 20);
            client.Start();

            _pollCts = new CancellationTokenSource();
            var ct = _pollCts.Token;

            // Poll() runs on a background task, not the UI thread — an
            // inline while-loop here would freeze the form for the whole
            // session, since nothing else in this method yields back to
            // the message pump once the loop starts.
            _pollTask = Task.Run(() => PollLoop(client, ct), ct);

            try
            {
                await _pollTask;
            }
            catch (OperationCanceledException)
            {
                // Expected on disconnect/close — not an error.
            }
            finally
            {
                _transport?.Dispose();
                _transport = null;
                SetStatus("Connection Status: Disconnected");
                btnConnect.Enabled = true;
            }
        }

        /// <summary>
        /// Background polling loop — pulls decoded protocol events off the
        /// client and reacts to them. Runs on a Task.Run background thread;
        /// SetStatus and HandleGarminShotData both marshal back to the UI
        /// thread themselves, so it's safe to call them directly from here.
        /// </summary>
        /// <param name="client">The connected protocol client to poll for events.</param>
        /// <param name="ct">Cancellation token signaled when the poll loop should stop.</param>
        private void PollLoop(Client.Client client, CancellationToken ct)
        {
            int shotCount = 0;
            bool readyPrinted = false;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var ev = client.Poll();
                    if (ev is null)
                    {
                        Thread.Sleep(5); // fine here — background thread, not the UI thread
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
                            string tiltInfo = de.Error.Tilt is { } tilt
                                ? $"  (tilt: roll={tilt.Roll:F1}° pitch={tilt.Pitch:F1}°)"
                                : string.Empty;
                            SetStatus($"DEVICE ERROR: {de.Error.Code} ({de.Error.Severity}){tiltInfo}");
                            break;

                        case ClientEvent.Shot s:
                            HandleGarminShotData(s.Data);
                            readyPrinted = false;
                            shotCount++;
                            SetStatus($"Shot #{shotCount} (id={s.Data.ShotId})");
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

        /// <summary>
        /// Handles the Disconnect button click by signaling the background poll loop to stop.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="e">Event arguments.</param>
        private void btnDisconnect_ButtonClick(object sender, EventArgs e)
        {
            StopPollingAndDisconnect();
        }

        /// <summary>
        /// Signals the background poll loop to stop. The actual disposal and
        /// status reset happen in ConnectToGarminR10Async's finally block
        /// once the loop has actually exited.
        /// </summary>
        private void StopPollingAndDisconnect()
        {
            _pollCts?.Cancel();
        }

        /// <summary>
        /// Ensures the background poll loop is signaled to stop when the form is closing,
        /// so the connection is cleaned up rather than left running.
        /// </summary>
        /// <param name="e">Form-closing event arguments.</param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopPollingAndDisconnect();
            base.OnFormClosing(e);
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
    }
}
