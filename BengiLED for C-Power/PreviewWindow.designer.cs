namespace BengiLED_for_C_Power
{
    partial class PreviewWindow
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PreviewWindow));
            this.panel1 = new System.Windows.Forms.Panel();
            this.manipulateItemCheckBox = new System.Windows.Forms.CheckBox();
            this.previewZoomOutButton = new System.Windows.Forms.Button();
            this.previewZoomInButton = new System.Windows.Forms.Button();
            this.previewZoomLabel = new System.Windows.Forms.Label();
            this.previewZoomUpDown = new System.Windows.Forms.NumericUpDown();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.previewZoomUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.Control;
            this.panel1.Controls.Add(this.manipulateItemCheckBox);
            this.panel1.Controls.Add(this.previewZoomOutButton);
            this.panel1.Controls.Add(this.previewZoomInButton);
            this.panel1.Controls.Add(this.previewZoomLabel);
            this.panel1.Controls.Add(this.previewZoomUpDown);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(474, 27);
            this.panel1.TabIndex = 19;
            // 
            // manipulateItemCheckBox
            // 
            this.manipulateItemCheckBox.AutoSize = true;
            this.manipulateItemCheckBox.Location = new System.Drawing.Point(191, 6);
            this.manipulateItemCheckBox.Name = "manipulateItemCheckBox";
            this.manipulateItemCheckBox.Size = new System.Drawing.Size(100, 17);
            this.manipulateItemCheckBox.TabIndex = 23;
            this.manipulateItemCheckBox.Text = "Manipulate item";
            this.manipulateItemCheckBox.UseVisualStyleBackColor = true;
            this.manipulateItemCheckBox.Visible = false;
            this.manipulateItemCheckBox.CheckedChanged += new System.EventHandler(this.manipulateItemCheckBox_CheckedChanged);
            // 
            // previewZoomOutButton
            // 
            this.previewZoomOutButton.Image = global::BengiLED_for_C_Power.Properties.Resources.zoomM;
            this.previewZoomOutButton.Location = new System.Drawing.Point(43, 1);
            this.previewZoomOutButton.Name = "previewZoomOutButton";
            this.previewZoomOutButton.Size = new System.Drawing.Size(32, 25);
            this.previewZoomOutButton.TabIndex = 22;
            this.previewZoomOutButton.UseVisualStyleBackColor = true;
            this.previewZoomOutButton.Click += new System.EventHandler(this.previewZoomOutButton_Click);
            // 
            // previewZoomInButton
            // 
            this.previewZoomInButton.Image = global::BengiLED_for_C_Power.Properties.Resources.zoomP;
            this.previewZoomInButton.Location = new System.Drawing.Point(5, 1);
            this.previewZoomInButton.Name = "previewZoomInButton";
            this.previewZoomInButton.Size = new System.Drawing.Size(32, 25);
            this.previewZoomInButton.TabIndex = 21;
            this.previewZoomInButton.UseVisualStyleBackColor = true;
            this.previewZoomInButton.Click += new System.EventHandler(this.previewZoomInButton_Click);
            // 
            // previewZoomLabel
            // 
            this.previewZoomLabel.AutoSize = true;
            this.previewZoomLabel.BackColor = System.Drawing.Color.Transparent;
            this.previewZoomLabel.ForeColor = System.Drawing.Color.Black;
            this.previewZoomLabel.Location = new System.Drawing.Point(81, 7);
            this.previewZoomLabel.Name = "previewZoomLabel";
            this.previewZoomLabel.Size = new System.Drawing.Size(33, 13);
            this.previewZoomLabel.TabIndex = 20;
            this.previewZoomLabel.Text = "200%";
            // 
            // previewZoomUpDown
            // 
            this.previewZoomUpDown.Increment = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.previewZoomUpDown.Location = new System.Drawing.Point(120, 5);
            this.previewZoomUpDown.Maximum = new decimal(new int[] {
            600,
            0,
            0,
            0});
            this.previewZoomUpDown.Minimum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.previewZoomUpDown.Name = "previewZoomUpDown";
            this.previewZoomUpDown.Size = new System.Drawing.Size(52, 20);
            this.previewZoomUpDown.TabIndex = 19;
            this.previewZoomUpDown.Value = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.previewZoomUpDown.Visible = false;
            this.previewZoomUpDown.ValueChanged += new System.EventHandler(this.previewZoomUpDown_ValueChanged);
            // 
            // PreviewWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(474, 154);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PreviewWindow";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Preview Window";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.PreviewWindow_FormClosing);
            this.Load += new System.EventHandler(this.PreviewWindow_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.PreviewWindow_Paint);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PreviewWindow_MouseDown);
            this.MouseEnter += new System.EventHandler(this.PreviewWindow_MouseEnter);
            this.MouseLeave += new System.EventHandler(this.PreviewWindow_MouseLeave);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PreviewWindow_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.PreviewWindow_MouseUp);
            this.Move += new System.EventHandler(this.PreviewWindow_Move);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.previewZoomUpDown)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.CheckBox manipulateItemCheckBox;
        private System.Windows.Forms.Button previewZoomOutButton;
        private System.Windows.Forms.Button previewZoomInButton;
        private System.Windows.Forms.Label previewZoomLabel;
        private System.Windows.Forms.NumericUpDown previewZoomUpDown;

    }
}

