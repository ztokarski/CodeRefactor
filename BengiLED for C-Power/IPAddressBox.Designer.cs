namespace BengiLED_for_C_Power
{
    partial class IPAddressBox
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
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.dotLabel1 = new System.Windows.Forms.Label();
            this.dotLabel2 = new System.Windows.Forms.Label();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.dotLabel3 = new System.Windows.Forms.Label();
            this.textBox4 = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox1.Location = new System.Drawing.Point(4, 4);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(27, 13);
            this.textBox1.TabIndex = 0;
            this.textBox1.Text = "255";
            this.textBox1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.textBox1.TextChanged += new System.EventHandler(this.ipSegmentTextBox_TextChanged);
            this.textBox1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ipSegmentTextBox_KeyDown);
            this.textBox1.Leave += new System.EventHandler(this.ipSegmentTextBox_Leave);
            // 
            // textBox2
            // 
            this.textBox2.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox2.Location = new System.Drawing.Point(38, 4);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(27, 13);
            this.textBox2.TabIndex = 1;
            this.textBox2.Text = "255";
            this.textBox2.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.textBox2.TextChanged += new System.EventHandler(this.ipSegmentTextBox_TextChanged);
            this.textBox2.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ipSegmentTextBox_KeyDown);
            this.textBox2.Leave += new System.EventHandler(this.ipSegmentTextBox_Leave);
            // 
            // dotLabel1
            // 
            this.dotLabel1.Location = new System.Drawing.Point(30, 3);
            this.dotLabel1.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            this.dotLabel1.Name = "dotLabel1";
            this.dotLabel1.Size = new System.Drawing.Size(10, 15);
            this.dotLabel1.TabIndex = 4;
            this.dotLabel1.Text = ".";
            this.dotLabel1.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // dotLabel2
            // 
            this.dotLabel2.Location = new System.Drawing.Point(63, 3);
            this.dotLabel2.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            this.dotLabel2.Name = "dotLabel2";
            this.dotLabel2.Size = new System.Drawing.Size(10, 15);
            this.dotLabel2.TabIndex = 6;
            this.dotLabel2.Text = ".";
            this.dotLabel2.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // textBox3
            // 
            this.textBox3.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox3.Location = new System.Drawing.Point(71, 4);
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new System.Drawing.Size(27, 13);
            this.textBox3.TabIndex = 5;
            this.textBox3.Text = "255";
            this.textBox3.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.textBox3.TextChanged += new System.EventHandler(this.ipSegmentTextBox_TextChanged);
            this.textBox3.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ipSegmentTextBox_KeyDown);
            this.textBox3.Leave += new System.EventHandler(this.ipSegmentTextBox_Leave);
            // 
            // dotLabel3
            // 
            this.dotLabel3.Location = new System.Drawing.Point(96, 3);
            this.dotLabel3.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            this.dotLabel3.Name = "dotLabel3";
            this.dotLabel3.Size = new System.Drawing.Size(10, 15);
            this.dotLabel3.TabIndex = 8;
            this.dotLabel3.Text = ".";
            this.dotLabel3.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // textBox4
            // 
            this.textBox4.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox4.Location = new System.Drawing.Point(104, 4);
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new System.Drawing.Size(27, 13);
            this.textBox4.TabIndex = 7;
            this.textBox4.Text = "255";
            this.textBox4.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.textBox4.TextChanged += new System.EventHandler(this.ipSegmentTextBox_TextChanged);
            this.textBox4.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ipSegmentTextBox_KeyDown);
            this.textBox4.Leave += new System.EventHandler(this.ipSegmentTextBox_Leave);
            // 
            // IPAddressBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.Controls.Add(this.dotLabel3);
            this.Controls.Add(this.textBox4);
            this.Controls.Add(this.dotLabel2);
            this.Controls.Add(this.textBox3);
            this.Controls.Add(this.dotLabel1);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.textBox1);
            this.Name = "IPAddressBox";
            this.Size = new System.Drawing.Size(135, 21);
            this.EnabledChanged += new System.EventHandler(this.IPAddressBox_EnabledChanged);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label dotLabel1;
        private System.Windows.Forms.Label dotLabel2;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.Label dotLabel3;
        private System.Windows.Forms.TextBox textBox4;
    }
}
