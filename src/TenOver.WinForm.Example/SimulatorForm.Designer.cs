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
            contextMenuStrip1 = new ContextMenuStrip(components);
            panelMetrics = new Panel();
            tableLayoutPanel1 = new TableLayoutPanel();
            tileBallSpeed = new MetricTileControl();
            tileSmashFactor = new MetricTileControl();
            tileCarry = new MetricTileControl();
            tileTotalYards = new MetricTileControl();
            tileClubSpeed = new MetricTileControl();
            tileFaceToPath = new MetricTileControl();
            statusStrip1.SuspendLayout();
            panelMetrics.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { btnConnect, toolStripStatusLabel1, lblStatus });
            statusStrip1.Location = new Point(0, 626);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(1042, 22);
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
            btnConnect.Size = new Size(57, 20);
            btnConnect.Text = "Connect";
            btnConnect.ButtonClick += btnConnect_ButtonClick;
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.BackColor = Color.Transparent;
            toolStripStatusLabel1.ForeColor = Color.Transparent;
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(716, 17);
            toolStripStatusLabel1.Spring = true;
            toolStripStatusLabel1.Text = "toolStripStatusLabel1";
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = false;
            lblStatus.BackColor = SystemColors.ActiveBorder;
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(254, 17);
            lblStatus.Text = "Connection Status";
            // 
            // panelSim
            // 
            panelSim.BackgroundImage = Properties.Resources.driving_range_with_side_lines_with_numbers;
            panelSim.BackgroundImageLayout = ImageLayout.Stretch;
            panelSim.Dock = DockStyle.Fill;
            panelSim.Location = new Point(0, 0);
            panelSim.Name = "panelSim";
            panelSim.Size = new Size(920, 626);
            panelSim.TabIndex = 2;
            panelSim.Paint += panelSim_Paint;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(61, 4);
            // 
            // panelMetrics
            // 
            panelMetrics.Controls.Add(tableLayoutPanel1);
            panelMetrics.Dock = DockStyle.Right;
            panelMetrics.Location = new Point(920, 0);
            panelMetrics.Name = "panelMetrics";
            panelMetrics.Size = new Size(122, 626);
            panelMetrics.TabIndex = 1;
            panelMetrics.Paint += panelMetrics_Paint;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(tileFaceToPath, 0, 5);
            tableLayoutPanel1.Controls.Add(tileBallSpeed, 0, 3);
            tableLayoutPanel1.Controls.Add(tileSmashFactor, 0, 2);
            tableLayoutPanel1.Controls.Add(tileCarry, 0, 1);
            tableLayoutPanel1.Controls.Add(tileTotalYards, 0, 0);
            tableLayoutPanel1.Controls.Add(tileClubSpeed, 0, 4);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 6;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel1.Size = new Size(122, 626);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // tileBallSpeed
            // 
            tileBallSpeed.BackColor = Color.FromArgb(28, 28, 32);
            tileBallSpeed.Dock = DockStyle.Fill;
            tileBallSpeed.Location = new Point(1, 313);
            tileBallSpeed.Margin = new Padding(1);
            tileBallSpeed.Name = "tileBallSpeed";
            tileBallSpeed.Padding = new Padding(1);
            tileBallSpeed.Size = new Size(120, 102);
            tileBallSpeed.TabIndex = 4;
            tileBallSpeed.Title = "Ball Speed";
            tileBallSpeed.UnitOfMeasure = "mph";
            // 
            // tileSmashFactor
            // 
            tileSmashFactor.BackColor = Color.FromArgb(28, 28, 32);
            tileSmashFactor.Dock = DockStyle.Fill;
            tileSmashFactor.Location = new Point(1, 209);
            tileSmashFactor.Margin = new Padding(1);
            tileSmashFactor.Name = "tileSmashFactor";
            tileSmashFactor.Padding = new Padding(1);
            tileSmashFactor.Size = new Size(120, 102);
            tileSmashFactor.TabIndex = 3;
            tileSmashFactor.Title = "Smash Factor";
            tileSmashFactor.UnitOfMeasure = "";
            // 
            // tileCarry
            // 
            tileCarry.BackColor = Color.FromArgb(28, 28, 32);
            tileCarry.Dock = DockStyle.Fill;
            tileCarry.Location = new Point(1, 105);
            tileCarry.Margin = new Padding(1);
            tileCarry.Name = "tileCarry";
            tileCarry.Padding = new Padding(1);
            tileCarry.Size = new Size(120, 102);
            tileCarry.TabIndex = 2;
            tileCarry.Title = "Carry";
            tileCarry.UnitOfMeasure = "yds.";
            // 
            // tileTotalYards
            // 
            tileTotalYards.BackColor = Color.FromArgb(28, 28, 32);
            tileTotalYards.Dock = DockStyle.Fill;
            tileTotalYards.Location = new Point(1, 1);
            tileTotalYards.Margin = new Padding(1);
            tileTotalYards.Name = "tileTotalYards";
            tileTotalYards.Padding = new Padding(1);
            tileTotalYards.Size = new Size(120, 102);
            tileTotalYards.TabIndex = 1;
            tileTotalYards.Title = "Total";
            tileTotalYards.UnitOfMeasure = "yds.";
            // 
            // tileClubSpeed
            // 
            tileClubSpeed.BackColor = Color.FromArgb(28, 28, 32);
            tileClubSpeed.Dock = DockStyle.Fill;
            tileClubSpeed.Location = new Point(1, 417);
            tileClubSpeed.Margin = new Padding(1);
            tileClubSpeed.Name = "tileClubSpeed";
            tileClubSpeed.Padding = new Padding(1);
            tileClubSpeed.Size = new Size(120, 102);
            tileClubSpeed.TabIndex = 0;
            tileClubSpeed.Title = "Club Speed";
            tileClubSpeed.UnitOfMeasure = "mph";
            // 
            // tileFaceToPath
            // 
            tileFaceToPath.BackColor = Color.FromArgb(28, 28, 32);
            tileFaceToPath.Dock = DockStyle.Fill;
            tileFaceToPath.Location = new Point(1, 521);
            tileFaceToPath.Margin = new Padding(1);
            tileFaceToPath.Name = "tileFaceToPath";
            tileFaceToPath.Padding = new Padding(1);
            tileFaceToPath.Size = new Size(120, 104);
            tileFaceToPath.TabIndex = 5;
            tileFaceToPath.Title = "Face To Path";
            tileFaceToPath.UnitOfMeasure = "deg";
            // 
            // SimulatorForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.ActiveBorder;
            ClientSize = new Size(1042, 648);
            Controls.Add(panelSim);
            Controls.Add(panelMetrics);
            Controls.Add(statusStrip1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Margin = new Padding(4);
            MaximizeBox = false;
            Name = "SimulatorForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "DimplePhysics - Custom Golf Simulator Engine";
            Load += SimulatorForm_Load;
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            panelMetrics.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private StatusStrip statusStrip1;
        private ToolStripSplitButton btnConnect;
        private ToolStripStatusLabel toolStripStatusLabel1; 
        private Panel panelSim;
        private ContextMenuStrip contextMenuStrip1;
        private Panel panelMetrics;
        private TableLayoutPanel tableLayoutPanel1;
        private MetricTileControl tileClubSpeed;
        private MetricTileControl tileBallSpeed;
        private MetricTileControl tileSmashFactor;
        private MetricTileControl tileCarry;
        private MetricTileControl tileTotalYards;
        private ToolStripStatusLabel lblStatus;
        private MetricTileControl tileFaceToPath;
    }
}