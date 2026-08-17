namespace TenOver.WinForm.Example
{
    partial class MetricTileControl
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblTitle = new Label();
            lblValue = new Label();
            lblUnit = new Label();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.Dock = DockStyle.Top;
            lblTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(255, 140, 60);
            lblTitle.Location = new Point(1, 1);
            lblTitle.Name = "lblTitle";
            lblTitle.Padding = new Padding(10, 8, 0, 0);
            lblTitle.Size = new Size(148, 40);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "TITLE";
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblValue
            // 
            lblValue.Dock = DockStyle.Fill;
            lblValue.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblValue.ForeColor = Color.White;
            lblValue.Location = new Point(1, 41);
            lblValue.Name = "lblValue";
            lblValue.Size = new Size(148, 42);
            lblValue.TabIndex = 1;
            lblValue.Text = "- - -";
            lblValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblUnit
            // 
            lblUnit.Dock = DockStyle.Bottom;
            lblUnit.Font = new Font("Segoe UI", 7.5F);
            lblUnit.ForeColor = Color.FromArgb(140, 140, 148);
            lblUnit.Location = new Point(1, 83);
            lblUnit.Name = "lblUnit";
            lblUnit.Padding = new Padding(10, 0, 0, 6);
            lblUnit.Size = new Size(148, 20);
            lblUnit.TabIndex = 2;
            lblUnit.Text = "unit";
            lblUnit.TextAlign = ContentAlignment.BottomLeft;
            // 
            // MetricTileControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(28, 28, 32);
            Controls.Add(lblValue);
            Controls.Add(lblUnit);
            Controls.Add(lblTitle);
            Margin = new Padding(1);
            Name = "MetricTileControl";
            Padding = new Padding(1);
            Size = new Size(150, 104);
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblValue;
        private System.Windows.Forms.Label lblUnit;
    }
}