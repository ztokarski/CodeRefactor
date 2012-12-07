namespace BengiLED_for_C_Power
{
    partial class LocalizatorForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LocalizatorForm));
            this.languagesComboBox = new System.Windows.Forms.ComboBox();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.generateLanguageFileButton = new System.Windows.Forms.Button();
            this.controlsToLocalizeGroupBox = new System.Windows.Forms.GroupBox();
            this.controlsToLocalizePanel = new System.Windows.Forms.Panel();
            this.localizationInfoLabel = new System.Windows.Forms.Label();
            this.controlsToLocalizeGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // languagesComboBox
            // 
            this.languagesComboBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.languagesComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.languagesComboBox.FormattingEnabled = true;
            this.languagesComboBox.Items.AddRange(new object[] {
            "Belarusian",
            "Bulgarian",
            "Chinese",
            "Croatian",
            "Czech",
            "Danish",
            "English (UK)",
            "English (US)",
            "Estonian",
            "Finnish",
            "French",
            "German",
            "Greek",
            "Hungarian",
            "Italian",
            "Latvian",
            "Lithuanian",
            "Norwegian",
            "Polish",
            "Portuguese",
            "Romanian",
            "Russian",
            "Serbian",
            "Slovak",
            "Spanish",
            "Swedish",
            "Turkish",
            "Ukrainian"});
            this.languagesComboBox.Location = new System.Drawing.Point(12, 13);
            this.languagesComboBox.Name = "languagesComboBox";
            this.languagesComboBox.Size = new System.Drawing.Size(123, 21);
            this.languagesComboBox.TabIndex = 0;
            this.languagesComboBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.languagesComboBox_DrawItem);
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "de.png");
            this.imageList1.Images.SetKeyName(1, "es.png");
            this.imageList1.Images.SetKeyName(2, "fr.png");
            this.imageList1.Images.SetKeyName(3, "pl.png");
            this.imageList1.Images.SetKeyName(4, "uk.png");
            // 
            // generateLanguageFileButton
            // 
            this.generateLanguageFileButton.Location = new System.Drawing.Point(141, 13);
            this.generateLanguageFileButton.Name = "generateLanguageFileButton";
            this.generateLanguageFileButton.Size = new System.Drawing.Size(75, 23);
            this.generateLanguageFileButton.TabIndex = 1;
            this.generateLanguageFileButton.Text = "Generate";
            this.generateLanguageFileButton.UseVisualStyleBackColor = true;
            this.generateLanguageFileButton.Click += new System.EventHandler(this.generateLanguageFileButton_Click);
            // 
            // controlsToLocalizeGroupBox
            // 
            this.controlsToLocalizeGroupBox.Controls.Add(this.controlsToLocalizePanel);
            this.controlsToLocalizeGroupBox.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.controlsToLocalizeGroupBox.Location = new System.Drawing.Point(0, 53);
            this.controlsToLocalizeGroupBox.Name = "controlsToLocalizeGroupBox";
            this.controlsToLocalizeGroupBox.Size = new System.Drawing.Size(703, 374);
            this.controlsToLocalizeGroupBox.TabIndex = 2;
            this.controlsToLocalizeGroupBox.TabStop = false;
            this.controlsToLocalizeGroupBox.Text = "List of controls for language file";
            // 
            // controlsToLocalizePanel
            // 
            this.controlsToLocalizePanel.AutoScroll = true;
            this.controlsToLocalizePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.controlsToLocalizePanel.Location = new System.Drawing.Point(3, 16);
            this.controlsToLocalizePanel.Name = "controlsToLocalizePanel";
            this.controlsToLocalizePanel.Size = new System.Drawing.Size(697, 355);
            this.controlsToLocalizePanel.TabIndex = 0;
            // 
            // localizationInfoLabel
            // 
            this.localizationInfoLabel.AutoSize = true;
            this.localizationInfoLabel.Location = new System.Drawing.Point(222, 18);
            this.localizationInfoLabel.Name = "localizationInfoLabel";
            this.localizationInfoLabel.Size = new System.Drawing.Size(463, 13);
            this.localizationInfoLabel.TabIndex = 3;
            this.localizationInfoLabel.Text = "Choose language from drop down, write translations and click Generate to create a" +
                " language file.";
            this.localizationInfoLabel.Click += new System.EventHandler(this.localizationInfoLabel_Click);
            // 
            // LocalizatorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(703, 427);
            this.Controls.Add(this.localizationInfoLabel);
            this.Controls.Add(this.controlsToLocalizeGroupBox);
            this.Controls.Add(this.generateLanguageFileButton);
            this.Controls.Add(this.languagesComboBox);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "LocalizatorForm";
            this.Text = "LocalizatorForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.LocalizatorForm_FormClosing);
            this.Load += new System.EventHandler(this.LocalizatorForm_Load);
            this.controlsToLocalizeGroupBox.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox languagesComboBox;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.Button generateLanguageFileButton;
        private System.Windows.Forms.GroupBox controlsToLocalizeGroupBox;
        private System.Windows.Forms.Panel controlsToLocalizePanel;
        private System.Windows.Forms.Label localizationInfoLabel;
    }
}