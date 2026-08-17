namespace TenOver.WinForm.Example
{
    partial class SimulatorFormAsync
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SimulatorFormAsync));
            statusStrip1 = new StatusStrip();
            btnConnect = new ToolStripSplitButton();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            lblStatus = new ToolStripStatusLabel();
            panelSim = new Panel();
            contextMenuStrip1 = new ContextMenuStrip(components);
            panelMetrics = new Panel();
            tableLayoutPanel1 = new TableLayoutPanel();
            tileFaceToPath = new MetricTileControl();
            tileBallSpeed = new MetricTileControl();
            tileSmashFactor = new MetricTileControl();
            tileCarry = new MetricTileControl();
            tileTotalYards = new MetricTileControl();
            tileClubSpeed = new MetricTileControl();
            statusStrip1.SuspendLayout();
            panelMetrics.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new Size(24, 24);
            statusStrip1.Items.AddRange(new ToolStripItem[] { btnConnect, toolStripStatusLabel1, lblStatus });
            statusStrip1.Location = new Point(0, 462);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new Padding(1, 0, 20, 0);
            statusStrip1.Size = new Size(894, 32);
            statusStrip1.TabIndex = 0;
            statusStrip1.Text = "statusStrip1";
            // 
            // btnConnect
            // 
            btnConnect.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnConnect.DoubleClickEnabled = true;
            btnConnect.DropDownButtonWidth = 0;
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
            toolStripStatusLabel1.Size = new Size(562, 27);
            toolStripStatusLabel1.Spring = true;
            toolStripStatusLabel1.Text = "toolStripStatusLabel1";
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = false;
            lblStatus.BackColor = SystemColors.ActiveBorder;
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(254, 27);
            lblStatus.Text = "Connection Status";
            // 
            // panelSim
            // 
            panelSim.BackgroundImage = (Image)resources.GetObject("panelSim.BackgroundImage");
            panelSim.BackgroundImageLayout = ImageLayout.Stretch;
            panelSim.Dock = DockStyle.Fill;
            panelSim.Location = new Point(0, 0);
            panelSim.Margin = new Padding(4, 5, 4, 5);
            panelSim.Name = "panelSim";
            panelSim.Size = new Size(804, 462);
            panelSim.TabIndex = 2;
            panelSim.Paint += panelSim_Paint;
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
            panelMetrics.Location = new Point(804, 0);
            panelMetrics.Margin = new Padding(4, 5, 4, 5);
            panelMetrics.Name = "panelMetrics";
            panelMetrics.Size = new Size(90, 462);
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
            tableLayoutPanel1.Margin = new Padding(4, 5, 4, 5);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 6;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel1.Size = new Size(90, 462);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // tileFaceToPath
            // 
            tileFaceToPath.BackColor = Color.FromArgb(28, 28, 32);
            tileFaceToPath.Dock = DockStyle.Fill;
            tileFaceToPath.Location = new Point(1, 387);
            tileFaceToPath.Margin = new Padding(1, 2, 1, 2);
            tileFaceToPath.Name = "tileFaceToPath";
            tileFaceToPath.Padding = new Padding(1, 2, 1, 2);
            tileFaceToPath.Size = new Size(88, 73);
            tileFaceToPath.TabIndex = 5;
            tileFaceToPath.Title = "Face To Path";
            tileFaceToPath.UnitOfMeasure = "deg";
            // 
            // tileBallSpeed
            // 
            tileBallSpeed.BackColor = Color.FromArgb(28, 28, 32);
            tileBallSpeed.Dock = DockStyle.Fill;
            tileBallSpeed.Location = new Point(1, 233);
            tileBallSpeed.Margin = new Padding(1, 2, 1, 2);
            tileBallSpeed.Name = "tileBallSpeed";
            tileBallSpeed.Padding = new Padding(1, 2, 1, 2);
            tileBallSpeed.Size = new Size(88, 73);
            tileBallSpeed.TabIndex = 4;
            tileBallSpeed.Title = "Ball Speed";
            tileBallSpeed.UnitOfMeasure = "mph";
            // 
            // tileSmashFactor
            // 
            tileSmashFactor.BackColor = Color.FromArgb(28, 28, 32);
            tileSmashFactor.Dock = DockStyle.Fill;
            tileSmashFactor.Location = new Point(1, 156);
            tileSmashFactor.Margin = new Padding(1, 2, 1, 2);
            tileSmashFactor.Name = "tileSmashFactor";
            tileSmashFactor.Padding = new Padding(1, 2, 1, 2);
            tileSmashFactor.Size = new Size(88, 73);
            tileSmashFactor.TabIndex = 3;
            tileSmashFactor.Title = "Smash Factor";
            tileSmashFactor.UnitOfMeasure = "";
            // 
            // tileCarry
            // 
            tileCarry.BackColor = Color.FromArgb(28, 28, 32);
            tileCarry.Dock = DockStyle.Fill;
            tileCarry.Location = new Point(1, 79);
            tileCarry.Margin = new Padding(1, 2, 1, 2);
            tileCarry.Name = "tileCarry";
            tileCarry.Padding = new Padding(1, 2, 1, 2);
            tileCarry.Size = new Size(88, 73);
            tileCarry.TabIndex = 2;
            tileCarry.Title = "Carry";
            tileCarry.UnitOfMeasure = "yds.";
            // 
            // tileTotalYards
            // 
            tileTotalYards.BackColor = Color.FromArgb(28, 28, 32);
            tileTotalYards.Dock = DockStyle.Fill;
            tileTotalYards.Location = new Point(1, 2);
            tileTotalYards.Margin = new Padding(1, 2, 1, 2);
            tileTotalYards.Name = "tileTotalYards";
            tileTotalYards.Padding = new Padding(1, 2, 1, 2);
            tileTotalYards.Size = new Size(88, 73);
            tileTotalYards.TabIndex = 1;
            tileTotalYards.Title = "Total";
            tileTotalYards.UnitOfMeasure = "yds.";
            // 
            // tileClubSpeed
            // 
            tileClubSpeed.BackColor = Color.FromArgb(28, 28, 32);
            tileClubSpeed.Dock = DockStyle.Fill;
            tileClubSpeed.Location = new Point(1, 310);
            tileClubSpeed.Margin = new Padding(1, 2, 1, 2);
            tileClubSpeed.Name = "tileClubSpeed";
            tileClubSpeed.Padding = new Padding(1, 2, 1, 2);
            tileClubSpeed.Size = new Size(88, 73);
            tileClubSpeed.TabIndex = 0;
            tileClubSpeed.Title = "Club Speed";
            tileClubSpeed.UnitOfMeasure = "mph";
            // 
            // SimulatorFormAsync
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            BackColor = SystemColors.ActiveBorder;
            ClientSize = new Size(894, 494);
            Controls.Add(panelSim);
            Controls.Add(panelMetrics);
            Controls.Add(statusStrip1);
            Margin = new Padding(6, 7, 6, 7);
            Name = "SimulatorFormAsync";
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
        private System.Windows.Forms.Panel panelSim;
        private ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.Panel panelMetrics;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private TenOver.WinForm.Example.MetricTileControl tileClubSpeed;
        private TenOver.WinForm.Example.MetricTileControl tileBallSpeed;
        private TenOver.WinForm.Example.MetricTileControl tileSmashFactor;
        private TenOver.WinForm.Example.MetricTileControl tileCarry;
        private TenOver.WinForm.Example.MetricTileControl tileTotalYards;
        private ToolStripStatusLabel lblStatus;
        private TenOver.WinForm.Example.MetricTileControl tileFaceToPath;
    }
}