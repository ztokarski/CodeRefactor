namespace BengiLED_for_C_Power
{
    partial class passWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(passWindow));
            this.passBox = new System.Windows.Forms.TextBox();
            this.passLabel = new System.Windows.Forms.Label();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.rememberPaswdCheckBox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // passBox
            // 
            this.passBox.Location = new System.Drawing.Point(56, 38);
            this.passBox.Name = "passBox";
            this.passBox.PasswordChar = '*';
            this.passBox.Size = new System.Drawing.Size(100, 20);
            this.passBox.TabIndex = 0;
            this.passBox.TextChanged += new System.EventHandler(this.passBox_TextChanged);
            this.passBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.passBox_KeyPress);
            // 
            // passLabel
            // 
            this.passLabel.AutoSize = true;
            this.passLabel.Location = new System.Drawing.Point(46, 9);
            this.passLabel.Name = "passLabel";
            this.passLabel.Size = new System.Drawing.Size(120, 13);
            this.passLabel.TabIndex = 1;
            this.passLabel.Text = "Please, enter password:";
            // 
            // okButton
            // 
            this.okButton.Enabled = false;
            this.okButton.Location = new System.Drawing.Point(12, 88);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 2;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.TextChanged += new System.EventHandler(this.okButton_Click);
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(125, 88);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 3;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.TextChanged += new System.EventHandler(this.cancelButton_Click);
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // rememberPaswdCheckBox
            // 
            this.rememberPaswdCheckBox.AutoSize = true;
            this.rememberPaswdCheckBox.Location = new System.Drawing.Point(13, 65);
            this.rememberPaswdCheckBox.Name = "rememberPaswdCheckBox";
            this.rememberPaswdCheckBox.Size = new System.Drawing.Size(141, 17);
            this.rememberPaswdCheckBox.TabIndex = 4;
            this.rememberPaswdCheckBox.Text = "Remember my password";
            this.rememberPaswdCheckBox.UseVisualStyleBackColor = true;
            this.rememberPaswdCheckBox.CheckedChanged += new System.EventHandler(this.rememberPaswdCheckBox_CheckedChanged);
            // 
            // passWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(212, 123);
            this.Controls.Add(this.rememberPaswdCheckBox);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.passLabel);
            this.Controls.Add(this.passBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "passWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "LedConfig";
            this.VisibleChanged += new System.EventHandler(this.passWindow_VisibleChanged);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox passBox;
        private System.Windows.Forms.Label passLabel;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.CheckBox rememberPaswdCheckBox;
    }
}