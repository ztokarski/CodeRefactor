using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Drawing;

namespace BengiLED_for_C_Power
{
    public partial class ScreenSettingsForm : Form
    {
        
        #region Private fields
        private int[] maxHeightGlobalArray = new int[2];
        private int[][] maxWidthGlobalArray = new int[2][];
        private int[] configListsValues = new int[12];
        private int[] configMaxValues = new int[13];
        private byte[] reservedValues = new byte[20];
        private byte[] screenConfigBytes = new byte[1];
        private System.Windows.Forms.Timer readAutoTimer = null;
        private int maxCount = 10;
        private int timerCounter = 0;
        private int periodTimerCounter = 0;
        private bool autoscanReady = false;
        private bool firstReadScan = false;
        private bool typeChanging = false;
        private bool getConfigOnReactive = false;
        private Color hubColor = Color.DarkKhaki;
        private int windowMaxHeight, windowMaxWidth, BorderWidth, TitlebarHeight;

        private TabPage[] settingsPages = new TabPage[3];

        #endregion

        #region Properties
        public System.Collections.Hashtable ConfigHashtable
        {
            get { return ((MainWindow)this.Owner).ConfigHashtable; }
        }
        public int MaxWidth
        {
            get
            {
                int retval = Convert.ToInt32(markHeightComboBox.Items[markHeightComboBox.SelectedIndex]) / 16 - 1;

                return maxWidthGlobalArray[ColorIndex][(retval < 0) ? 0 : retval];
            }
        }
        public int ColorIndex
        {
            get
            {
                return (markColorComboBox.SelectedIndex == 0) ? 1 : 0; // 1 = mono, 0 = color
            }
        }
        #endregion
        
        
        private bool allBrightnessAutoChanging;
        private bool allBrightnessValueChanging;
        private LocalizatorForm localizatorForm;
        private passWindow accessWindow;

        public Boolean AllBrightnessAutoChanging
        {
            get { return allBrightnessAutoChanging; }
            set { allBrightnessAutoChanging = value; }
        }

        public Boolean AllBrightnessValueChanging
        {
            get { return allBrightnessValueChanging; }
            set { allBrightnessValueChanging = value; }
        }
        
        public ScreenSettingsForm()
        {
            InitializeComponent();

            windowMaxHeight = this.Height;
            windowMaxWidth = this.Width;

            BorderWidth = (this.Width);
            BorderWidth -= this.ClientSize.Width;
            BorderWidth /= 2;

            TitlebarHeight = this.Height;
            TitlebarHeight -= this.ClientSize.Height;
            TitlebarHeight -= 2 * BorderWidth;

            this.Text = string.Format("{0} - {1}", this.Text, MainWindow.RetrieveLinkerTimestamp());

            localizatorForm = new LocalizatorForm();
            localizatorForm.Owner = this;

            //accessWindow = new passWindow("5819");
            accessWindow = new passWindow("8e036cc193d0af59aa9b22821248292b");
            accessWindow.Owner = this;


            for (int i = 0; i < 3; i++)
                settingsPages[i] = controllerSettingsTabControl.TabPages[i];

            this.Height = controllerInfoGroupBox.Top + controllerInfoGroupBox.Height + 40 + TitlebarHeight;
            this.Width = controllerInfoGroupBox.Left + controllerInfoGroupBox.Width + 10 + BorderWidth * 2;

            saveSettingsButton.Visible = false;
            loadSettingsButton.Visible = false;

            controllerSettingsTabControl.Enabled = false;
            controllerSettingsTabControl.Visible = false;
            ipSettingsGroupBox.Visible = false;
            iDAndBaudrateGroupBox.Visible = false;


            controllerSettingsTabControl.TabPages.Clear();
        }

        #region IP
        private void GetIP()
        {
            this.Cursor = Cursors.WaitCursor;

            System.Net.IPAddress[] tmpTab = new System.Net.IPAddress[4];
            int tmpport = 0;

            CommunicationResult result = Communication.GetIPConfiguration(out tmpTab, out tmpport);

            if (result == CommunicationResult.Success)
            {
                controllerIpAddressBox.IPAddress = tmpTab[0];
                gatewayIpAddressBox.IPAddress = tmpTab[1];
                maskIpAddressBox.IPAddress = tmpTab[2];
                passcodeIpAddressBox.IPAddress = tmpTab[3];

                //portTextBox.Text = tmpport.ToString();
                portNumericUpDown.Value = tmpport;
            }
            else
                MessageBox.Show(result.ToString());

            
            this.Cursor = Cursors.Default;
        }
        private void SetIP()
        {
            this.Cursor = Cursors.WaitCursor;

            System.Net.IPAddress[] tmpTab = new System.Net.IPAddress[4] 
                    { controllerIpAddressBox.IPAddress, gatewayIpAddressBox.IPAddress, maskIpAddressBox.IPAddress, passcodeIpAddressBox.IPAddress };
            int tmpport = Convert.ToInt32(portNumericUpDown.Value); //int.Parse(portTextBox.Text);

            CommunicationResult result = Communication.SetIPConfiguration(tmpTab, tmpport);

            if (result != CommunicationResult.Success)
                MessageBox.Show(result.ToString());
            

            this.Cursor = Cursors.Default;
        }

        private void getNetworkSettingsButton_Click(object sender, EventArgs e)
        {
            GetIP();
        }
        private void setNetworkSettingsButton_Click(object sender, EventArgs e)
        {
            SetIP();
        }
        #endregion

        #region Screen configuration

        private void getScreenConfigurationButton_Click(object sender, EventArgs e)
        {
            GetScreenConfig();
        }

        private void setScreenConfigurationButton_Click(object sender, EventArgs e)
        {
            Save1200ControlsState();
            SetScreenConfig();
        }

        private void SetScreenConfig()
        {
            try
            {
                SetIDAndBaudrate();
                if(MainWindow.controllerSettings.HasNetwork)
                    SetIP();


                this.Cursor = Cursors.WaitCursor;
                CommunicationResult result = CommunicationResult.Success;

                switch (MainWindow.controllerSettings.Series)
                {
                    case ControllerType.C_PowerX200:
                        result = Communication.SetScreenConfig(screenConfigBytes);
                        break;
                    case ControllerType.MARK_XX:
                        result = Communication.SetMarkConfig(false, configListsValues, reservedValues);
                        break;
                }

                if (result != CommunicationResult.Success)
                {
                    MessageBox.Show(result.ToString());
                }

            }
            catch
            {
                MessageBox.Show("There was a problem. Try again");
            }

            this.Cursor = Cursors.Default;
        }


        private byte[] ParseConfigByte(byte valueToParse, byte[] tmp)
        {
            byte[] retval = new byte[tmp.Length];

            System.Collections.BitArray tmpbits = new System.Collections.BitArray(new byte[] { valueToParse });

            for (int i = 0; i < tmp.Length; i++)
            {
                for (byte bitIndex = tmp[i]; bitIndex < tmpbits.Length; bitIndex++)
                {
                    retval[i] += Convert.ToByte(Convert.ToByte(tmpbits[bitIndex]) * Math.Pow(2.0, (double)(bitIndex - tmp[i])));
                    tmpbits[bitIndex] = false;
                }
            }

            return retval;
        }

        private void GetScreenConfig()
        {
            this.Cursor = Cursors.WaitCursor;
            typeChanging = true;

            //controllerSettingsTabControl.TabPages.Clear();

            if (readAutoTimer != null)
            {
                readAutoTimer.Stop();
                readAutoTimer.Dispose();
                readAutoTimer = null;
            }

            statusTextLabel.ForeColor = System.Drawing.Color.Red;
            statusTextLabel.Text = "NOT FOUND";
            controllerTypeTextLabel.Text = "---";
            controllerVersionTextLabel.Text = "---";

            ((MainWindow)Owner).SearchForScreen();//false);

            if (Communication.Type == CommunicationType.None)
            {
                this.Height = controllerInfoGroupBox.Top + controllerInfoGroupBox.Height + 40 + TitlebarHeight;
                this.Width = controllerInfoGroupBox.Left + controllerInfoGroupBox.Width + 10 + BorderWidth * 2;

                saveSettingsButton.Visible = false;
                loadSettingsButton.Visible = false;

                controllerSettingsTabControl.Enabled = false;
                controllerSettingsTabControl.Visible = false;
                ipSettingsGroupBox.Visible = false;
                iDAndBaudrateGroupBox.Visible = false;
                //offlineGroupBox.Visible = true;
            }
            else
            {
                this.Height = windowMaxHeight;
                this.Width = windowMaxWidth;

                saveSettingsButton.Visible = true;
                loadSettingsButton.Visible = true;

                controllerSettingsTabControl.Enabled = true;
                controllerSettingsTabControl.Visible = true;
                iDAndBaudrateGroupBox.Visible = true;

                offlineGroupBox.Visible = false;
                testWithoutControllerCheckBox.Visible = false;

                statusTextLabel.ForeColor = System.Drawing.Color.Green;
                statusTextLabel.Text = "FOUND";


                int baudrateIndex = 0;

                CommunicationResult result = Communication.GetIdAndBaudrate(out baudrateIndex);

                if (result == CommunicationResult.Success)
                {
                    baudrateComboBox.SelectedItem = baudrateComboBox.Items[baudrateIndex];
                    //controllerIDTextBox.Text = Communication.CardID.ToString();
                    idNumericUpDown.Value = Communication.CardID;
                }

                
                string version = "";
                result = Communication.GetVersion(out version);

                try
                {
                    //if (controllerType != null)
                    if (result == CommunicationResult.Success)
                    {
                        controllerTypeTextLabel.Text = MainWindow.controllerSettings.Name;//controllerType[0];
                        controllerVersionTextLabel.Text = version;//controllerType[1];

                        //if ((controllerType[0].Contains("C-Power")))
                        if (MainWindow.controllerSettings.Series == ControllerType.C_PowerX200)
                        {
                            //byte[] screenConfigBytes = new byte[1];//SerialCommunication.GetScreenConfig();

                            result = Communication.GetCpowerConfig(out screenConfigBytes);

                            if (MainWindow.controllerSettings.Type == ControllerType.C_Power1200)
                                MainWindow.controllerSettings.HasNetwork = false;
                            else
                                MainWindow.controllerSettings.HasNetwork = true;

                            if (MainWindow.controllerSettings.Type == ControllerType.C_Power1200 || MainWindow.controllerSettings.Type == ControllerType.C_Power2200 || MainWindow.controllerSettings.Type == ControllerType.C_Power3200)
                            {
                                if (controllerSettingsTabControl.SelectedTab == null ||
                                (controllerSettingsTabControl.SelectedTab != null
                                && controllerSettingsTabControl.SelectedTab.Name != "cpower1200SettingsTabPage"))
                                {
                                    controllerSettingsTabControl.TabPages.Clear();
                                    foreach (TabPage tab in settingsPages)
                                    {
                                        if (tab.Name == "cpower1200SettingsTabPage")
                                            controllerSettingsTabControl.TabPages.Add(tab);
                                    }
                                }
                                controllerSettingsTabControl.SelectedTab = controllerSettingsTabControl.TabPages["cpower1200SettingsTabPage"];

                                controllerSettingsTabControl.SelectedTab.Text = MainWindow.controllerSettings.Name;
                                screenConfigurationGroupBox.Text = MainWindow.controllerSettings.Name;

                                Setup1200Controls(screenConfigBytes);
                            }
                            else if (MainWindow.controllerSettings.Type == ControllerType.C_Power4200 || MainWindow.controllerSettings.Type == ControllerType.C_Power5200)
                            {
                                if (controllerSettingsTabControl.SelectedTab == null ||
                                (controllerSettingsTabControl.SelectedTab != null
                                && controllerSettingsTabControl.SelectedTab.Name != "cpower4200SettingsTabPage"))
                                {
                                    controllerSettingsTabControl.TabPages.Clear();
                                    foreach (TabPage tab in settingsPages)
                                    {
                                        if (tab.Name == "cpower4200SettingsTabPage")
                                            controllerSettingsTabControl.TabPages.Add(tab);
                                    }
                                }
                                controllerSettingsTabControl.SelectedTab = controllerSettingsTabControl.TabPages["cpower4200SettingsTabPage"];

                                controllerSettingsTabControl.SelectedTab.Text = MainWindow.controllerSettings.Name;
                                cpower4200ConfigurationGroupBox.Text = MainWindow.controllerSettings.Name;

                                Setup4200Controls(screenConfigBytes);
                            }


                            setScreenConfigurationButton.Enabled = true;
                        }
                        else if (MainWindow.controllerSettings.Series == ControllerType.MARK_XX)
                        {
                            if (controllerSettingsTabControl.SelectedTab == null ||
                                (controllerSettingsTabControl.SelectedTab != null
                                && controllerSettingsTabControl.SelectedTab.Name != "markXXSettingsTabPage"))
                            {
                                controllerSettingsTabControl.TabPages.Clear();
                                foreach (TabPage tab in settingsPages)
                                {
                                    if (tab.Name == "markXXSettingsTabPage")
                                        controllerSettingsTabControl.TabPages.Add(tab);
                                }
                            }
                            controllerSettingsTabControl.SelectedTab = controllerSettingsTabControl.TabPages["markXXSettingsTabPage"];
                            controllerSettingsTabControl.SelectedTab.Text = MainWindow.controllerSettings.Name;
                            markXXSettingsGroupBox.Text = MainWindow.controllerSettings.Name;


                            GetMarkConfiguration();
                        }
                        else
                        {
                            //controllerTypeTextLabel.Text = "---";
                            //controllerVersionTextLabel.Text = "---";

                            MessageBox.Show("Your controller is not supported.", "LedConfig");
                            setScreenConfigurationButton.Enabled = false;
                        }


                        if (MainWindow.controllerSettings.HasNetwork)
                        {
                            ipSettingsGroupBox.Enabled = true;
                            ipSettingsGroupBox.Visible = true;
                            GetIP();
                        }
                        else
                        {
                            ipSettingsGroupBox.Enabled = false;
                            ipSettingsGroupBox.Visible = false;
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("Your controller is not supported.", "LedConfig");
                    setScreenConfigurationButton.Enabled = false;

                }
            }

            ((MainWindow)Owner).logWindow.Hide();
            this.Activate();

            typeChanging = false;
            this.Cursor = Cursors.Default;
        }

        private void Setup1200Controls(byte[] screenConfigBytes)
        {
            if (screenConfigBytes[0] == 0xFF)
                oePolarityComboBox.Enabled = false;
            else
            {
                oePolarityComboBox.Enabled = true;
                oePolarityComboBox.SelectedIndex = screenConfigBytes[0];
            }
            columnOrderComboBox.SelectedIndex = screenConfigBytes[1];
            dataPolarityComboBox.SelectedIndex = screenConfigBytes[2];
            hideScanComboBox.SelectedIndex = screenConfigBytes[3];
            lineAdjustComboBox.SelectedIndex = screenConfigBytes[4];
            clockModeComboBox.SelectedIndex = screenConfigBytes[5];
            moduleSizeComboBox.SelectedIndex = screenConfigBytes[6];
            scanModeComboBox.SelectedIndex = screenConfigBytes[7];
            lineChangeSpaceComboBox.SelectedIndex = screenConfigBytes[8];
            lineChangeDirectionComboBox.SelectedIndex = screenConfigBytes[9];
            signalReverseComboBox.SelectedIndex = screenConfigBytes[10];
            colorOrderComboBox.SelectedIndex = screenConfigBytes[11];
            outputBoardComboBox.SelectedIndex = screenConfigBytes[12];
        }

        private void Setup4200Controls(byte[] configBytes)
        {
            byte[] tmpByteArray;

            // get: hide scan (6)
            tmpByteArray = ParseConfigByte(configBytes[0], new byte[] { 6 });
            cpower4200HideScanComboBox.SelectedItem = cpower4200HideScanComboBox.Items[tmpByteArray[0]];

            // get: line adjust (6), color order (3), data polarity (2), OE polarity (1), column order (0)
            tmpByteArray = ParseConfigByte(configBytes[1], new byte[] { 6, 3, 2, 1, 0 });
            cpower4200LineAdjustComboBox.SelectedItem = cpower4200LineAdjustComboBox.Items[tmpByteArray[0]];
            cpower4200ColorOrderComboBox.SelectedItem = cpower4200ColorOrderComboBox.Items[tmpByteArray[1]];
            cpower4200DataPolarityComboBox.SelectedItem = cpower4200DataPolarityComboBox.Items[tmpByteArray[2]];
            cpower4200OEPolarityComboBox.SelectedItem = cpower4200OEPolarityComboBox.Items[tmpByteArray[3]];
            cpower4200ColumnOrderComboBox.SelectedItem = cpower4200ColumnOrderComboBox.Items[tmpByteArray[4]];

            // get: line change space (6), signal reverse (3), scan mode (0)
            tmpByteArray = ParseConfigByte(configBytes[2], new byte[] { 6, 3, 0 });
            cpower4200LineChangeSpaceComboBox.SelectedItem = cpower4200LineChangeSpaceComboBox.Items[tmpByteArray[0]];
            cpower4200SignalReverseComboBox.SelectedItem = cpower4200SignalReverseComboBox.Items[tmpByteArray[1]];
            cpower4200ScanModeComboBox.SelectedItem = cpower4200ScanModeComboBox.Items[tmpByteArray[2]];

            // get: line reverse (4), line change direction (3), module size (0)
            tmpByteArray = ParseConfigByte(configBytes[3], new byte[] { 4, 3, 0 });
            //cpower4200LineReverseComboBox.SelectedItem = cpower4200LineReverseComboBox.Items[tmpByteArray[0]-1];
            cpower4200LineReverseComboBox.SelectedIndex = (tmpByteArray[0] > 0) ? tmpByteArray[0] - 1 : tmpByteArray[0];
            cpower4200LineChangeDirectionComboBox.SelectedItem = cpower4200LineChangeDirectionComboBox.Items[tmpByteArray[1]];
            cpower4200ModuleSizeComboBox.SelectedItem = cpower4200ModuleSizeComboBox.Items[tmpByteArray[2]];

            // get: pulse trimming (6), timing trimming (4), clock mode (0)
            tmpByteArray = ParseConfigByte(configBytes[4], new byte[] { 6, 4, 0 });
            cpower4200PulseTrimmingComboBox.SelectedItem = cpower4200PulseTrimmingComboBox.Items[tmpByteArray[0]];
            cpower4200TimingTrimmingComboBox.SelectedItem = cpower4200TimingTrimmingComboBox.Items[tmpByteArray[1]];
            cpower4200ClockModeComboBox.SelectedItem = cpower4200ClockModeComboBox.Items[tmpByteArray[2]];

            cpower4200OutputBoardComboBox.SelectedItem = cpower4200OutputBoardComboBox.Items[configBytes[5]];
        }

        private void Save1200ControlsState()
        { 
            if (oePolarityComboBox.Enabled == false)
                screenConfigBytes[0] = 0xFF;
            else
                screenConfigBytes[0] = (byte)oePolarityComboBox.SelectedIndex;
            screenConfigBytes[1] = (byte)columnOrderComboBox.SelectedIndex;
            screenConfigBytes[2] = (byte)dataPolarityComboBox.SelectedIndex;
            screenConfigBytes[3] = (byte)hideScanComboBox.SelectedIndex;
            screenConfigBytes[4] = (byte)lineAdjustComboBox.SelectedIndex;
            screenConfigBytes[5] = (byte)clockModeComboBox.SelectedIndex;
            screenConfigBytes[6] = (byte)moduleSizeComboBox.SelectedIndex;
            screenConfigBytes[7] = (byte)scanModeComboBox.SelectedIndex;
            screenConfigBytes[8] = (byte)lineChangeSpaceComboBox.SelectedIndex;
            screenConfigBytes[9] = (byte)lineChangeDirectionComboBox.SelectedIndex;
            screenConfigBytes[10] = (byte)signalReverseComboBox.SelectedIndex;
            screenConfigBytes[11] = (byte)colorOrderComboBox.SelectedIndex;
            screenConfigBytes[12] = (byte)outputBoardComboBox.SelectedIndex;
        }
       
        private void Save4200ControlsState()
        {
            // set: hide scan (6)
            screenConfigBytes[0] = Convert.ToByte(
                0x2F //?
                + 64 * cpower4200HideScanComboBox.SelectedIndex // hide scan
                );

            // set: line adjust (6), color order (3), data polarity (2), OE polarity (1), column order (0)
            screenConfigBytes[1] = Convert.ToByte(
               64 * cpower4200LineAdjustComboBox.SelectedIndex // line adjust
               + 8 * cpower4200ColorOrderComboBox.SelectedIndex // color order
               + 4 * cpower4200DataPolarityComboBox.SelectedIndex // data polarity
               + 2 * cpower4200OEPolarityComboBox.SelectedIndex // OE polarity
               + 1 * cpower4200ColumnOrderComboBox.SelectedIndex // column order
                );

            // set: line change space (6), signal reverse (3), scan mode (0)
             screenConfigBytes[2] = Convert.ToByte(
               64 * cpower4200LineChangeSpaceComboBox.SelectedIndex // line change space
               + 8 * cpower4200SignalReverseComboBox.SelectedIndex // signal reverse
               + 1 * cpower4200ScanModeComboBox.SelectedIndex // scan mode
               );

            // set: line reverse (4), line change direction (3), module size (0)
             screenConfigBytes[3] = Convert.ToByte(
                 16 * ((cpower4200LineReverseComboBox.SelectedIndex > 0) ? cpower4200LineReverseComboBox.SelectedIndex + 1 : cpower4200LineReverseComboBox.SelectedIndex) // line reverse
                + 8 * cpower4200LineChangeDirectionComboBox.SelectedIndex // line change direction
                + 1 * cpower4200ModuleSizeComboBox.SelectedIndex // module size
                );

            // set: pulse trimming (6), timing trimming (4), clock mode (0)
             screenConfigBytes[4] = Convert.ToByte(
                64 * cpower4200PulseTrimmingComboBox.SelectedIndex // pulse trimming
                + 16 * cpower4200TimingTrimmingComboBox.SelectedIndex // timing trimming
                + 1 * cpower4200ClockModeComboBox.SelectedIndex // clock mode
                );

             // set: output board (0)
             screenConfigBytes[5] = Convert.ToByte(
                1 * cpower4200OutputBoardComboBox.SelectedIndex // output board
                );
        }
        
        #endregion

        private void ScreenSettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            getConfigOnReactive = true;


            if (readAutoTimer != null)
            {
                readAutoTimer.Stop();
                readAutoTimer.Dispose();
                readAutoTimer = null;

                nextReadTimeInfoLabel.Text = "";
                readScanStatusTextBox.Text = "";
            }
            performReadScanPeriodicalyCheckBox.Checked = false;
            periodTimerCounter = 0;
            timerCounter = 0;

            if (e.CloseReason == CloseReason.UserClosing)
                e.Cancel = true;
        }

        private void ScreenSettingsForm_Shown(object sender, EventArgs e)
        {
            GetScreenConfig();

            if (Communication.Type == CommunicationType.None)
            {
                //testWithoutControllerCheckBox.Visible = true;
            }

            //if(Communication.Type != CommunicationType.None)
            //{
            //    int baudrateIndex = 0;

            //    CommunicationResult result = Communication.GetIdAndBaudrate(out baudrateIndex);

            //    if (result == CommunicationResult.Success)
            //    {
            //        baudrateComboBox.SelectedItem = baudrateComboBox.Items[baudrateIndex];
            //        controllerIDTextBox.Text = Communication.CardID.ToString();
            //    }

            //    if (MainWindow.controllerSettings.HasNetwork)
            //    {
            //        ipSettingsGroupBox.Enabled = true;
            //        ipSettingsGroupBox.Visible = true;
            //        GetIP();
            //    }
            //    else
            //    {
            //        ipSettingsGroupBox.Enabled = false;
            //        ipSettingsGroupBox.Visible = false;
            //    }
            //}
            //else
            //{
            //    statusTextLabel.ForeColor = System.Drawing.Color.Red;
            //    statusTextLabel.Text = "NOT FOUND";
            //    controllerTypeTextLabel.Text = "---";
            //    controllerVersionTextLabel.Text = "---";
            //}
        }

        private void openLocalizatorButton_Click(object sender, EventArgs e)
        {
            if (accessWindow.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                localizatorForm.Show();
            }
        }

        private void ScreenSettingsForm_Load(object sender, EventArgs e)
        {
            bool configurator = false;
            if (((MainWindow)Owner).ConfigHashtable.ContainsKey("ConfiguratorPassword"))
            {
                using (MD5 md5Hash = MD5.Create())
                {
                    string hash = "cde77a38cc4513b2a704158a20e06c85"; //GetMd5Hash(md5Hash, ConfigHashtable["TesterPassword"].ToString());

                    if (MainWindow.VerifyMd5Hash(md5Hash, ((MainWindow)Owner).ConfigHashtable["ConfiguratorPassword"].ToString(), hash))
                        configurator = true;
                }
            }
            
            //if (!((MainWindow)Owner).ConfigHashtable.ContainsKey("ConfiguratorPassword")
            //    || ((MainWindow)Owner).ConfigHashtable["ConfiguratorPassword"].ToString() != "letmechangeit")
            if(!configurator)
            {
                screenConfigurationGroupBox.Visible = false;
                this.Width -= (screenConfigurationGroupBox.Width + 10);
            }
        }

        private void checkControllerButton_Click(object sender, EventArgs e)
        {
            GetScreenConfig();
            //if (SerialCommunication.DevicesLists.Count > 0)
            //if (Communication.Type != CommunicationType.None)
            //{
            //    int baudrateIndex = 0;

            //    CommunicationResult result = Communication.GetIdAndBaudrate(out baudrateIndex);

            //    if (result == CommunicationResult.Success)
            //    {
            //        baudrateComboBox.SelectedItem = baudrateComboBox.Items[baudrateIndex];
            //        controllerIDTextBox.Text = Communication.CardID.ToString();
            //    }


            //    if (MainWindow.controllerSettings.HasNetwork)
            //    {
            //        ipSettingsGroupBox.Enabled = true;
            //        ipSettingsGroupBox.Visible = true;
            //        GetIP();
            //    }
            //    else
            //    {
            //        ipSettingsGroupBox.Enabled = false;
            //        ipSettingsGroupBox.Visible = false;
            //    }
            //}
            //else
            //{
            //    statusTextLabel.ForeColor = System.Drawing.Color.Red;
            //    statusTextLabel.Text = "NOT FOUND";
            //    controllerTypeTextLabel.Text = "---";
            //    controllerVersionTextLabel.Text = "---";
            //}
        }

        private void SetIDAndBaudrate()
        {
            this.Cursor = Cursors.WaitCursor;

            CommunicationResult result = Communication.SetIdAndBaudrate(Convert.ToInt32(idNumericUpDown.Value),//int.Parse(controllerIDTextBox.Text), 
                baudrateComboBox.SelectedIndex);

            if (result != CommunicationResult.Success)
            {
                if (Communication.Type == CommunicationType.Serial)
                {
                    SerialCommunication.BaudrateToChange = baudrateComboBox.SelectedIndex;
                    SerialCommunication.CurrentSerialPort.Baudrate = SerialCommunication.BaudrateToChange;
                    SerialCommunication.ControllerSerialPort.BaudRate = SerialCommunication.CurrentSerialPort.Baudrate;
                    //Communication.CardID = 255;

                    int tmpbaudrateIndex = 0;
                    result = Communication.GetIdAndBaudrate(out tmpbaudrateIndex);

                    if (result != CommunicationResult.Success)
                        MessageBox.Show(result.ToString());
                    else
                        baudrateComboBox.SelectedItem = baudrateComboBox.Items[tmpbaudrateIndex];
                }
            }

            if (Communication.Type == CommunicationType.Serial)
                ((MainWindow)Owner).UpdateSerialCommunication();

            this.Cursor = Cursors.Default;
        }

        private void setIDAndBaudrateButton_Click(object sender, EventArgs e)
        {
            SetIDAndBaudrate();
        }

        #region MARK-XX setttings window

        void readAutoTimer_Tick(object sender, EventArgs e)
        {
            timerCounter++;
            nextReadTimeInfoLabel.Text = string.Format("Next read in {0} s", maxCount - timerCounter);
            readScanStatusTextBox.Text = string.Format("Next read in {0} s", maxCount - timerCounter);
            //if (timerCounter == maxCount - 2)
            //    autoscanReady = SetupAutoscanValues();
            //else 
            if (timerCounter == maxCount - 1)
            {
                autoscanReady = SetupAutoscanValues();

                //readScanStatusTextBox.Text = "No info";
                Color currentBackColor = Color.DarkOrange;

                readScanStatusTextBox.BackColor = currentBackColor;
                autoColorTextBox.BackColor = currentBackColor;
                autoHeightTextBox.BackColor = currentBackColor;
                autoWidthTextBox.BackColor = currentBackColor;
                autoScanModeTextBox.BackColor = currentBackColor;

                autoHubTextBox.BackColor = currentBackColor;
                controllerHubTextBox.BackColor = currentBackColor;
                screenHubTextBox.BackColor = currentBackColor;


                autoDevicesListBox.BackColor = currentBackColor;
                autoValuesListBox.BackColor = currentBackColor;


                if (firstReadScan)
                {
                    GetMarkConfiguration();

                    firstReadScan = false;
                }

            }
            else if (timerCounter == maxCount)
            {
                timerCounter = 0;

                nextReadTimeInfoLabel.Text = "";
                SetAutoscanColorByReady(autoscanReady);
                autoscanReady = false;
                //SetupAutoscanValues();

                //if (readAutoTimer != null)
                //{
                //    readAutoTimer.Stop();
                //    readAutoTimer.Dispose();
                //    readAutoTimer = null;

                //    nextReadTimeInfoLabel.Text = "";
                //}
            }

        }

        void readAutoTimerWithPeriod_Tick(object sender, EventArgs e)
        {
            periodTimerCounter++;


            //if (periodTimerCounter == readScanPeriodNumericUpDown.Value - 2)
            //    autoscanReady = SetupAutoscanValues();
            if (periodTimerCounter == readScanPeriodNumericUpDown.Value - 1)
            {
                autoscanReady = SetupAutoscanValues();

                //readScanStatusTextBox.Text = "No info";
                Color currentBackColor = Color.DarkOrange;

                readScanStatusTextBox.BackColor = currentBackColor;
                autoColorTextBox.BackColor = currentBackColor;
                autoHeightTextBox.BackColor = currentBackColor;
                autoWidthTextBox.BackColor = currentBackColor;
                autoScanModeTextBox.BackColor = currentBackColor;


                autoHubTextBox.BackColor = currentBackColor;
                controllerHubTextBox.BackColor = currentBackColor;
                screenHubTextBox.BackColor = currentBackColor;

                autoDevicesListBox.BackColor = currentBackColor;
                autoValuesListBox.BackColor = currentBackColor;
            }
            else if (periodTimerCounter == readScanPeriodNumericUpDown.Value)
            {
                periodTimerCounter = 0;
                //SetupAutoscanValues();

                SetAutoscanColorByReady(autoscanReady);
                autoscanReady = false;
            }

            nextReadTimeInfoLabel.Text = string.Format("Next read in {0} s", readScanPeriodNumericUpDown.Value - periodTimerCounter);
            readScanStatusTextBox.Text = string.Format("Next read in {0} s", readScanPeriodNumericUpDown.Value - periodTimerCounter);
        }

        private void autoSendTimeCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            bool autoSendTime = true;
            bool.TryParse(ConfigHashtable["AutoSendTime"].ToString(), out autoSendTime);

            // check if checkbox has other value than configuration setting we want to update
            if (autoSendTime != ((CheckBox)sender).Checked)
            {
                ConfigHashtable["AutoSendTime"] = ((CheckBox)sender).Checked.ToString();
                ((MainWindow)this.Owner).SetConfigurationSettingText("LedConfiguration/Program/Defaults/AutoSendTime", ConfigHashtable["AutoSendTime"].ToString());
                ((MainWindow)this.Owner).MainWindowDateTimeControlsEnable(!Convert.ToBoolean(((CheckBox)sender).Checked.ToString()));
            }
        }

        private int GetModuleHeight()
        {
            int retval = 0;
            string wxh = markModuleTypeComboBox.Items[markModuleTypeComboBox.SelectedIndex].ToString();
            int xposition = wxh.IndexOf('x') + 1;

            retval = Convert.ToInt16(wxh.Substring(xposition, wxh.Length - xposition));

            return retval;
        }

        private int GetModuleWidth()
        {
            int retval = 0;
            string wxh = markModuleTypeComboBox.Items[markModuleTypeComboBox.SelectedIndex].ToString();
            int xposition = wxh.IndexOf('x');

            retval = Convert.ToInt16(wxh.Substring(0, xposition));

            return retval;
        }

        private void SetupMarkControls(int[] configListsValues)
        {
            SetupDropDown(markModuleTypeComboBox, configListsValues[9], configMaxValues[3]);

            SetupDropDown(markColorComboBox, configListsValues[0], configMaxValues[2]);

            markHeightComboBox.Items.Clear();
            markHeightComboBox.Items.AddRange(CreateNubmersList(100 * GetModuleHeight(), GetModuleHeight()));

            markWidthComboBox.Items.Clear();
            markWidthComboBox.Items.AddRange(CreateNubmersList(100 * GetModuleWidth(), GetModuleWidth()));


            SetupDropDown(markHeightComboBox, configListsValues[1] * 8 / GetModuleHeight(), maxHeightGlobalArray[ColorIndex] / (GetModuleHeight() / 8));

            SetupDropDown(markWidthComboBox, configListsValues[2] * 8 / GetModuleWidth(), MaxWidth / (GetModuleWidth() / 8));

            markScanModeComboBox.SelectedItem = markScanModeComboBox.Items[configListsValues[3]];
            markCheckCompatibilityComboBox.SelectedItem = markCheckCompatibilityComboBox.Items[configListsValues[4]];
            markSkipHeadersComboBox.SelectedItem = markSkipHeadersComboBox.Items[configListsValues[5]];
            SetupDropDown(markModeBlankingComboBox, configListsValues[6], configMaxValues[0]);
            SetupDropDown(markQualityComboBox, configListsValues[7], configMaxValues[1]);
            //markCloningComboBox.SelectedItem = markCloningComboBox.Items[configListsValues[8]];
            SetupDropDown(markCloningComboBox, configListsValues[8], configMaxValues[4]);



            //markFromPlaybillcheckBox.Checked = Convert.ToBoolean(configListsValues[10]);

            SetupDropDown(markParamComboBox, configListsValues[10], configMaxValues[6]);
            SetupDropDown(markUserComboBox, configListsValues[11], configMaxValues[5]);
        }

        private void GetMarkConfiguration()
        {
            try
            {
                int autoscan = 0;
                int[][] configWidthMaxValues = new int[1][];
                int[] configHeightMaxValues = new int[1];
                int[] configDeviceInfo = new int[3];


                CommunicationResult result = Communication.GetMarkConfig(
                    testWithoutControllerCheckBox.Checked,
                    out autoscan, out configListsValues,
                    out configMaxValues, out configHeightMaxValues, out configWidthMaxValues
                    , out reservedValues);

                if (result != CommunicationResult.Success)
                {
                    MessageBox.Show(result.ToString());
                    //markSetConfigButton.Enabled = false;
                }
                else
                {
                    maxHeightGlobalArray = new int[configHeightMaxValues.Length];
                    configHeightMaxValues.CopyTo(maxHeightGlobalArray, 0);

                    maxWidthGlobalArray[0] = new int[configWidthMaxValues[0].Length];
                    configWidthMaxValues[0].CopyTo(maxWidthGlobalArray[0], 0);
                    maxWidthGlobalArray[1] = new int[configWidthMaxValues[1].Length];
                    configWidthMaxValues[1].CopyTo(maxWidthGlobalArray[1], 0);


                    SetupMarkControls(configListsValues);

                    markSetConfigButton.Enabled = true;
                }
            }
            catch
            {
                //markSetConfigButton.Enabled = false;
                MessageBox.Show("There was a problem. Try again");
            }

            try
            {
                autoscanReady = SetupAutoscanValues();
                SetAutoscanColorByReady(autoscanReady);
                autoscanReady = false;
            }
            catch { }
        }

        private void markGetConfigButton_Click(object sender, EventArgs e)
        {
            GetScreenConfig();
            //GetMarkConfiguration();
        }

        private void SetupDropDown(ComboBox cb, int newIndex, int maxIndex)
        {
            if (newIndex >= maxIndex)
            {
                if (cb.Name == markHeightComboBox.Name || cb.Name == markWidthComboBox.Name)
                    newIndex = maxIndex;
                else
                    newIndex = maxIndex - 1;
            }

            if (maxIndex == 0)
            {
                if (cb.Name == markHeightComboBox.Name || cb.Name == markWidthComboBox.Name)
                {
                    newIndex = 1;
                    maxIndex = 1;
                }
                else if (cb.Name == markColorComboBox.Name || cb.Name == markScanModeComboBox.Name)
                {
                    newIndex = 0;
                    maxIndex = 1;
                }
                else
                {
                    cb.SelectedItem = null;
                    cb.SelectedIndex = -1;

                    string name = cb.Name;
                    name = name.Substring(0, name.LastIndexOf("ComboBox"));
                    name = string.Format("{0}Label", name);

                    Control label = markXXSettingsGroupBox.Controls.Find(name, false)[0];
                    label.Visible = false;
                    cb.Visible = false;


                    return;
                }
            }
            //else
            {
                string name = cb.Name;
                name = name.Substring(0, name.LastIndexOf("ComboBox"));
                name = string.Format("{0}Label", name);

                Control label = markXXSettingsGroupBox.Controls.Find(name, false)[0];
                label.Visible = true;

                cb.Visible = true;

                //if (newIndex >= maxIndex)
                //    newIndex = maxIndex - 1;

                if (cb.Name == "markHeightComboBox")
                {
                    //maxIndex /= GetModuleHeight();
                    maxIndex++;
                }
                if (cb.Name == markWidthComboBox.Name)
                {
                    //maxIndex /= GetModuleWidth();
                    maxIndex++;
                }


                if (cb.Items.Count < maxIndex)
                {
                    for (int i = cb.Items.Count; i < maxIndex; i++)
                    {
                        if (cb.Name == "markHeightComboBox")
                        {
                            if (i * GetModuleHeight() > 512)
                                break;

                            cb.Items.Add((i * GetModuleHeight()).ToString());
                            //maxIndex /= GetModuleHeight();
                            //maxIndex++;
                        }
                        else if (cb.Name == markWidthComboBox.Name)
                        {
                            if (i * GetModuleWidth() > 4096)
                                break;

                            cb.Items.Add((i * GetModuleWidth()).ToString());

                            //maxIndex /= GetModuleWidth();
                            //maxIndex++;
                        }
                        else if (cb.Name == markParamComboBox.Name)
                            cb.Items.Add("param" + (i + 1).ToString());
                        else if (cb.Name == markUserComboBox.Name)
                            cb.Items.Add("user" + (i + 1).ToString());
                        else
                            cb.Items.Add("value #" + (i + 1).ToString());
                    }
                }
                else if (cb.Items.Count > maxIndex)
                {
                    for (int i = cb.Items.Count - 1; i >= maxIndex; i--)
                    {
                        cb.Items.RemoveAt(i);
                    }
                }

                cb.SelectedItem = cb.Items[newIndex];
            }
        }

        private string[] CreateNubmersList(int maxNumber, int step)
        {
            List<string> retval = new List<string>();

            for (int i = 0; i < maxNumber / step; i++)
            {
                retval.Add((i * step).ToString());
            }

            return retval.ToArray();
        }

        private string[] CreateNubmersList(int maxIndex)
        {
            List<string> retval = new List<string>();

            for (int i = 0; i < maxIndex; i++)
            {
                retval.Add((i * 8).ToString());
            }

            return retval.ToArray();
        }

        private void SaveMarkControlsState()
        {
            configListsValues[0] = markColorComboBox.SelectedIndex;
            configListsValues[1] = Convert.ToInt32((string)markHeightComboBox.SelectedItem);
            configListsValues[2] = Convert.ToInt32((string)markWidthComboBox.SelectedItem) / 8;
            configListsValues[3] = markScanModeComboBox.SelectedIndex;
            configListsValues[4] = markCheckCompatibilityComboBox.SelectedIndex;
            configListsValues[5] = markSkipHeadersComboBox.SelectedIndex;
            configListsValues[6] = markModeBlankingComboBox.SelectedIndex;
            configListsValues[7] = markQualityComboBox.SelectedIndex;
            configListsValues[8] = markCloningComboBox.SelectedIndex;
            configListsValues[9] = markModuleTypeComboBox.SelectedIndex;
            configListsValues[10] = (markParamComboBox.SelectedItem != null) ? markParamComboBox.SelectedIndex : 0;
            configListsValues[11] = (markUserComboBox.SelectedItem != null) ? markUserComboBox.SelectedIndex : 0;
        }

        private void markSetConfigButton_Click(object sender, EventArgs e)
        {
            SaveMarkControlsState();
            SetScreenConfig();
        }

        private void markAutoscanButton_Click(object sender, EventArgs e)
        {
            maxCount = 6;

            //if(performReadScanPeriodicalyCheckBox.Checked)
            try
            {
                //if (!testWithoutControllerCheckBox.Checked && ((MainWindow)Owner).TestCurrentSerialPort() == 0)
                //{
                //    readScanStatusTextBox.Text = "No connection";
                //    readScanStatusTextBox.BackColor = Color.Red;
                //    readScanStatusTextBox.BackColor = Color.Red;
                //    return;
                //}

                //autoDevicesListBox.Items.Clear();


                //readScanStatusTextBox.Text = "No info";
                Color currentBackColor = Color.DarkOrange;

                readScanStatusTextBox.BackColor = currentBackColor;
                autoColorTextBox.BackColor = currentBackColor;
                autoHeightTextBox.BackColor = currentBackColor;
                autoWidthTextBox.BackColor = currentBackColor;
                autoScanModeTextBox.BackColor = currentBackColor;


                autoHubTextBox.BackColor = currentBackColor;
                controllerHubTextBox.BackColor = currentBackColor;
                screenHubTextBox.BackColor = currentBackColor;

                autoDevicesListBox.BackColor = currentBackColor;
                autoValuesListBox.BackColor = currentBackColor;



                int[] autoValues = new int[1];
                int[] deviceInfo = new int[1];
                bool ready = false;
                firstReadScan = true;
                byte fromValue = (byte)((fromScanCheckBox.Checked) ? 2 : ((markFromPlaybillcheckBox.Checked) ? 1 : 0));
                CommunicationResult result = Communication.SetMarkAutoscan(
                    testWithoutControllerCheckBox.Checked,
                    (byte)markColorComboBox.SelectedIndex, (byte)markModuleTypeComboBox.SelectedIndex,
                    fromValue,
                    //markFromPlaybillcheckBox.Checked,
                    1,
                    out ready, out autoValues, out deviceInfo);

                if (result != CommunicationResult.Success)
                    MessageBox.Show(result.ToString());


                if (!ready)
                {
                    readScanStatusTextBox.Text = "Busy";

                    currentBackColor = Color.Gainsboro;

                    readScanStatusTextBox.BackColor = currentBackColor;
                    autoColorTextBox.BackColor = currentBackColor;
                    autoHeightTextBox.BackColor = currentBackColor;
                    autoWidthTextBox.BackColor = currentBackColor;
                    autoScanModeTextBox.BackColor = currentBackColor;




                    autoHubTextBox.BackColor = currentBackColor;
                    controllerHubTextBox.BackColor = currentBackColor;
                    screenHubTextBox.BackColor = currentBackColor;

                    autoDevicesListBox.BackColor = currentBackColor;
                    autoValuesListBox.BackColor = currentBackColor;

                    //MessageBox.Show("Busy. Application will try again in 5 seconds.");

                    //if (readAutoTimer != null)
                    //{
                    //    readAutoTimer.Stop();
                    //    readAutoTimer.Dispose();
                    //    readAutoTimer = null;

                    //    nextReadTimeInfoLabel.Text = "";
                    //}

                    //readAutoTimer = new System.Windows.Forms.Timer();
                    //readAutoTimer.Tick += readAutoTimer_Tick;
                    //readAutoTimer.Interval = 1000;
                    //readAutoTimer.Start();
                }
                else
                {
                    readScanStatusTextBox.Text = "Success";
                    currentBackColor = Color.DarkKhaki;

                    readScanStatusTextBox.BackColor = currentBackColor;
                    autoColorTextBox.BackColor = currentBackColor;
                    autoHeightTextBox.BackColor = currentBackColor;
                    autoWidthTextBox.BackColor = currentBackColor;
                    autoScanModeTextBox.BackColor = currentBackColor;


                    autoHubTextBox.BackColor = hubColor;
                    controllerHubTextBox.BackColor = hubColor;
                    screenHubTextBox.BackColor = hubColor;

                    autoDevicesListBox.BackColor = currentBackColor;
                    autoValuesListBox.BackColor = currentBackColor;

                    //autoColorTextBox.Text = GetStringValue(autoColorTextBox, autoValues[0]);
                    //autoHeightTextBox.Text = GetStringValue(autoHeightTextBox, autoValues[1]);
                    //autoWidthTextBox.Text = GetStringValue(autoWidthTextBox, autoValues[2]);
                    //autoScanModeTextBox.Text =  GetStringValue(autoScanModeTextBox, autoValues[3]);
                    //autoHubTextBox.Text = GetStringValue(autoHubTextBox, autoValues[4]);

                }

                autoColorTextBox.Text = GetStringValue(autoColorTextBox, autoValues[2]);
                autoHeightTextBox.Text = GetStringValue(autoHeightTextBox, autoValues[0]);
                autoWidthTextBox.Text = GetStringValue(autoWidthTextBox, autoValues[1]);
                autoScanModeTextBox.Text = GetStringValue(autoScanModeTextBox, autoValues[2]);

                autoHubTextBox.Text = GetStringValue(autoHubTextBox, autoValues[3]);
                controllerHubTextBox.Text = GetStringValue(controllerHubTextBox, autoValues[3]);
                screenHubTextBox.Text = GetStringValue(screenHubTextBox, autoValues[3]);

                SetupDevicesLists(deviceInfo);

                if (markFromPlaybillcheckBox.Checked)
                {
                    markFromPlaybillcheckBox.Checked = false;
                    GetMarkConfiguration();
                }
                nextReadTimeInfoLabel.Text = "";


                if (markReadAutoAfterAutoscanCheckBox.Checked && !ready)
                {
                    readScanStatusTextBox.Text = string.Format("Next read in {0} s", maxCount);
                    
                    //if (performReadScanPeriodicalyCheckBox.Checked)
                    //    readScanPeriodNumericUpDown.Value = 10;
                    //else
                    {
                        if (readAutoTimer != null)
                        {
                            readAutoTimer.Stop();
                            readAutoTimer.Dispose();
                            readAutoTimer = null;

                            nextReadTimeInfoLabel.Text = "";
                        }

                        readAutoTimer = new System.Windows.Forms.Timer();
                        readAutoTimer.Tick += readAutoTimer_Tick;
                        readAutoTimer.Interval = 1000;
                        readAutoTimer.Start();
                    }
                }
            }
            catch
            {
                MessageBox.Show("There was a problem. Try again");
            }
        }

        private void markResetButton_Click(object sender, EventArgs e)
        {
            try
            {
                //CommunicationResult result = Communication.SetAutoscan(2);

                //            if (result != CommunicationResult.Success)
                //              MessageBox.Show(result.ToString());

            }
            catch
            {
                MessageBox.Show("There was a problem. Try again");
            }
        }

        private void markGetAutoscanButton_Click(object sender, EventArgs e)
        {
            //if (!testWithoutControllerCheckBox.Checked && ((MainWindow)Owner).TestCurrentSerialPort() == 0)
            //{
            //    readScanStatusTextBox.Text = "No connection";
            //    readScanStatusTextBox.BackColor = Color.Red;
            //    return;
            //} 

            ////readScanStatusTextBox.Text = "No info";
            //Color currentBackColor = Color.DarkOrange;

            //readScanStatusTextBox.BackColor = currentBackColor;
            //autoHeightTextBox.BackColor = currentBackColor;
            //autoWidthTextBox.BackColor = currentBackColor;
            //autoScanModeTextBox.BackColor = currentBackColor;
            //autoHubTextBox.BackColor = currentBackColor;
            //autoDevicesListBox.BackColor = currentBackColor;
            //autoValuesListBox.BackColor = currentBackColor;

            if (readAutoTimer != null)
            {
                readAutoTimer.Stop();
                readAutoTimer.Dispose();
                readAutoTimer = null;

                nextReadTimeInfoLabel.Text = "";
            }

            maxCount = 4;
            timerCounter = 0;

            readScanStatusTextBox.Text = string.Format("Next read in {0} s", maxCount);

            readAutoTimer = new System.Windows.Forms.Timer();
            readAutoTimer.Tick += readAutoTimer_Tick;
            readAutoTimer.Interval = 1000;
            readAutoTimer.Start();

            //SetupAutoscanValues();
        }

        private void SetAutoscanColorByReady(bool ready)
        {
            Color currentBackColor;

            if (!ready)
            {
                readScanStatusTextBox.Text = "Busy";

                currentBackColor = Color.Gainsboro;

                readScanStatusTextBox.BackColor = currentBackColor;
                autoColorTextBox.BackColor = currentBackColor;
                autoHeightTextBox.BackColor = currentBackColor;
                autoWidthTextBox.BackColor = currentBackColor;

                autoScanModeTextBox.BackColor = currentBackColor;
                screenHubTextBox.BackColor = currentBackColor;
                controllerHubTextBox.BackColor = currentBackColor;

                autoDevicesListBox.BackColor = currentBackColor;
                autoValuesListBox.BackColor = currentBackColor;

                //MessageBox.Show("Busy. Application will try again in 5 seconds.");

                //if (readAutoTimer != null)
                //{
                //    readAutoTimer.Stop();
                //    readAutoTimer.Dispose();
                //    readAutoTimer = null;

                //    nextReadTimeInfoLabel.Text = "";
                //}

                if (readAutoTimer == null)
                {
                    readScanStatusTextBox.Text = string.Format("Next read in {0} s", maxCount);
                    
                    readAutoTimer = new System.Windows.Forms.Timer();
                    readAutoTimer.Tick += readAutoTimer_Tick;
                    readAutoTimer.Interval = 1000;
                    readAutoTimer.Start();
                }

            }
            else
            {
                readScanStatusTextBox.Text = "Success";
                currentBackColor = Color.DarkKhaki;

                readScanStatusTextBox.BackColor = currentBackColor;
                autoColorTextBox.BackColor = currentBackColor;
                autoHeightTextBox.BackColor = currentBackColor;
                autoWidthTextBox.BackColor = currentBackColor;
                autoScanModeTextBox.BackColor = currentBackColor;

                autoHubTextBox.BackColor = hubColor;
                controllerHubTextBox.BackColor = hubColor;
                screenHubTextBox.BackColor = hubColor;

                autoDevicesListBox.BackColor = currentBackColor;
                autoValuesListBox.BackColor = currentBackColor;

                if (readAutoTimer != null)
                {
                    readAutoTimer.Stop();
                    readAutoTimer.Dispose();
                    readAutoTimer = null;

                }
                nextReadTimeInfoLabel.Text = "";

                if (performReadScanPeriodicalyCheckBox.Checked)
                {
                    readAutoTimer = new System.Windows.Forms.Timer();
                    readAutoTimer.Tick += readAutoTimerWithPeriod_Tick;
                    readAutoTimer.Interval = 1000;
                    readAutoTimer.Start();
                }

                //autoColorTextBox.Text = GetStringValue(autoColorTextBox, autoValues[0]);
                //autoHeightTextBox.Text = GetStringValue(autoHeightTextBox, autoValues[1]);
                //autoWidthTextBox.Text = GetStringValue(autoWidthTextBox, autoValues[2]);
                //autoScanModeTextBox.Text =  GetStringValue(autoScanModeTextBox, autoValues[3]);
                //autoHubTextBox.Text = GetStringValue(autoHubTextBox, autoValues[4]);

            }
        }

        private bool SetupAutoscanValues()
        {
            //maxCount = 10;

            bool ready = false;
            int[] autoValues = new int[1];
            int[] deviceInfo = new int[1];

            try
            {
                //if (!testWithoutControllerCheckBox.Checked && ((MainWindow)Owner).TestCurrentSerialPort() == 0)
                //{
                //    readScanStatusTextBox.Text = "No connection";
                //    readScanStatusTextBox.BackColor = Color.Red;
                //    return;
                //}
                /*if (readAutoTimer != null)
                {
                    readAutoTimer.Stop();
                    readAutoTimer.Dispose();
                    readAutoTimer = null;
                }*/

                CommunicationResult result = Communication.GetMarkAutoscan(

                testWithoutControllerCheckBox.Checked,
                    out ready, out autoValues, out deviceInfo);

                //if (result != CommunicationResult.Success)
                //    MessageBox.Show(result.ToString());

                autoColorTextBox.Text = GetStringValue(autoColorTextBox, autoValues[2]);
                autoHeightTextBox.Text = GetStringValue(autoHeightTextBox, autoValues[0]);
                autoWidthTextBox.Text = GetStringValue(autoWidthTextBox, autoValues[1]);
                autoScanModeTextBox.Text = GetStringValue(autoScanModeTextBox, autoValues[2]);

                autoHubTextBox.Text = GetStringValue(autoHubTextBox, autoValues[3]);
                controllerHubTextBox.Text = GetStringValue(controllerHubTextBox, autoValues[3]);
                screenHubTextBox.Text = GetStringValue(screenHubTextBox, autoValues[3]);

                SetupDevicesLists(deviceInfo);


            }
            catch
            {
                // MessageBox.Show("There was a problem. Try again");
            }

            return ready;
        }

        private string GetStringValue(Control c, int numberValue)
        {
            string retval = "NO DATA";

            if ((c.Name == autoHeightTextBox.Name) || (c.Name == autoWidthTextBox.Name))
            {
                if (numberValue == 0)
                    retval = "NO DATA";
                else if (c.Name == autoWidthTextBox.Name)
                    retval = (numberValue * 8).ToString();
                else
                    retval = (numberValue).ToString();
            }
            else if (c.Name == autoColorTextBox.Name)
            {
                numberValue = numberValue / 16;
                switch (numberValue)
                {
                    case 15:
                        retval = "NO DATA";
                        break;
                    case 0:
                        retval = "Mono";
                        break;
                    case 1:
                        retval = "RG";
                        break;
                    case 2:
                        retval = "RGB";
                        break;
                    case 3:
                        retval = "Full color";
                        break;
                }
            }
            else if (c.Name == autoScanModeTextBox.Name)
            {
                numberValue = numberValue & 0x0F;
                switch (numberValue)
                {
                    case 255:
                        retval = "NO DATA";
                        break;
                    case 0:
                        retval = "1/16";
                        break;
                    case 1:
                        retval = "1/8";
                        break;
                    case 2:
                        retval = "1/4";
                        break;
                }
            }
            else if (c.Name == autoHubTextBox.Name)
            {
                switch (numberValue)
                {
                    case 0:
                        retval = "NIEPODŁĄCZONY";
                        //c.BackColor = Control.DefaultBackColor;
                        break;
                    case 1:
                        retval = "HUB12";
                        //c.BackColor = Control.DefaultBackColor;
                        break;
                    case 2:
                        retval = "HUB8";
                        //c.BackColor = Control.DefaultBackColor;
                        break;
                    case 3:
                        retval = "HUB8 W GNIEZDZIE HUB12";
                        c.BackColor = Color.Red;
                        break;
                    case 4:
                        retval = "HUB12 W GNISZDIEZ HUB8";
                        c.BackColor = Color.Red;
                        break;
                    case 5:
                        retval = "HUB8  POMIEDZY";
                        c.BackColor = Color.Red;
                        break;
                    case 6:
                        retval = "HUB12 POMIEDZY";
                        c.BackColor = Color.Red;
                        break;
                    case 255:
                        retval = "NO DATA";
                        break;
                }
            }
            else if (c.Name == controllerHubTextBox.Name)
            {
                switch (numberValue)
                {
                    case 0:
                        retval = "HUB8E";
                        hubColor = Color.DarkKhaki;
                        //c.BackColor = Control.DefaultBackColor;
                        break;
                    case 1:
                        retval = "HUB12";
                        hubColor = Color.DarkKhaki;
                        //c.BackColor = Control.DefaultBackColor;
                        break;
                    case 2:
                        retval = "HUB8";
                        hubColor = Color.DarkKhaki;
                        //c.BackColor = Control.DefaultBackColor;
                        break;
                    case 3:
                        retval = "HUB12";
                        hubColor = Color.Red;
                        c.BackColor = Color.Red;
                        break;
                    case 4:
                        retval = "HUB8";
                        hubColor = Color.Red;
                        c.BackColor = Color.Red;
                        break;
                    case 5:
                        retval = "HUB8/12";
                        hubColor = Color.Red;
                        c.BackColor = Color.Red;
                        break;
                    case 6:
                        retval = "HUB8/12";
                        hubColor = Color.Red;
                        c.BackColor = Color.Red;
                        break;
                    case 7:
                        retval = "OFF";
                        hubColor = Color.Yellow;
                        c.BackColor = Color.Yellow;
                        break;
                    case 255:
                        hubColor = Color.DarkKhaki;
                        retval = "NO DATA";
                        break;
                    default:
                        hubColor = Color.DarkKhaki;
                        retval = "NO DATA";
                        break;
                }
            }
            else if (c.Name == screenHubTextBox.Name)
            {
                switch (numberValue)
                {
                    case 0:
                        retval = "HUB8E";
                        hubColor = Color.DarkKhaki;
                        //c.BackColor = Control.DefaultBackColor;
                        break;
                    case 1:
                        retval = "HUB12";
                        hubColor = Color.DarkKhaki;
                        //c.BackColor = Control.DefaultBackColor;
                        break;
                    case 2:
                        retval = "HUB8";
                        hubColor = Color.DarkKhaki;
                        //c.BackColor = Control.DefaultBackColor;
                        break;
                    case 3:
                        retval = "HUB8";
                        hubColor = Color.Red;
                        c.BackColor = Color.Red;
                        break;
                    case 4:
                        retval = "HUB12";
                        hubColor = Color.Red;
                        c.BackColor = Color.Red;
                        break;
                    case 5:
                        retval = "HUB8";
                        hubColor = Color.Red;
                        c.BackColor = Color.Red;
                        break;
                    case 6:
                        retval = "HUB12";
                        hubColor = Color.Red;
                        c.BackColor = Color.Red;
                        break;
                    case 7:
                        retval = "OFF";
                        hubColor = Color.Yellow;
                        c.BackColor = Color.Yellow;
                        break;
                    case 255:
                        hubColor = Color.DarkKhaki;
                        retval = "NO DATA";
                        break;
                    default:
                        hubColor = Color.DarkKhaki;
                        retval = "NO DATA";
                        break;
                }
            }

            return retval;
        }

        private void SetupDevicesLists(int[] values)
        {
            autoValuesListBox.Items.Clear();
            autoDevicesListBox.Items.Clear();

            autoValuesListBox.Items.Add(string.Format("Voltage: \t\t{0}V", string.Format("{0:0.00}", values[2] / 100.0)));

            if ((values[0] & 1) != 0)
                autoDevicesListBox.Items.Add("Real-Time-Clock: \t Yes");
            else
                autoDevicesListBox.Items.Add("Real-Time-Clock: \t No");
            if ((values[0] & 2) != 0)
            {
                autoDevicesListBox.Items.Add("Temp. sensor: \t Yes");
                autoValuesListBox.Items.Add(string.Format("Temerature: \t{0}°C", string.Format("{0:0.0}", values[3] / 16.0)));

            }
            else
            {
                autoDevicesListBox.Items.Add("Temp. sensor: \t No");
                autoValuesListBox.Items.Add("Temerature: \t--°C");

            }
            if ((values[0] & 4) != 0)
            {
                autoDevicesListBox.Items.Add("Photosensor: \t Yes");
                autoValuesListBox.Items.Add(string.Format("Brightness: \t{0}", values[4]));

            }
            else
            {
                autoDevicesListBox.Items.Add("Photosensor: \t No");
                autoValuesListBox.Items.Add("Brightness: \t--");
            }
            if ((values[0] & 8) != 0)
                autoDevicesListBox.Items.Add("Memory card: \t Yes");
            else
                autoDevicesListBox.Items.Add("Memory card: \t No");
            if ((values[0] & 16) != 0)
            {
                autoDevicesListBox.Items.Add("Network card: \t Yes");
                MainWindow.controllerSettings.HasNetwork = true;
            }
            else
            {
                autoDevicesListBox.Items.Add("Network card: \t No");
                MainWindow.controllerSettings.HasNetwork = false;
            }
            if ((values[0] & 32) != 0)
                autoDevicesListBox.Items.Add("Bluetooth: \t Yes");
            else
                autoDevicesListBox.Items.Add("Bluetooth: \t No");
            //if ((values[0] & 64) != 0)
            //    autoDevicesListBox.Items.Add("---");
            //if ((values[0] & 128) != 0)
            //    autoDevicesListBox.Items.Add("---");


            autoValuesListBox.Items.Add(string.Format("Memory: \t\t{0} MB", values[1] / 16.0));
        }

        private void markHeightComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int lastWidthValue = (markWidthComboBox.SelectedItem != null) ? Convert.ToInt32(markWidthComboBox.SelectedItem.ToString()) : 1;
            markWidthComboBox.Items.Clear();
            markWidthComboBox.Items.AddRange(CreateNubmersList(100 * GetModuleWidth(), GetModuleWidth()));

            SetupDropDown(markWidthComboBox, lastWidthValue / GetModuleWidth(), MaxWidth / (GetModuleWidth() / 8));
        }

        private void markModuleTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                int maxHindex = markHeightComboBox.Items.Count - 1;
                int lastHeightValue = (markHeightComboBox.SelectedItem != null) ? Convert.ToInt32(markHeightComboBox.SelectedItem.ToString()) : 1;

                markHeightComboBox.Items.Clear();
                markHeightComboBox.Items.AddRange(CreateNubmersList(100 * GetModuleHeight(), GetModuleHeight()));

                //SetupDropDown(markHeightComboBox, 1, maxHeightGlobalArray[ColorIndex]);
                SetupDropDown(markHeightComboBox, lastHeightValue / GetModuleHeight(), maxHeightGlobalArray[ColorIndex] / (GetModuleHeight() / 8));

                int lastWidthValue = (markWidthComboBox.SelectedItem != null) ? Convert.ToInt32(markWidthComboBox.SelectedItem.ToString()) : 1;

                markWidthComboBox.Items.Clear();
                markWidthComboBox.Items.AddRange(CreateNubmersList(100 * GetModuleWidth(), GetModuleWidth()));

                SetupDropDown(markWidthComboBox, lastWidthValue / GetModuleWidth(), MaxWidth / (GetModuleWidth() / 8));
            }
            catch
            {
                MessageBox.Show("Error.");
            }
        }

        private void performReadScanPeriodicalyCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked)
            {
                readScanPeriodNumericUpDown.Enabled = true;

                if (readAutoTimer != null)
                {
                    readAutoTimer.Stop();
                    readAutoTimer.Dispose();
                    readAutoTimer = null;

                    nextReadTimeInfoLabel.Text = "";
                }

                periodTimerCounter = 0;
                readAutoTimer = new System.Windows.Forms.Timer();
                readAutoTimer.Tick += readAutoTimerWithPeriod_Tick;
                readAutoTimer.Interval = 1000;//(int)(readScanPeriodNumericUpDown.Value * 1000);
                readAutoTimer.Start();
            }
            else
            {
                readScanPeriodNumericUpDown.Enabled = false;

                if (readAutoTimer != null)
                {
                    readAutoTimer.Stop();
                    readAutoTimer.Dispose();
                    readAutoTimer = null;

                    nextReadTimeInfoLabel.Text = "";
                }
            }
        }

        private void readScanPeriodNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (performReadScanPeriodicalyCheckBox.Checked)
            {
                if (readAutoTimer != null)
                {
                    readAutoTimer.Stop();
                    readAutoTimer.Dispose();
                    readAutoTimer = null;

                    nextReadTimeInfoLabel.Text = "";
                }

                periodTimerCounter = 0;
                readAutoTimer = new System.Windows.Forms.Timer();
                readAutoTimer.Tick += readAutoTimerWithPeriod_Tick;
                readAutoTimer.Interval = 1000;// (int)(readScanPeriodNumericUpDown.Value * 1000);
                readAutoTimer.Start();
            }
        }

        private void markColorComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                int maxHindex = markHeightComboBox.Items.Count - 1;
                int lastHeightValue = (markHeightComboBox.SelectedItem != null) ? Convert.ToInt32(markHeightComboBox.SelectedItem.ToString()) : 1;
                markHeightComboBox.Items.Clear();
                markHeightComboBox.Items.AddRange(CreateNubmersList(100 * GetModuleHeight(), GetModuleHeight()));

                //SetupDropDown(markHeightComboBox, 1, maxHeightGlobalArray[ColorIndex]);

                SetupDropDown(markHeightComboBox, lastHeightValue / GetModuleHeight(), maxHeightGlobalArray[ColorIndex] / (GetModuleHeight() / 8));

                int lastWidthValue = (markWidthComboBox.SelectedItem != null) ? Convert.ToInt32(markWidthComboBox.SelectedItem.ToString()) : 1;
                markWidthComboBox.Items.Clear();
                markWidthComboBox.Items.AddRange(CreateNubmersList(100 * GetModuleWidth(), GetModuleWidth()));

                SetupDropDown(markWidthComboBox, lastWidthValue / GetModuleWidth(), MaxWidth / (GetModuleWidth() / 8));
            }
            catch
            {
                MessageBox.Show("Error.");
            }
        }

        private void markFromPlaybillcheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Color currentBackColor;

            if (((CheckBox)sender).Checked)
            {
                fromScanCheckBox.Checked = false;
                
                currentBackColor = Color.DarkOrange;

                markColorComboBox.BackColor = currentBackColor;
                markHeightComboBox.BackColor = currentBackColor;
                markWidthComboBox.BackColor = currentBackColor;
                markAutoscanButton.BackColor = currentBackColor;
            }
            else
            {
                currentBackColor = Color.White;//ComboBox.DefaultBackColor;

                markColorComboBox.BackColor = currentBackColor;
                markHeightComboBox.BackColor = currentBackColor;
                markWidthComboBox.BackColor = currentBackColor;

                currentBackColor = Control.DefaultBackColor;//Button.DefaultBackColor;

                markAutoscanButton.BackColor = currentBackColor;
            }
        }

        #endregion

        private void controllerSettingsTabControl_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (!typeChanging)
                e.Cancel = true ;
        }

        private void ScreenSettingsForm_Activated(object sender, EventArgs e)
        {
            if (getConfigOnReactive)
            {
                getConfigOnReactive = false;

                GetScreenConfig();
                
                //if (Communication.Type != CommunicationType.None)
                //{
                //    int baudrateIndex = 0;

                //    CommunicationResult result = Communication.GetIdAndBaudrate(out baudrateIndex);

                //    if (result == CommunicationResult.Success)
                //    {
                //        baudrateComboBox.SelectedItem = baudrateComboBox.Items[baudrateIndex];
                //        controllerIDTextBox.Text = Communication.CardID.ToString();
                //    }


                //    if (MainWindow.controllerSettings.HasNetwork)
                //    {
                //        ipSettingsGroupBox.Enabled = true;
                //        ipSettingsGroupBox.Visible = true;
                //        GetIP();
                //    }
                //    else
                //    {
                //        ipSettingsGroupBox.Enabled = false;
                //        ipSettingsGroupBox.Visible = false;
                //    }
                //}
                //else
                //{
                //    statusTextLabel.ForeColor = System.Drawing.Color.Red;
                //    statusTextLabel.Text = "NOT FOUND";
                //    controllerTypeTextLabel.Text = "---";
                //    controllerVersionTextLabel.Text = "---";
                //}

            }
        }

        private void cpower4200GetConfigButton_Click(object sender, EventArgs e)
        {
            GetScreenConfig();
        }

        private void saveSettingsButton_Click(object sender, EventArgs e)
        {
            System.IO.Stream filestream;

            SaveFileDialog savefile1 = new SaveFileDialog();
            
            savefile1.Filter = "controller settings files (*.lcs)|*.lcs";
            savefile1.FilterIndex = 1;
            savefile1.DefaultExt = "lcs";
            
            //if (ConfigHashtable["PathsSaveProgram"].ToString() != "Empty")
            //    savefile1.InitialDirectory = ConfigHashtable["PathsSaveProgram"].ToString();

            if (savefile1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if ((filestream = savefile1.OpenFile()) != null)
                    {
                        if(MainWindow.controllerSettings.Series == ControllerType.MARK_XX)
                            SaveMarkControlsState();
                        else if (MainWindow.controllerSettings.Type == ControllerType.C_Power1200 || MainWindow.controllerSettings.Type == ControllerType.C_Power2200 || MainWindow.controllerSettings.Type == ControllerType.C_Power3200)
                            Save1200ControlsState();
                        else if (MainWindow.controllerSettings.Type == ControllerType.C_Power4200 || MainWindow.controllerSettings.Type == ControllerType.C_Power5200)
                            Save4200ControlsState();


                        MainWindow.controllerSettings.maxHeightGlobalArray = maxHeightGlobalArray;
                        MainWindow.controllerSettings.maxWidthGlobalArray = maxWidthGlobalArray;
                        MainWindow.controllerSettings.configListsValues = configListsValues;
                        MainWindow.controllerSettings.configListsValues[1] /= 8; // divide height by 8
                        MainWindow.controllerSettings.configMaxValues = configMaxValues;
                        MainWindow.controllerSettings.reservedValues = reservedValues;
                        MainWindow.controllerSettings.screenConfigBytes = screenConfigBytes;


                        MainWindow.controllerSettings.ID = Convert.ToInt32(idNumericUpDown.Value); //int.Parse(controllerIDTextBox.Text);
                        MainWindow.controllerSettings.Baudrate = baudrateComboBox.SelectedIndex;

                        if (MainWindow.controllerSettings.HasNetwork)
                        {
                            MainWindow.controllerSettings.NetConfig = new NetworkConfiguration(controllerIpAddressBox.IPAddress, Convert.ToInt32(portNumericUpDown.Value),//int.Parse(portTextBox.Text),
                                gatewayIpAddressBox.IPAddress, maskIpAddressBox.IPAddress, passcodeIpAddressBox.IPAddress);
                        }

                        
                        
                        MainWindow.controllerSettings.SaveToFile(filestream);
                        filestream.Close();

                        //string progPath = savefile1.FileName.Remove(savefile1.FileName.LastIndexOf('\\'));

                        //SetConfigurationSettingText("LedConfiguration/Program/Other/Paths/SaveProgram", progPath);
                        //LoadConfiguration();
                    }
                }
                catch (Exception)
                {
                    //MessageBox.Show(messageBoxesHashTable["messageBoxMessage_couldNotOpenFile"].ToString());
                }
            }

        }

        private void loadSettingsButton_Click(object sender, EventArgs e)
        {
            System.IO.Stream filestream;

            OpenFileDialog openFile1 = new OpenFileDialog();
            openFile1.RestoreDirectory = true;

            //if (ConfigHashtable["PathsLoadProgram"].ToString() != "Empty")
            //    openFile1.InitialDirectory = ConfigHashtable["PathsLoadProgram"].ToString();

            
            openFile1.Filter = "controller settings files (*.lcs)|*.lcs";
            openFile1.FilterIndex = 1;

            if (openFile1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if ((filestream = openFile1.OpenFile()) != null)
                    {
                        //string progPath = openFile1.FileName.Remove(openFile1.FileName.LastIndexOf('\\'));

                        //SetConfigurationSettingText("LedConfiguration/Program/Other/Paths/LoadProgram", progPath);
                        //LoadConfiguration();


                        ControllerSettings loadedConfig = ControllerSettings.LoadFromFile(filestream);
                        filestream.Close();

                        if (MainWindow.controllerSettings.Series == loadedConfig.Series)
                        {
                            if (loadedConfig.Series == ControllerType.MARK_XX
                                || (loadedConfig.Series == ControllerType.C_PowerX200 && MainWindow.controllerSettings.Type == loadedConfig.Type))
                            {
                                MainWindow.controllerSettings = loadedConfig;

                                maxHeightGlobalArray = MainWindow.controllerSettings.maxHeightGlobalArray;
                                maxWidthGlobalArray = MainWindow.controllerSettings.maxWidthGlobalArray;
                                configListsValues = MainWindow.controllerSettings.configListsValues;
                                configMaxValues = MainWindow.controllerSettings.configMaxValues;
                                reservedValues = MainWindow.controllerSettings.reservedValues;
                                screenConfigBytes = MainWindow.controllerSettings.screenConfigBytes;

                                idNumericUpDown.Value = MainWindow.controllerSettings.ID;
                                //controllerIDTextBox.Text = MainWindow.controllerSettings.ID.ToString();
                                baudrateComboBox.SelectedIndex = MainWindow.controllerSettings.Baudrate;

                                if (MainWindow.controllerSettings.HasNetwork)
                                {
                                    controllerIpAddressBox.IPAddress = MainWindow.controllerSettings.NetConfig.ControllerIP;
                                    portNumericUpDown.Value = MainWindow.controllerSettings.NetConfig.Port;
                                    //portTextBox.Text = MainWindow.controllerSettings.NetConfig.Port.ToString();
                                    gatewayIpAddressBox.IPAddress = MainWindow.controllerSettings.NetConfig.GatewayIP;
                                    maskIpAddressBox.IPAddress = MainWindow.controllerSettings.NetConfig.SubnetMask;
                                    passcodeIpAddressBox.IPAddress = MainWindow.controllerSettings.NetConfig.NetworkIDCode;

                                    ipSettingsGroupBox.Visible = true;
                                    ipSettingsGroupBox.Enabled = true;
                                }
                                else
                                {
                                    ipSettingsGroupBox.Visible = false;
                                    ipSettingsGroupBox.Enabled = false;
                                }

                                #region Choose proper method to setup controls
                                switch (MainWindow.controllerSettings.Series)
                                {
                                    case ControllerType.MARK_XX:
                                        {
                                            SetupMarkControls(configListsValues);
                                            break;
                                        }
                                    case ControllerType.C_PowerX200:
                                        {
                                            switch (MainWindow.controllerSettings.Type)
                                            {
                                                case ControllerType.C_Power1200:
                                                    {
                                                        Setup1200Controls(screenConfigBytes);
                                                        break;
                                                    }
                                                case ControllerType.C_Power2200:
                                                    {
                                                        Setup1200Controls(screenConfigBytes);
                                                        break;
                                                    }
                                                case ControllerType.C_Power3200:
                                                    {
                                                        Setup1200Controls(screenConfigBytes);
                                                        break;
                                                    }
                                                case ControllerType.C_Power4200:
                                                    {
                                                        Setup4200Controls(screenConfigBytes);
                                                        break;
                                                    }
                                                case ControllerType.C_Power5200:
                                                    {
                                                        Setup4200Controls(screenConfigBytes);
                                                        break;
                                                    }
                                            }
                                            break;
                                        }
                                }
                                #endregion

                                markSetConfigButton.Enabled = true;
                            }
                            else
                            {
                                MessageBox.Show("You try to load config of other controller!");
                            }
                        }
                        else
                        {
                            MessageBox.Show("You try to load config of other controller series!");
                        }
                    }
                }
                catch (Exception)
                {
                    int a = 6;
                    a += 1;
                    
                    //MessageBox.Show(messageBoxesHashTable["messageBoxMessage_couldNotOpenFile"].ToString());
                }
            }
        }

        private void cpower4200SetConfigButton_Click(object sender, EventArgs e)
        {
            Save4200ControlsState();
            SetScreenConfig();
        }

        private void offlineSeriesComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Communication.Type == CommunicationType.None)
            {
                MainWindow.controllerSettings.Series = (ControllerType)Enum.Parse(typeof(ControllerType), ((string)offlineSeriesComboBox.SelectedItem).Replace('-', '_'));
                
                switch (MainWindow.controllerSettings.Series)
                {
                    case ControllerType.MARK_XX:
                        {
                            typeChanging = true;
                            controllerSettingsTabControl.SelectedTab = controllerSettingsTabControl.TabPages["markXXSettingsTabPage"];
                            typeChanging = false;

                            offlineTypeComboBox.Items.Clear();
                            offlineTypeComboBox.Items.Add(ControllerType.unknown);
                            offlineTypeComboBox.Items.Add(ControllerType.MARK_24.ToString().Replace('_', '-'));
                            offlineTypeComboBox.Items.Add(ControllerType.MARK_56.ToString().Replace('_', '-'));
                            offlineTypeComboBox.Items.Add(ControllerType.MARK_120.ToString().Replace('_', '-'));
                            offlineTypeComboBox.Enabled = true;
                            break;
                        }
                    case ControllerType.C_PowerX200:
                        {
                            typeChanging = true;

                            switch (MainWindow.controllerSettings.Type)
                            {
                                case ControllerType.C_Power1200:
                                    {
                                        controllerSettingsTabControl.SelectedTab = controllerSettingsTabControl.TabPages["cpower1200SettingsTabPage"];
                                        break;
                                    }
                                case ControllerType.C_Power2200:
                                    {
                                        controllerSettingsTabControl.SelectedTab = controllerSettingsTabControl.TabPages["cpower1200SettingsTabPage"];
                                        break;
                                    }
                                case ControllerType.C_Power3200:
                                    {
                                        controllerSettingsTabControl.SelectedTab = controllerSettingsTabControl.TabPages["cpower1200SettingsTabPage"];
                                        break;
                                    }
                                case ControllerType.C_Power4200:
                                    {
                                        controllerSettingsTabControl.SelectedTab = controllerSettingsTabControl.TabPages["cpower4200SettingsTabPage"];

                                        break;
                                    }
                                case ControllerType.C_Power5200:
                                    {
                                        controllerSettingsTabControl.SelectedTab = controllerSettingsTabControl.TabPages["cpower4200SettingsTabPage"];

                                        break;
                                    }
                            }
                            

                            controllerSettingsTabControl.SelectedTab = controllerSettingsTabControl.TabPages["markXXSettingsTabPage"];
                            typeChanging = false;

                            offlineTypeComboBox.SelectedIndex = 0;
                            offlineTypeComboBox.Enabled = true;
                            break;
                        }
                }
            }
        }

        private void offlineTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Communication.Type == CommunicationType.None)
            {
                MainWindow.controllerSettings.Type = (ControllerType)Enum.Parse(typeof(ControllerType), ((string)offlineTypeComboBox.SelectedItem).Replace('-', '_'));

                switch (MainWindow.controllerSettings.Series)
                {
                    case ControllerType.MARK_XX:
                        {
                            offlineTypeComboBox.Items.Clear();
                            offlineTypeComboBox.Items.Add(ControllerType.unknown);
                            offlineTypeComboBox.Items.Add(ControllerType.MARK_24.ToString().Replace('_', '-'));
                            offlineTypeComboBox.Items.Add(ControllerType.MARK_56.ToString().Replace('_', '-'));
                            offlineTypeComboBox.Items.Add(ControllerType.MARK_120.ToString().Replace('_', '-'));
                            offlineTypeComboBox.Enabled = true;
                            break;
                        }
                    case ControllerType.C_PowerX200:
                        {
                            offlineTypeComboBox.Items.Clear();
                            offlineTypeComboBox.Items.Add(ControllerType.unknown);
                            offlineTypeComboBox.Items.Add(ControllerType.C_Power1200.ToString().Replace('_', '-'));
                            offlineTypeComboBox.Items.Add(ControllerType.C_Power2200.ToString().Replace('_', '-'));
                            offlineTypeComboBox.Items.Add(ControllerType.C_Power3200.ToString().Replace('_', '-'));
                            offlineTypeComboBox.Items.Add(ControllerType.C_Power4200.ToString().Replace('_', '-'));
                            offlineTypeComboBox.Items.Add(ControllerType.C_Power5200.ToString().Replace('_', '-'));

                            offlineTypeComboBox.SelectedIndex = 0;
                            offlineTypeComboBox.Enabled = true;
                            break;
                        }
                    case ControllerType.unknown:
                        {
                            offlineTypeComboBox.Items.Clear();
                            offlineTypeComboBox.Items.Add(ControllerType.unknown);
                            offlineTypeComboBox.SelectedIndex = 0;

                            offlineTypeComboBox.Enabled = false;
                            break;
                        }
                }
            }
        }

        private void fromScanCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Color currentBackColor;

            if (((CheckBox)sender).Checked)
            {
                markFromPlaybillcheckBox.Checked = false;

                currentBackColor = Color.DarkOrange;

                markColorComboBox.BackColor = currentBackColor;
                markHeightComboBox.BackColor = currentBackColor;
                markWidthComboBox.BackColor = currentBackColor;
                markAutoscanButton.BackColor = currentBackColor;
            }
            else
            {
                currentBackColor = Color.White;//ComboBox.DefaultBackColor;

                markColorComboBox.BackColor = currentBackColor;
                markHeightComboBox.BackColor = currentBackColor;
                markWidthComboBox.BackColor = currentBackColor;

                currentBackColor = Control.DefaultBackColor;//Button.DefaultBackColor;

                markAutoscanButton.BackColor = currentBackColor;
            }
        }

        private void testWithoutControllerCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked == true)
            {
                this.Height = windowMaxHeight;
                this.Width = windowMaxWidth;

                saveSettingsButton.Visible = true;
                loadSettingsButton.Visible = true;

                controllerSettingsTabControl.Enabled = true;
                controllerSettingsTabControl.Visible = true;
                iDAndBaudrateGroupBox.Visible = true;

                offlineGroupBox.Visible = false;
                statusTextLabel.ForeColor = System.Drawing.Color.Gray;
                statusTextLabel.Text = "OFFLINE TEST";
                
                controllerSettingsTabControl.Visible = true;
                controllerSettingsTabControl.Enabled = true;

                if (controllerSettingsTabControl.SelectedTab == null ||
                    (controllerSettingsTabControl.SelectedTab != null
                    && controllerSettingsTabControl.SelectedTab.Name != "markXXSettingsTabPage"))
                {
                    controllerSettingsTabControl.TabPages.Clear();
                    foreach (TabPage tab in settingsPages)
                    {
                        if (tab.Name == "markXXSettingsTabPage")
                            controllerSettingsTabControl.TabPages.Add(tab);
                    }
                }
                controllerSettingsTabControl.SelectedTab = controllerSettingsTabControl.TabPages["markXXSettingsTabPage"];
                            
            }
        }
    }
}
