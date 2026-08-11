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
            statusStrip1 = new System.Windows.Forms.StatusStrip();
            btnConnect = new System.Windows.Forms.ToolStripSplitButton();
            toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            panelSim = new System.Windows.Forms.Panel();
            contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(components);
            panelMetrics = new System.Windows.Forms.Panel();
            tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            tileFaceToPath = new TenOver.WinForm.Example.MetricTileControl();
            tileBallSpeed = new TenOver.WinForm.Example.MetricTileControl();
            tileSmashFactor = new TenOver.WinForm.Example.MetricTileControl();
            tileCarry = new TenOver.WinForm.Example.MetricTileControl();
            tileTotalYards = new TenOver.WinForm.Example.MetricTileControl();
            tileClubSpeed = new TenOver.WinForm.Example.MetricTileControl();
            statusStrip1.SuspendLayout();
            panelMetrics.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { btnConnect, toolStripStatusLabel1, lblStatus });
            statusStrip1.Location = new System.Drawing.Point(0, 462);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new System.Windows.Forms.Padding(1, 0, 20, 0);
            statusStrip1.Size = new System.Drawing.Size(894, 32);
            statusStrip1.TabIndex = 0;
            statusStrip1.Text = "statusStrip1";
            // 
            // btnConnect
            // 
            btnConnect.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            btnConnect.DoubleClickEnabled = true;
            btnConnect.DropDownButtonWidth = 0;
            btnConnect.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            btnConnect.ImageTransparentColor = System.Drawing.Color.Magenta;
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new System.Drawing.Size(57, 30);
            btnConnect.Text = "Connect";
            btnConnect.ButtonClick += btnConnect_ButtonClick;
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.BackColor = System.Drawing.Color.Transparent;
            toolStripStatusLabel1.ForeColor = System.Drawing.Color.Transparent;
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new System.Drawing.Size(562, 27);
            toolStripStatusLabel1.Spring = true;
            toolStripStatusLabel1.Text = "toolStripStatusLabel1";
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = false;
            lblStatus.BackColor = System.Drawing.SystemColors.ActiveBorder;
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new System.Drawing.Size(254, 27);
            lblStatus.Text = "Connection Status";
            // 
            // panelSim
            // 
            panelSim.BackgroundImage = ((System.Drawing.Image)resources.GetObject("panelSim.BackgroundImage"));
            panelSim.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            panelSim.Dock = System.Windows.Forms.DockStyle.Fill;
            panelSim.Location = new System.Drawing.Point(0, 0);
            panelSim.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            panelSim.Name = "panelSim";
            panelSim.Size = new System.Drawing.Size(804, 462);
            panelSim.TabIndex = 2;
            panelSim.Paint += panelSim_Paint;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // panelMetrics
            // 
            panelMetrics.Controls.Add(tableLayoutPanel1);
            panelMetrics.Dock = System.Windows.Forms.DockStyle.Right;
            panelMetrics.Location = new System.Drawing.Point(804, 0);
            panelMetrics.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            panelMetrics.Name = "panelMetrics";
            panelMetrics.Size = new System.Drawing.Size(90, 462);
            panelMetrics.TabIndex = 1;
            panelMetrics.Paint += panelMetrics_Paint;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(tileFaceToPath, 0, 5);
            tableLayoutPanel1.Controls.Add(tileBallSpeed, 0, 3);
            tableLayoutPanel1.Controls.Add(tileSmashFactor, 0, 2);
            tableLayoutPanel1.Controls.Add(tileCarry, 0, 1);
            tableLayoutPanel1.Controls.Add(tileTotalYards, 0, 0);
            tableLayoutPanel1.Controls.Add(tileClubSpeed, 0, 4);
            tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 6;
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.666666F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.666666F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.666666F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.666666F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.666666F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.666666F));
            tableLayoutPanel1.Size = new System.Drawing.Size(90, 462);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // tileFaceToPath
            // 
            tileFaceToPath.BackColor = System.Drawing.Color.FromArgb(((int)((byte)28)), ((int)((byte)28)), ((int)((byte)32)));
            tileFaceToPath.Dock = System.Windows.Forms.DockStyle.Fill;
            tileFaceToPath.Location = new System.Drawing.Point(1, 387);
            tileFaceToPath.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tileFaceToPath.Name = "tileFaceToPath";
            tileFaceToPath.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tileFaceToPath.Size = new System.Drawing.Size(88, 73);
            tileFaceToPath.TabIndex = 5;
            tileFaceToPath.Title = "Face To Path";
            tileFaceToPath.UnitOfMeasure = "deg";
            // 
            // tileBallSpeed
            // 
            tileBallSpeed.BackColor = System.Drawing.Color.FromArgb(((int)((byte)28)), ((int)((byte)28)), ((int)((byte)32)));
            tileBallSpeed.Dock = System.Windows.Forms.DockStyle.Fill;
            tileBallSpeed.Location = new System.Drawing.Point(1, 233);
            tileBallSpeed.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tileBallSpeed.Name = "tileBallSpeed";
            tileBallSpeed.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tileBallSpeed.Size = new System.Drawing.Size(88, 73);
            tileBallSpeed.TabIndex = 4;
            tileBallSpeed.Title = "Ball Speed";
            tileBallSpeed.UnitOfMeasure = "mph";
            // 
            // tileSmashFactor
            // 
            tileSmashFactor.BackColor = System.Drawing.Color.FromArgb(((int)((byte)28)), ((int)((byte)28)), ((int)((byte)32)));
            tileSmashFactor.Dock = System.Windows.Forms.DockStyle.Fill;
            tileSmashFactor.Location = new System.Drawing.Point(1, 156);
            tileSmashFactor.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tileSmashFactor.Name = "tileSmashFactor";
            tileSmashFactor.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tileSmashFactor.Size = new System.Drawing.Size(88, 73);
            tileSmashFactor.TabIndex = 3;
            tileSmashFactor.Title = "Smash Factor";
            tileSmashFactor.UnitOfMeasure = "";
            // 
            // tileCarry
            // 
            tileCarry.BackColor = System.Drawing.Color.FromArgb(((int)((byte)28)), ((int)((byte)28)), ((int)((byte)32)));
            tileCarry.Dock = System.Windows.Forms.DockStyle.Fill;
            tileCarry.Location = new System.Drawing.Point(1, 79);
            tileCarry.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tileCarry.Name = "tileCarry";
            tileCarry.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tileCarry.Size = new System.Drawing.Size(88, 73);
            tileCarry.TabIndex = 2;
            tileCarry.Title = "Carry";
            tileCarry.UnitOfMeasure = "yds.";
            // 
            // tileTotalYards
            // 
            tileTotalYards.BackColor = System.Drawing.Color.FromArgb(((int)((byte)28)), ((int)((byte)28)), ((int)((byte)32)));
            tileTotalYards.Dock = System.Windows.Forms.DockStyle.Fill;
            tileTotalYards.Location = new System.Drawing.Point(1, 2);
            tileTotalYards.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tileTotalYards.Name = "tileTotalYards";
            tileTotalYards.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tileTotalYards.Size = new System.Drawing.Size(88, 73);
            tileTotalYards.TabIndex = 1;
            tileTotalYards.Title = "Total";
            tileTotalYards.UnitOfMeasure = "yds.";
            // 
            // tileClubSpeed
            // 
            tileClubSpeed.BackColor = System.Drawing.Color.FromArgb(((int)((byte)28)), ((int)((byte)28)), ((int)((byte)32)));
            tileClubSpeed.Dock = System.Windows.Forms.DockStyle.Fill;
            tileClubSpeed.Location = new System.Drawing.Point(1, 310);
            tileClubSpeed.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tileClubSpeed.Name = "tileClubSpeed";
            tileClubSpeed.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tileClubSpeed.Size = new System.Drawing.Size(88, 73);
            tileClubSpeed.TabIndex = 0;
            tileClubSpeed.Title = "Club Speed";
            tileClubSpeed.UnitOfMeasure = "mph";
            // 
            // SimulatorFormAsync
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            BackColor = System.Drawing.SystemColors.ActiveBorder;
            ClientSize = new System.Drawing.Size(894, 494);
            Controls.Add(panelSim);
            Controls.Add(panelMetrics);
            Controls.Add(statusStrip1);
            Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
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