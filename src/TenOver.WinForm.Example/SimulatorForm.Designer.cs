namespace TenOver.WinForm.Example
{
    partial class SimulatorForm
    {
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SimulatorForm));
            statusStrip1 = new StatusStrip();
            btnConnect = new ToolStripSplitButton();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            lblStatus = new ToolStripStatusLabel();
            panelSim = new Panel();
            tileSpinAxis = new MetricTileControl();
            tileCarry = new MetricTileControl();
            tileOfflineYds = new MetricTileControl();
            tileApex = new MetricTileControl();
            tileLaunchAnlge = new MetricTileControl();
            tileBackSpin = new MetricTileControl();
            tileSideSpin = new MetricTileControl();
            contextMenuStrip1 = new ContextMenuStrip(components);
            panelMetrics = new Panel();
            tableLayoutPanel1 = new TableLayoutPanel();
            tileFaceToPath = new MetricTileControl();
            tileBallSpeed = new MetricTileControl();
            tileClubSpeed = new MetricTileControl();
            tileSmashFactor = new MetricTileControl();
            tileTotalYards = new MetricTileControl();
            golfRangeCanvas1 = new GolfRangeCanvas();
            statusStrip1.SuspendLayout();
            panelSim.SuspendLayout();
            panelMetrics.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new Size(24, 24);
            statusStrip1.Items.AddRange(new ToolStripItem[] { btnConnect, toolStripStatusLabel1, lblStatus });
            statusStrip1.Location = new Point(0, 644);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new Padding(1, 0, 20, 0);
            statusStrip1.Size = new Size(1229, 32);
            statusStrip1.TabIndex = 0;
            statusStrip1.Text = "statusStrip1";
            // 
            // btnConnect
            // 
            btnConnect.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnConnect.DoubleClickEnabled = true;
            btnConnect.DropDownButtonWidth = 0;
            btnConnect.Image = (Image)resources.GetObject("btnConnect.Image");
            btnConnect.ImageScaling = ToolStripItemImageScaling.None;
            btnConnect.ImageTransparentColor = Color.Magenta;
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(57, 30);
            btnConnect.Text = "Connect";
            btnConnect.ButtonClick += btnConnect_ButtonClick;
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.BackColor = Color.Transparent;
            toolStripStatusLabel1.ForeColor = Color.Transparent;
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(801, 27);
            toolStripStatusLabel1.Spring = true;
            toolStripStatusLabel1.Text = "toolStripStatusLabel1";
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = false;
            lblStatus.BackColor = SystemColors.ActiveBorder;
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(350, 27);
            lblStatus.Text = "Connection Status";
            // 
            // panelSim
            // 
            panelSim.BackgroundImageLayout = ImageLayout.Stretch;
            panelSim.Controls.Add(golfRangeCanvas1);
            panelSim.Dock = DockStyle.Fill;
            panelSim.Location = new Point(0, 0);
            panelSim.Margin = new Padding(4, 5, 4, 5);
            panelSim.Name = "panelSim";
            panelSim.Size = new Size(1028, 644);
            panelSim.TabIndex = 2;
            panelSim.Paint += panelSim_Paint;
            // 
            // tileSpinAxis
            // 
            tileSpinAxis.BackColor = Color.FromArgb(28, 28, 32);
            tileSpinAxis.Dock = DockStyle.Fill;
            tileSpinAxis.Location = new Point(101, 491);
            tileSpinAxis.Margin = new Padding(1);
            tileSpinAxis.Name = "tileSpinAxis";
            tileSpinAxis.Padding = new Padding(1);
            tileSpinAxis.Size = new Size(98, 96);
            tileSpinAxis.TabIndex = 6;
            tileSpinAxis.Title = "Spin Axis";
            tileSpinAxis.UnitOfMeasure = "deg.";
            // 
            // tileCarry
            // 
            tileCarry.BackColor = Color.FromArgb(28, 28, 32);
            tileCarry.Dock = DockStyle.Fill;
            tileCarry.Location = new Point(1, 100);
            tileCarry.Margin = new Padding(1, 2, 1, 2);
            tileCarry.Name = "tileCarry";
            tileCarry.Padding = new Padding(1, 2, 1, 2);
            tileCarry.Size = new Size(98, 94);
            tileCarry.TabIndex = 2;
            tileCarry.Title = "Carry";
            tileCarry.UnitOfMeasure = "yds.";
            // 
            // tileOfflineYds
            // 
            tileOfflineYds.BackColor = Color.FromArgb(28, 28, 32);
            tileOfflineYds.Dock = DockStyle.Fill;
            tileOfflineYds.Location = new Point(1, 1);
            tileOfflineYds.Margin = new Padding(1);
            tileOfflineYds.Name = "tileOfflineYds";
            tileOfflineYds.Padding = new Padding(1);
            tileOfflineYds.Size = new Size(98, 96);
            tileOfflineYds.TabIndex = 4;
            tileOfflineYds.Title = "Offline";
            tileOfflineYds.UnitOfMeasure = "yds";
            // 
            // tileApex
            // 
            tileApex.BackColor = Color.FromArgb(28, 28, 32);
            tileApex.Dock = DockStyle.Fill;
            tileApex.Location = new Point(101, 1);
            tileApex.Margin = new Padding(1);
            tileApex.Name = "tileApex";
            tileApex.Padding = new Padding(1);
            tileApex.Size = new Size(98, 96);
            tileApex.TabIndex = 3;
            tileApex.Title = "Apex";
            tileApex.UnitOfMeasure = "feet";
            // 
            // tileLaunchAnlge
            // 
            tileLaunchAnlge.BackColor = Color.FromArgb(28, 28, 32);
            tileLaunchAnlge.Dock = DockStyle.Fill;
            tileLaunchAnlge.Location = new Point(1, 197);
            tileLaunchAnlge.Margin = new Padding(1);
            tileLaunchAnlge.Name = "tileLaunchAnlge";
            tileLaunchAnlge.Padding = new Padding(1);
            tileLaunchAnlge.Size = new Size(98, 96);
            tileLaunchAnlge.TabIndex = 2;
            tileLaunchAnlge.Title = "Launch Angle";
            // 
            // tileBackSpin
            // 
            tileBackSpin.BackColor = Color.FromArgb(28, 28, 32);
            tileBackSpin.Dock = DockStyle.Fill;
            tileBackSpin.Location = new Point(1, 491);
            tileBackSpin.Margin = new Padding(1);
            tileBackSpin.Name = "tileBackSpin";
            tileBackSpin.Padding = new Padding(1);
            tileBackSpin.Size = new Size(98, 96);
            tileBackSpin.TabIndex = 1;
            tileBackSpin.Title = "Back Spin";
            tileBackSpin.UnitOfMeasure = "rpm";
            // 
            // tileSideSpin
            // 
            tileSideSpin.BackColor = Color.FromArgb(28, 28, 32);
            tileSideSpin.Dock = DockStyle.Fill;
            tileSideSpin.Location = new Point(1, 393);
            tileSideSpin.Margin = new Padding(1);
            tileSideSpin.Name = "tileSideSpin";
            tileSideSpin.Padding = new Padding(1);
            tileSideSpin.Size = new Size(98, 96);
            tileSideSpin.TabIndex = 0;
            tileSideSpin.Title = "Side Spin";
            tileSideSpin.UnitOfMeasure = "rpm";
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.ImageScalingSize = new Size(24, 24);
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(61, 4);
            // 
            // panelMetrics
            // 
            panelMetrics.Controls.Add(tableLayoutPanel1);
            panelMetrics.Dock = DockStyle.Right;
            panelMetrics.Location = new Point(1028, 0);
            panelMetrics.Margin = new Padding(4, 5, 4, 5);
            panelMetrics.Name = "panelMetrics";
            panelMetrics.Size = new Size(201, 644);
            panelMetrics.TabIndex = 1;
            panelMetrics.Paint += panelMetrics_Paint;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Controls.Add(tileSpinAxis, 1, 5);
            tableLayoutPanel1.Controls.Add(tileBackSpin, 0, 5);
            tableLayoutPanel1.Controls.Add(tileLaunchAnlge, 0, 2);
            tableLayoutPanel1.Controls.Add(tileSideSpin, 0, 4);
            tableLayoutPanel1.Controls.Add(tileApex, 1, 0);
            tableLayoutPanel1.Controls.Add(tileFaceToPath, 0, 3);
            tableLayoutPanel1.Controls.Add(tileCarry, 0, 1);
            tableLayoutPanel1.Controls.Add(tileBallSpeed, 1, 4);
            tableLayoutPanel1.Controls.Add(tileClubSpeed, 1, 3);
            tableLayoutPanel1.Controls.Add(tileSmashFactor, 1, 2);
            tableLayoutPanel1.Controls.Add(tileTotalYards, 1, 1);
            tableLayoutPanel1.Controls.Add(tileOfflineYds, 0, 0);
            tableLayoutPanel1.Location = new Point(1, 56);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 6;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel1.Size = new Size(200, 588);
            tableLayoutPanel1.TabIndex = 7;
            // 
            // tileFaceToPath
            // 
            tileFaceToPath.BackColor = Color.FromArgb(28, 28, 32);
            tileFaceToPath.Dock = DockStyle.Fill;
            tileFaceToPath.Location = new Point(1, 296);
            tileFaceToPath.Margin = new Padding(1, 2, 1, 2);
            tileFaceToPath.Name = "tileFaceToPath";
            tileFaceToPath.Padding = new Padding(1, 2, 1, 2);
            tileFaceToPath.Size = new Size(98, 94);
            tileFaceToPath.TabIndex = 5;
            tileFaceToPath.Title = "Face To Path";
            tileFaceToPath.UnitOfMeasure = "deg";
            // 
            // tileBallSpeed
            // 
            tileBallSpeed.BackColor = Color.FromArgb(28, 28, 32);
            tileBallSpeed.Dock = DockStyle.Fill;
            tileBallSpeed.Location = new Point(101, 394);
            tileBallSpeed.Margin = new Padding(1, 2, 1, 2);
            tileBallSpeed.Name = "tileBallSpeed";
            tileBallSpeed.Padding = new Padding(1, 2, 1, 2);
            tileBallSpeed.Size = new Size(98, 94);
            tileBallSpeed.TabIndex = 4;
            tileBallSpeed.Title = "Ball Speed";
            tileBallSpeed.UnitOfMeasure = "mph";
            // 
            // tileClubSpeed
            // 
            tileClubSpeed.BackColor = Color.FromArgb(28, 28, 32);
            tileClubSpeed.Dock = DockStyle.Fill;
            tileClubSpeed.Location = new Point(101, 296);
            tileClubSpeed.Margin = new Padding(1, 2, 1, 2);
            tileClubSpeed.Name = "tileClubSpeed";
            tileClubSpeed.Padding = new Padding(1, 2, 1, 2);
            tileClubSpeed.Size = new Size(98, 94);
            tileClubSpeed.TabIndex = 0;
            tileClubSpeed.Title = "Club Speed";
            tileClubSpeed.UnitOfMeasure = "mph";
            // 
            // tileSmashFactor
            // 
            tileSmashFactor.BackColor = Color.FromArgb(28, 28, 32);
            tileSmashFactor.Dock = DockStyle.Fill;
            tileSmashFactor.Location = new Point(101, 198);
            tileSmashFactor.Margin = new Padding(1, 2, 1, 2);
            tileSmashFactor.Name = "tileSmashFactor";
            tileSmashFactor.Padding = new Padding(1, 2, 1, 2);
            tileSmashFactor.Size = new Size(98, 94);
            tileSmashFactor.TabIndex = 3;
            tileSmashFactor.Title = "Smash Factor";
            tileSmashFactor.UnitOfMeasure = "";
            // 
            // tileTotalYards
            // 
            tileTotalYards.BackColor = Color.FromArgb(28, 28, 32);
            tileTotalYards.Dock = DockStyle.Fill;
            tileTotalYards.Location = new Point(101, 100);
            tileTotalYards.Margin = new Padding(1, 2, 1, 2);
            tileTotalYards.Name = "tileTotalYards";
            tileTotalYards.Padding = new Padding(1, 2, 1, 2);
            tileTotalYards.Size = new Size(98, 94);
            tileTotalYards.TabIndex = 1;
            tileTotalYards.Title = "Total";
            tileTotalYards.UnitOfMeasure = "yds.";
            // 
            // golfRangeCanvas1
            // 
            golfRangeCanvas1.ArcIntensityMultiplier = 1D;
            golfRangeCanvas1.BackgroundImage = Properties.Resources.wide_angle_driving_range_no_lines;
            golfRangeCanvas1.BackgroundImageLayout = ImageLayout.Stretch;
            golfRangeCanvas1.Dock = DockStyle.Fill;
            golfRangeCanvas1.GridFontSize = 14F;
            golfRangeCanvas1.GridPenThickness = 4F;
            golfRangeCanvas1.HorizonYPercent = 0D;
            golfRangeCanvas1.IsLeftHanded = true;
            golfRangeCanvas1.LayDownFlatness = 2D;
            golfRangeCanvas1.LeftEdgePercent = 0.05D;
            golfRangeCanvas1.Location = new Point(0, 0);
            golfRangeCanvas1.Name = "golfRangeCanvas1";
            golfRangeCanvas1.RightEdgePercent = 0.95D;
            golfRangeCanvas1.Size = new Size(1028, 644);
            golfRangeCanvas1.TabIndex = 0;
            golfRangeCanvas1.TeeXPercent = 0.5D;
            golfRangeCanvas1.TeeYPercent = 0.6D;
            // 
            // SimulatorForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.ActiveBorder;
            BackgroundImage = Properties.Resources.wide_angle_driving_range_no_lines;
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(1229, 676);
            Controls.Add(panelSim);
            Controls.Add(panelMetrics);
            Controls.Add(statusStrip1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Margin = new Padding(6, 7, 6, 7);
            Name = "SimulatorForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "DimplePhysics - Custom Golf Simulator Engine";
            Load += SimulatorForm_Load;
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            panelSim.ResumeLayout(false);
            panelMetrics.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        private TenOver.WinForm.Example.MetricTileControl tileSpinAxis;

        private TenOver.WinForm.Example.MetricTileControl tileBackSpin;
        private TenOver.WinForm.Example.MetricTileControl tileLaunchAnlge;
        private TenOver.WinForm.Example.MetricTileControl tileApex;
        private TenOver.WinForm.Example.MetricTileControl tileOfflineYds;

        private TenOver.WinForm.Example.MetricTileControl tileSideSpin;

        #endregion

        private System.Windows.Forms.StatusStrip statusStrip1;
        private ToolStripSplitButton btnConnect;
        private ToolStripStatusLabel toolStripStatusLabel1; 
        private System.Windows.Forms.Panel panelSim;
        private ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.Panel panelMetrics;
        private TenOver.WinForm.Example.MetricTileControl tileClubSpeed;
        private TenOver.WinForm.Example.MetricTileControl tileBallSpeed;
        private TenOver.WinForm.Example.MetricTileControl tileSmashFactor;
        private TenOver.WinForm.Example.MetricTileControl tileCarry;
        private TenOver.WinForm.Example.MetricTileControl tileTotalYards;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private TenOver.WinForm.Example.MetricTileControl tileFaceToPath;
        private TableLayoutPanel tableLayoutPanel1;
        private GolfRangeCanvas golfRangeCanvas1;
    }
}