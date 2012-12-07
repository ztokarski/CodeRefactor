namespace BengiLED_for_C_Power
{
    partial class NetworkSearchWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NetworkSearchWindow));
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.menuTab = new System.Windows.Forms.TabPage();
            this.selectCloseButton = new System.Windows.Forms.Button();
            this.selectSearchButton = new System.Windows.Forms.Button();
            this.selectConnectButton = new System.Windows.Forms.Button();
            this.connectTab = new System.Windows.Forms.TabPage();
            this.connectBackButton = new System.Windows.Forms.Button();
            this.notConnectButton = new System.Windows.Forms.Button();
            this.IpAdressTextBox = new System.Windows.Forms.TextBox();
            this.IpAddressLabel = new System.Windows.Forms.Label();
            this.connectButton = new System.Windows.Forms.Button();
            this.searchTab = new System.Windows.Forms.TabPage();
            this.searchBackButton = new System.Windows.Forms.Button();
            this.searchNetListBox = new System.Windows.Forms.CheckedListBox();
            this.statusLabel = new System.Windows.Forms.Label();
            this.notSearchButton = new System.Windows.Forms.Button();
            this.netSearchLabel = new System.Windows.Forms.Label();
            this.rangeGroupBox = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.minRangeLabel = new System.Windows.Forms.Label();
            this.maxRangelabel = new System.Windows.Forms.Label();
            this.minTextBox = new System.Windows.Forms.TextBox();
            this.maxTextBox = new System.Windows.Forms.TextBox();
            this.searchButton = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.menuTab.SuspendLayout();
            this.connectTab.SuspendLayout();
            this.searchTab.SuspendLayout();
            this.rangeGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.menuTab);
            this.tabControl1.Controls.Add(this.connectTab);
            this.tabControl1.Controls.Add(this.searchTab);
            this.tabControl1.Location = new System.Drawing.Point(12, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(350, 275);
            this.tabControl1.TabIndex = 0;
            // 
            // menuTab
            // 
            this.menuTab.Controls.Add(this.selectCloseButton);
            this.menuTab.Controls.Add(this.selectSearchButton);
            this.menuTab.Controls.Add(this.selectConnectButton);
            this.menuTab.Location = new System.Drawing.Point(4, 22);
            this.menuTab.Name = "menuTab";
            this.menuTab.Padding = new System.Windows.Forms.Padding(3);
            this.menuTab.Size = new System.Drawing.Size(342, 249);
            this.menuTab.TabIndex = 2;
            this.menuTab.Text = "Connect via network";
            this.menuTab.UseVisualStyleBackColor = true;
            // 
            // selectCloseButton
            // 
            this.selectCloseButton.Location = new System.Drawing.Point(129, 143);
            this.selectCloseButton.Name = "selectCloseButton";
            this.selectCloseButton.Size = new System.Drawing.Size(85, 40);
            this.selectCloseButton.TabIndex = 4;
            this.selectCloseButton.Text = "Close without connecting";
            this.selectCloseButton.UseVisualStyleBackColor = true;
            this.selectCloseButton.Click += new System.EventHandler(this.notConnectButton_Click);
            // 
            // selectSearchButton
            // 
            this.selectSearchButton.Location = new System.Drawing.Point(188, 65);
            this.selectSearchButton.Name = "selectSearchButton";
            this.selectSearchButton.Size = new System.Drawing.Size(95, 55);
            this.selectSearchButton.TabIndex = 1;
            this.selectSearchButton.Text = "I don\'t know IP, let me search the network";
            this.selectSearchButton.UseVisualStyleBackColor = true;
            this.selectSearchButton.Click += new System.EventHandler(this.selectSearchButton_Click);
            // 
            // selectConnectButton
            // 
            this.selectConnectButton.Location = new System.Drawing.Point(59, 65);
            this.selectConnectButton.Name = "selectConnectButton";
            this.selectConnectButton.Size = new System.Drawing.Size(95, 55);
            this.selectConnectButton.TabIndex = 0;
            this.selectConnectButton.Text = "I know IP address let me connect directly";
            this.selectConnectButton.UseVisualStyleBackColor = true;
            this.selectConnectButton.Click += new System.EventHandler(this.selectConnectButton_Click);
            // 
            // connectTab
            // 
            this.connectTab.Controls.Add(this.connectBackButton);
            this.connectTab.Controls.Add(this.notConnectButton);
            this.connectTab.Controls.Add(this.IpAdressTextBox);
            this.connectTab.Controls.Add(this.IpAddressLabel);
            this.connectTab.Controls.Add(this.connectButton);
            this.connectTab.Location = new System.Drawing.Point(4, 22);
            this.connectTab.Name = "connectTab";
            this.connectTab.Padding = new System.Windows.Forms.Padding(3);
            this.connectTab.Size = new System.Drawing.Size(342, 249);
            this.connectTab.TabIndex = 0;
            this.connectTab.Text = "Connect to given screen";
            this.connectTab.UseVisualStyleBackColor = true;
            // 
            // connectBackButton
            // 
            this.connectBackButton.Location = new System.Drawing.Point(121, 184);
            this.connectBackButton.Name = "connectBackButton";
            this.connectBackButton.Size = new System.Drawing.Size(100, 37);
            this.connectBackButton.TabIndex = 4;
            this.connectBackButton.Text = "Back to connection menu";
            this.connectBackButton.UseVisualStyleBackColor = true;
            this.connectBackButton.Click += new System.EventHandler(this.BackButton_Click);
            // 
            // notConnectButton
            // 
            this.notConnectButton.Location = new System.Drawing.Point(171, 138);
            this.notConnectButton.Name = "notConnectButton";
            this.notConnectButton.Size = new System.Drawing.Size(85, 40);
            this.notConnectButton.TabIndex = 3;
            this.notConnectButton.Text = "Close without connecting";
            this.notConnectButton.UseVisualStyleBackColor = true;
            this.notConnectButton.Click += new System.EventHandler(this.notConnectButton_Click);
            // 
            // IpAdressTextBox
            // 
            this.IpAdressTextBox.Location = new System.Drawing.Point(121, 97);
            this.IpAdressTextBox.Name = "IpAdressTextBox";
            this.IpAdressTextBox.Size = new System.Drawing.Size(100, 20);
            this.IpAdressTextBox.TabIndex = 2;
            // 
            // IpAddressLabel
            // 
            this.IpAddressLabel.AutoSize = true;
            this.IpAddressLabel.Location = new System.Drawing.Point(79, 70);
            this.IpAddressLabel.Name = "IpAddressLabel";
            this.IpAddressLabel.Size = new System.Drawing.Size(184, 13);
            this.IpAddressLabel.TabIndex = 1;
            this.IpAddressLabel.Text = "Please fill in with the given IP address";
            // 
            // connectButton
            // 
            this.connectButton.Location = new System.Drawing.Point(86, 138);
            this.connectButton.Name = "connectButton";
            this.connectButton.Size = new System.Drawing.Size(75, 40);
            this.connectButton.TabIndex = 0;
            this.connectButton.Text = "Connect!";
            this.connectButton.UseVisualStyleBackColor = true;
            this.connectButton.Click += new System.EventHandler(this.connectButton_Click);
            // 
            // searchTab
            // 
            this.searchTab.Controls.Add(this.searchBackButton);
            this.searchTab.Controls.Add(this.searchNetListBox);
            this.searchTab.Controls.Add(this.statusLabel);
            this.searchTab.Controls.Add(this.notSearchButton);
            this.searchTab.Controls.Add(this.netSearchLabel);
            this.searchTab.Controls.Add(this.rangeGroupBox);
            this.searchTab.Controls.Add(this.searchButton);
            this.searchTab.Location = new System.Drawing.Point(4, 22);
            this.searchTab.Name = "searchTab";
            this.searchTab.Padding = new System.Windows.Forms.Padding(3);
            this.searchTab.Size = new System.Drawing.Size(342, 249);
            this.searchTab.TabIndex = 1;
            this.searchTab.Text = "Search for Screen";
            this.searchTab.UseVisualStyleBackColor = true;
            // 
            // searchBackButton
            // 
            this.searchBackButton.Location = new System.Drawing.Point(117, 184);
            this.searchBackButton.Name = "searchBackButton";
            this.searchBackButton.Size = new System.Drawing.Size(100, 37);
            this.searchBackButton.TabIndex = 7;
            this.searchBackButton.Text = "Back to connection menu";
            this.searchBackButton.UseVisualStyleBackColor = true;
            this.searchBackButton.Click += new System.EventHandler(this.BackButton_Click);
            // 
            // searchNetListBox
            // 
            this.searchNetListBox.CheckOnClick = true;
            this.searchNetListBox.FormattingEnabled = true;
            this.searchNetListBox.Location = new System.Drawing.Point(13, 51);
            this.searchNetListBox.Name = "searchNetListBox";
            this.searchNetListBox.Size = new System.Drawing.Size(160, 124);
            this.searchNetListBox.TabIndex = 6;
            this.searchNetListBox.SelectedIndexChanged += new System.EventHandler(this.netSearchComboBox_SelectedIndexChanged);
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = true;
            this.statusLabel.Location = new System.Drawing.Point(7, 233);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(0, 13);
            this.statusLabel.TabIndex = 5;
            this.statusLabel.TextChanged += new System.EventHandler(this.statusLabel_TextChanged);
            // 
            // notSearchButton
            // 
            this.notSearchButton.Location = new System.Drawing.Point(245, 181);
            this.notSearchButton.Name = "notSearchButton";
            this.notSearchButton.Size = new System.Drawing.Size(85, 40);
            this.notSearchButton.TabIndex = 4;
            this.notSearchButton.Text = "Close without searching";
            this.notSearchButton.UseVisualStyleBackColor = true;
            this.notSearchButton.Click += new System.EventHandler(this.notConnectButton_Click);
            // 
            // netSearchLabel
            // 
            this.netSearchLabel.AutoSize = true;
            this.netSearchLabel.Location = new System.Drawing.Point(10, 12);
            this.netSearchLabel.Name = "netSearchLabel";
            this.netSearchLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.netSearchLabel.Size = new System.Drawing.Size(255, 26);
            this.netSearchLabel.TabIndex = 2;
            this.netSearchLabel.Text = "Your computer is connected to more than 1 network,\r\nplease select a network to se" +
                "arch screen:";
            // 
            // rangeGroupBox
            // 
            this.rangeGroupBox.Controls.Add(this.label1);
            this.rangeGroupBox.Controls.Add(this.label2);
            this.rangeGroupBox.Controls.Add(this.minRangeLabel);
            this.rangeGroupBox.Controls.Add(this.maxRangelabel);
            this.rangeGroupBox.Controls.Add(this.minTextBox);
            this.rangeGroupBox.Controls.Add(this.maxTextBox);
            this.rangeGroupBox.Location = new System.Drawing.Point(179, 75);
            this.rangeGroupBox.Name = "rangeGroupBox";
            this.rangeGroupBox.Size = new System.Drawing.Size(157, 100);
            this.rangeGroupBox.TabIndex = 1;
            this.rangeGroupBox.TabStop = false;
            this.rangeGroupBox.Text = "Search range";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 33);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(33, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "From:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 69);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(23, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "To:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // minRangeLabel
            // 
            this.minRangeLabel.AutoSize = true;
            this.minRangeLabel.Location = new System.Drawing.Point(55, 33);
            this.minRangeLabel.Name = "minRangeLabel";
            this.minRangeLabel.Size = new System.Drawing.Size(10, 13);
            this.minRangeLabel.TabIndex = 3;
            this.minRangeLabel.Text = ",";
            this.minRangeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // maxRangelabel
            // 
            this.maxRangelabel.AutoSize = true;
            this.maxRangelabel.Location = new System.Drawing.Point(55, 69);
            this.maxRangelabel.Name = "maxRangelabel";
            this.maxRangelabel.Size = new System.Drawing.Size(10, 13);
            this.maxRangelabel.TabIndex = 2;
            this.maxRangelabel.Text = ",";
            this.maxRangelabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // minTextBox
            // 
            this.minTextBox.Location = new System.Drawing.Point(125, 30);
            this.minTextBox.Name = "minTextBox";
            this.minTextBox.Size = new System.Drawing.Size(27, 20);
            this.minTextBox.TabIndex = 1;
            this.minTextBox.Text = "1";
            this.minTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.numbersOnlyTextBox_KeyPress);
            this.minTextBox.Leave += new System.EventHandler(this.TextBox_Leave);
            // 
            // maxTextBox
            // 
            this.maxTextBox.Location = new System.Drawing.Point(125, 66);
            this.maxTextBox.Name = "maxTextBox";
            this.maxTextBox.Size = new System.Drawing.Size(27, 20);
            this.maxTextBox.TabIndex = 0;
            this.maxTextBox.Text = "254";
            this.maxTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.numbersOnlyTextBox_KeyPress);
            this.maxTextBox.Leave += new System.EventHandler(this.TextBox_Leave);
            // 
            // searchButton
            // 
            this.searchButton.Location = new System.Drawing.Point(12, 181);
            this.searchButton.Name = "searchButton";
            this.searchButton.Size = new System.Drawing.Size(75, 40);
            this.searchButton.TabIndex = 0;
            this.searchButton.Text = "Search";
            this.searchButton.UseVisualStyleBackColor = true;
            this.searchButton.Click += new System.EventHandler(this.searchButton_Click);
            // 
            // NetworkSearchWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(374, 299);
            this.Controls.Add(this.tabControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "NetworkSearchWindow";
            this.Text = "Connecting via network";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.NetworkSearchWindow_FormClosed);
            this.Load += new System.EventHandler(this.NetworkSearchWindow_Load);
            this.tabControl1.ResumeLayout(false);
            this.menuTab.ResumeLayout(false);
            this.connectTab.ResumeLayout(false);
            this.connectTab.PerformLayout();
            this.searchTab.ResumeLayout(false);
            this.searchTab.PerformLayout();
            this.rangeGroupBox.ResumeLayout(false);
            this.rangeGroupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage connectTab;
        private System.Windows.Forms.TabPage searchTab;
        private System.Windows.Forms.TextBox IpAdressTextBox;
        private System.Windows.Forms.Label IpAddressLabel;
        private System.Windows.Forms.Button connectButton;
        private System.Windows.Forms.Label netSearchLabel;
        private System.Windows.Forms.GroupBox rangeGroupBox;
        private System.Windows.Forms.Label minRangeLabel;
        private System.Windows.Forms.Label maxRangelabel;
        private System.Windows.Forms.TextBox minTextBox;
        private System.Windows.Forms.TextBox maxTextBox;
        private System.Windows.Forms.Button notConnectButton;
        private System.Windows.Forms.Button notSearchButton;
        private System.Windows.Forms.Button searchButton;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckedListBox searchNetListBox;
        private System.Windows.Forms.TabPage menuTab;
        private System.Windows.Forms.Button selectSearchButton;
        private System.Windows.Forms.Button selectConnectButton;
        private System.Windows.Forms.Button selectCloseButton;
        private System.Windows.Forms.Button connectBackButton;
        private System.Windows.Forms.Button searchBackButton;
    }
}