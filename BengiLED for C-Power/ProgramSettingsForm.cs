using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Collections;

namespace BengiLED_for_C_Power
{
    public partial class ProgramSettingsForm : Form
    {
        #region Private fields
        private int[] maxHeightGlobalArray = new int[2];
        private int[][] maxWidthGlobalArray = new int[2][];
        private byte[] reservedValues = new byte[20];
        private System.Windows.Forms.Timer readAutoTimer = null;
        private int maxCount = 10;
        private int timerCounter = 0;
        private int periodTimerCounter = 0;
        private bool autoscanReady = false;
        private bool firstReadScan = false;
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

        public ProgramSettingsForm()
        {
            InitializeComponent();


            this.Text = string.Format("{0} - {1}", this.Text, MainWindow.RetrieveLinkerTimestamp());
        }

        private void LoadSettings()
        {
            bool boolVar = false;
            
            // autosending time on connect
            bool.TryParse(ConfigHashtable["AutoSendTime"].ToString(), out boolVar);
            autoSendTimeCheckBox.Checked = boolVar;
        }

        void readAutoTimer_Tick(object sender, EventArgs e)
        {
            timerCounter++;
            nextReadTimeInfoLabel.Text = string.Format("Next read in {0} s", maxCount - timerCounter);

            //if (timerCounter == maxCount - 2)
            //    autoscanReady = SetupAutoscanValues();
            //else 
            if (timerCounter == maxCount-1)
            {
                autoscanReady = SetupAutoscanValues();

                readScanStatusTextBox.Text = "No info";
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

                readScanStatusTextBox.Text = "No info";
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

        private void ProgramSettingsForm_Shown(object sender, EventArgs e)
        {
            LoadSettings();
            //Communication.Type = CommunicationType.Serial;

            ((MainWindow)Owner).SearchForScreen();//false);
            if (Communication.Type != CommunicationType.None)
            {
                //int id = 0;
                int baudrateIndex = 0;

                CommunicationResult result =  Communication.GetIdAndBaudrate(out baudrateIndex);

                if (result == CommunicationResult.Success)
                {
                    baudrateComboBox.SelectedItem = baudrateComboBox.Items[baudrateIndex];
                    controllerIDTextBox.Text = Communication.CardID.ToString();
                }
            }
            
            if (((MainWindow)Owner).logWindow.Visible)
            {
                Thread.Sleep(2000);
                ((MainWindow)Owner).logWindow.Hide();
            }


            markHeightComboBox.Items.Clear();
            markHeightComboBox.Items.AddRange(CreateNubmersList(100));

            markWidthComboBox.Items.Clear();
            markWidthComboBox.Items.AddRange(CreateNubmersList(100));
            /*
            markColorComboBox.SelectedItem = markColorComboBox.Items[2];
            try
            {
                markHeightComboBox.SelectedItem = markHeightComboBox.Items[16 / 8];
                markWidthComboBox.SelectedItem = markWidthComboBox.Items[128 / 8];
            }
            catch { }
            markScanModeComboBox.SelectedItem = markScanModeComboBox.Items[2];
            markCheckCompatibilityComboBox.SelectedItem = markCheckCompatibilityComboBox.Items[1];
            markSkipHeadersComboBox.SelectedItem = markSkipHeadersComboBox.Items[1];
            markModeBlankingComboBox.SelectedItem = markModeBlankingComboBox.Items[4];
            markQualityComboBox.SelectedItem = markQualityComboBox.Items[5];
            markCloningComboBox.SelectedItem = markCloningComboBox.Items[1];

            markModuleWidthComboBox.SelectedItem = markModuleWidthComboBox.Items[1];
            markModuleHeightComboBox.SelectedItem = markModuleHeightComboBox.Items[2];*/

            if (markScanModeComboBox.Items.Count > 3)
            {
                markScanModeComboBox.Items.Remove("1/2");
            }
        }

        private void ProgramSettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            if (readAutoTimer != null)
            {
                readAutoTimer.Stop();
                readAutoTimer.Dispose();
                readAutoTimer = null;

                nextReadTimeInfoLabel.Text = "";
            }
            performReadScanPeriodicalyCheckBox.Checked = false;
            periodTimerCounter = 0;
            timerCounter = 0;

            if (e.CloseReason == CloseReason.UserClosing)
                e.Cancel = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DateTime tmpd = DateTime.Now;
            
            CommunicationResult result = Communication.SetTime(tmpd);//DateTime.Now);
            //MessageBox.Show(result.ToString());
            if(result == CommunicationResult.Success)
                MessageBox.Show(result.ToString() + "! Date:\n" + tmpd.DayOfWeek + ", " + tmpd.ToLongDateString() + ", " + tmpd.ToLongTimeString());
       
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DateTime tmpd; //= SerialCommunication.GetTime();
            CommunicationResult result = Communication.GetTime(out tmpd);
            //MessageBox.Show(result.ToString());
            MessageBox.Show(result.ToString() + "! Date:\n" + tmpd.DayOfWeek + ", " + tmpd.ToLongDateString() + ", " + tmpd.ToLongTimeString());
        }

        private void button3_Click(object sender, EventArgs e)
        {
            CommunicationResult result = Communication.DeleteFile("*.*");
            MessageBox.Show(result.ToString());
        }

        private void button4_Click(object sender, EventArgs e)
        {
            CommunicationResult result = Communication.Format();
            MessageBox.Show(result.ToString());
        }

        private void button5_Click(object sender, EventArgs e)
        {
            CommunicationResult result = Communication.RestartApp();
            MessageBox.Show(result.ToString());
        }

        private void button6_Click(object sender, EventArgs e)
        {
            CommunicationResult result = Communication.RestartHardware();
            MessageBox.Show(result.ToString());
        }

        private void button7_Click(object sender, EventArgs e)
        {
            //string[] ver = SerialCommunication.GetVersion();
            //MessageBox.Show(ver[1], ver[0]);
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

        private void GetMarkConfiguration()
        {
            try
            {
                //if (!testWithoutControllerCheckBox.Checked && ((MainWindow)Owner).TestCurrentSerialPort() == 0)
                //{
                //    readScanStatusTextBox.Text = "No connection";
                //    readScanStatusTextBox.BackColor = Color.Red;
                //    return;
                //}

                int autoscan = 0;
                int[] configListsValues = new int[12];
                int[] configMaxValues = new int[13];
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
                    markSetConfigButton.Enabled = false;
                }
                else
                {
                    maxHeightGlobalArray = new int[configHeightMaxValues.Length];
                    configHeightMaxValues.CopyTo(maxHeightGlobalArray, 0);

                    maxWidthGlobalArray[0] = new int[configWidthMaxValues[0].Length];
                    configWidthMaxValues[0].CopyTo(maxWidthGlobalArray[0], 0);
                    maxWidthGlobalArray[1] = new int[configWidthMaxValues[1].Length];
                    configWidthMaxValues[1].CopyTo(maxWidthGlobalArray[1], 0);


                    //SetupDropDown(markModuleHeightComboBox, configListsValues[9], configMaxValues[3]);
                    //SetupDropDown(markModuleWidthComboBox, configListsValues[11], configMaxValues[4]);
                    //SetupDropDown(markModuleTypeComboBox, configListsValues[11], configMaxValues[4]);
                    SetupDropDown(markModuleTypeComboBox, configListsValues[9], configMaxValues[3]);

                    SetupDropDown(markColorComboBox, configListsValues[0], configMaxValues[2]);

                    markHeightComboBox.Items.Clear();
                    markHeightComboBox.Items.AddRange(CreateNubmersList(100 * GetModuleHeight(), GetModuleHeight()));

                    markWidthComboBox.Items.Clear();
                    markWidthComboBox.Items.AddRange(CreateNubmersList(100 * GetModuleWidth(), GetModuleWidth()));


                    SetupDropDown(markHeightComboBox, configListsValues[1] * 8 / GetModuleHeight(), maxHeightGlobalArray[ColorIndex] / (GetModuleHeight() / 8));

                    SetupDropDown(markWidthComboBox, configListsValues[2] * 8 / GetModuleWidth(), MaxWidth / (GetModuleWidth() / 8));
                    //switch(configMaxValues[5])
                    //{
                    //    case 16:
                    //        SetupDropDown(markWidthComboBox, configListsValues[2] * 8 / GetModuleWidth(), configMaxValues[6] * 16 / GetModuleWidth());
                    //        break;
                    //    case 32:
                    //        SetupDropDown(markWidthComboBox, configListsValues[2] * 8 / GetModuleWidth(), configMaxValues[7] * 16 / GetModuleWidth());
                    //        break;
                    //    case 48:
                    //        SetupDropDown(markWidthComboBox, configListsValues[2] * 8 / GetModuleWidth(), configMaxValues[8] * 16 / GetModuleWidth());
                    //        break;
                    //    case 64:
                    //        SetupDropDown(markWidthComboBox, configListsValues[2] * 8 / GetModuleWidth(), configMaxValues[9] * 16 / GetModuleWidth());
                    //        break;
                    //    case 80:
                    //        SetupDropDown(markWidthComboBox, configListsValues[2] * 8 / GetModuleWidth(), configMaxValues[10] * 16 / GetModuleWidth());
                    //        break;
                    //    case 96:
                    //        SetupDropDown(markWidthComboBox, configListsValues[2] * 8 / GetModuleWidth(), configMaxValues[11] * 16 / GetModuleWidth());
                    //        break;
                    //    case 112:
                    //        SetupDropDown(markWidthComboBox, configListsValues[2] * 8 / GetModuleWidth(), configMaxValues[12] * 16 / GetModuleWidth());
                    //        break;
                    //    case 128:
                    //        SetupDropDown(markWidthComboBox, configListsValues[2] * 8 / GetModuleWidth(), configMaxValues[13] * 16 / GetModuleWidth());
                    //        break;
                    //    default:
                    //        SetupDropDown(markWidthComboBox, configListsValues[2] * 8 / GetModuleWidth(), configMaxValues[6] * 16 / GetModuleWidth());
                    //        break;
                    //}
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

                    markSetConfigButton.Enabled = true;
                }
            }
            catch
            {
                markSetConfigButton.Enabled = false;
                MessageBox.Show("There was a problem. Try again");
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            GetMarkConfiguration();
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
                if(cb.Name == markWidthComboBox.Name)
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
                retval.Add((i*8).ToString());
            }

            return retval.ToArray();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            try
            {
                //if (!testWithoutControllerCheckBox.Checked && ((MainWindow)Owner).TestCurrentSerialPort() == 0)
                //{
                //    readScanStatusTextBox.Text = "No connection";
                //    readScanStatusTextBox.BackColor = Color.Red;
                //    return;
                //}

                int[] configValues = new int[12];

                configValues[0] = markColorComboBox.SelectedIndex;
                configValues[1] = Convert.ToInt32((string)markHeightComboBox.SelectedItem);
                configValues[2] = Convert.ToInt32((string)markWidthComboBox.SelectedItem) / 8;
                configValues[3] = markScanModeComboBox.SelectedIndex;
                configValues[4] = markCheckCompatibilityComboBox.SelectedIndex;
                configValues[5] = markSkipHeadersComboBox.SelectedIndex;
                configValues[6] = markModeBlankingComboBox.SelectedIndex;
                configValues[7] = markQualityComboBox.SelectedIndex;
                configValues[8] = markCloningComboBox.SelectedIndex;
                configValues[9] = markModuleTypeComboBox.SelectedIndex;//0;//markModuleHeightComboBox.Items.Count - markModuleHeightComboBox.SelectedIndex - 1;
                configValues[10] = (markParamComboBox.SelectedItem != null) ? markParamComboBox.SelectedIndex : 0;
                    //0;//Convert.ToByte(markFromPlaybillcheckBox.Checked);
                configValues[11] = (markUserComboBox.SelectedItem != null) ? markUserComboBox.SelectedIndex : 0;
                    // 0;//markModuleTypeComboBox.SelectedIndex;//markModuleWidthComboBox.SelectedIndex;


                CommunicationResult result = Communication.SetMarkConfig(false, configValues, reservedValues);

                if (result != CommunicationResult.Success)
                    MessageBox.Show(result.ToString());

            }
            catch
            {
                MessageBox.Show("There was a problem. Try again");
            }
        }

        private void markAutoscanButton_Click(object sender, EventArgs e)
        {
            maxCount = 10;
            
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


                readScanStatusTextBox.Text = "No info";
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
                CommunicationResult result = CommunicationResult.Failed;
                    ////Communication.SetMarkAutoscan(
                    ////testWithoutControllerCheckBox.Checked,
                    ////(byte)markColorComboBox.SelectedIndex, (byte)markModuleTypeComboBox.SelectedIndex, markFromPlaybillcheckBox.Checked, 1,
                    ////out ready, out autoValues, out deviceInfo);

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
                    autoHubTextBox.BackColor = currentBackColor;
                    autoDevicesListBox.BackColor = currentBackColor;
                    autoValuesListBox.BackColor = currentBackColor;

                    //autoColorTextBox.Text = GetStringValue(autoColorTextBox, autoValues[0]);
                    //autoHeightTextBox.Text = GetStringValue(autoHeightTextBox, autoValues[1]);
                    //autoWidthTextBox.Text = GetStringValue(autoWidthTextBox, autoValues[2]);
                    //autoScanModeTextBox.Text =  GetStringValue(autoScanModeTextBox, autoValues[3]);
                    //autoHubTextBox.Text = GetStringValue(autoHubTextBox, autoValues[4]);

                }

                autoHeightTextBox.Text = GetStringValue(autoHeightTextBox, autoValues[0]);
                autoWidthTextBox.Text = GetStringValue(autoWidthTextBox, autoValues[1]);
                autoScanModeTextBox.Text = GetStringValue(autoScanModeTextBox, autoValues[2]);

                autoHubTextBox.Text = GetStringValue(autoHubTextBox, autoValues[3]);
                controllerHubTextBox.Text = GetStringValue(controllerHubTextBox, autoValues[3]);
                screenHubTextBox.Text = GetStringValue(screenHubTextBox, autoValues[3]);

                SetupDevicesLists(deviceInfo);

                markFromPlaybillcheckBox.Checked = false;
                nextReadTimeInfoLabel.Text = "";


                if (markReadAutoAfterAutoscanCheckBox.Checked && !ready)
                {
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
            
            //readScanStatusTextBox.Text = "No info";
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

                autoHubTextBox.BackColor = currentBackColor;
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

                if (autoHubTextBox.BackColor != Color.Red && autoHubTextBox.BackColor != Color.Green)
                    autoHubTextBox.BackColor = currentBackColor;

                if(controllerHubTextBox.BackColor != Color.Red && controllerHubTextBox.BackColor != Color.Green)
                    controllerHubTextBox.BackColor = currentBackColor;
                if (screenHubTextBox.BackColor != Color.Red && screenHubTextBox.BackColor != Color.Green)
                    screenHubTextBox.BackColor = currentBackColor;

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
            string retval = "";

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
                switch(numberValue)
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
                        retval = "HUB12";
                        c.BackColor = Color.Red;
                        break;
                    case 4:
                        retval = "HUB8";
                        c.BackColor = Color.Red;
                        break;
                    case 5:
                        retval = "HUB8/12";
                        c.BackColor = Color.Red;
                        break;
                    case 6:
                        retval = "HUB8/12";
                        c.BackColor = Color.Red;
                        break;
                    case 7:
                        retval = "OFF";
                        c.BackColor = Color.Yellow;
                        break;
                    case 255:
                        retval = "NO DATA";
                        break;
                    default:
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
                        retval = "HUB8";
                        c.BackColor = Color.Red;
                        break;
                    case 4:
                        retval = "HUB12";
                        c.BackColor = Color.Red;
                        break;
                    case 5:
                        retval = "HUB8";
                        c.BackColor = Color.Red;
                        break;
                    case 6:
                        retval = "HUB12";
                        c.BackColor = Color.Red;
                        break;
                    case 7:
                        retval = "OFF";
                        c.BackColor = Color.Yellow;
                        break;
                    case 255:
                        retval = "NO DATA";
                        break;
                    default:
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

            autoValuesListBox.Items.Add(string.Format("Voltage: \t\t{0}V", string.Format("{0:0.00}",values[2]/ 100.0)));
            
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
                autoDevicesListBox.Items.Add("Network card: \t Yes");
            else
                autoDevicesListBox.Items.Add("Network card: \t No");
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
                currentBackColor = Color.DarkOrange;

                markColorComboBox.BackColor = currentBackColor;
                markHeightComboBox.BackColor = currentBackColor;
                markWidthComboBox.BackColor = currentBackColor;
                markAutoscanButton.BackColor = currentBackColor;

                fromScanCheckBox.Checked = false;
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

        private void button8_Click_1(object sender, EventArgs e)
        {
            //ipAddressBox1.IPAddress = System.Net.IPAddress.Broadcast;

            System.Net.IPAddress[] tmpTab = new System.Net.IPAddress[4];
            int tmpport = 0;

            CommunicationResult result = Communication.GetIPConfiguration(out tmpTab, out tmpport);

            if (result == CommunicationResult.Success)
            {
                ipAddressBox1.IPAddress = tmpTab[0];
                ipAddressBox2.IPAddress = tmpTab[1];
                ipAddressBox3.IPAddress = tmpTab[2];
                ipAddressBox4.IPAddress = tmpTab[3];

                textBox1.Text = tmpport.ToString();
            }
            else
                MessageBox.Show(result.ToString());
        }

        private void setIDAndBaudrateButton_Click(object sender, EventArgs e)
        {
            CommunicationResult result = Communication.SetIdAndBaudrate(int.Parse(controllerIDTextBox.Text), baudrateComboBox.SelectedIndex);

            if (result != CommunicationResult.Success)
            {
                if (Communication.Type == CommunicationType.Serial)
                {
                    SerialCommunication.BaudrateToChange = baudrateComboBox.SelectedIndex;
                    SerialCommunication.CurrentSerialPort.Baudrate = SerialCommunication.BaudrateToChange;
                    SerialCommunication.ControllerSerialPort.BaudRate = SerialCommunication.CurrentSerialPort.Baudrate;
                    Communication.CardID = 255;

                    int tmpbaudrateIndex = 0;
                    result = Communication.GetIdAndBaudrate(out tmpbaudrateIndex);

                    if (result != CommunicationResult.Success)
                        MessageBox.Show(result.ToString());
                    else
                        baudrateComboBox.SelectedItem = baudrateComboBox.Items[tmpbaudrateIndex];
                }
            }
        }

        private void button9_Click_1(object sender, EventArgs e)
        {
            System.Net.IPAddress[] tmpTab = new System.Net.IPAddress[4]
                {ipAddressBox1.IPAddress, ipAddressBox2.IPAddress, ipAddressBox3.IPAddress, ipAddressBox4.IPAddress};
            int tmpport = int.Parse(textBox1.Text);

            CommunicationResult result = Communication.SetIPConfiguration(tmpTab, tmpport);

            if (result != CommunicationResult.Success)
                MessageBox.Show(result.ToString());
        }

        private void ProgramSettingsForm_Deactivate(object sender, EventArgs e)
        {
            //if (readAutoTimer != null)
            //{
            //    readAutoTimer.Stop();
            //    //readAutoTimer.Dispose();
            //    //readAutoTimer = null;

            //    nextReadTimeInfoLabel.Text = "";
            //}
        }

        private void ProgramSettingsForm_Activated(object sender, EventArgs e)
        {
            //if (performReadScanPeriodicalyCheckBox.Checked)
            {
                //if (readAutoTimer != null)
                //{
                //    readAutoTimer.Stop();
                //    readAutoTimer.Dispose();
                //    readAutoTimer = null;
                //}
                //periodTimerCounter = 0;
                //readAutoTimer = new System.Windows.Forms.Timer();
                //readAutoTimer.Tick += readAutoTimerWithPeriod_Tick;
                //readAutoTimer.Interval = 1000;// (int)(readScanPeriodNumericUpDown.Value * 1000);


                //if(readAutoTimer != null)
                //   readAutoTimer.Start();
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint iep = new IPEndPoint(IPAddress.Any, 5200);
            sock.Bind(iep);
            EndPoint ep = (EndPoint)iep;
            Console.WriteLine("Ready to receive...");

            byte[] data = new byte[1024];
            int recv = sock.ReceiveFrom(data, ref ep);
            string stringData = Encoding.ASCII.GetString(data, 0, recv);
            Console.WriteLine("received: {0}  from: {1}", stringData, ep.ToString());

            sock.Close();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            IPEndPoint iep = new IPEndPoint(IPAddress.Broadcast, 9999);

            string hostname = Dns.GetHostName();
            byte[] data = Encoding.ASCII.GetBytes(hostname);
            while (true)
            {
                sock.SendTo(data, iep);
                Thread.Sleep(600);
            }
        }

        private void button10_Click_1(object sender, EventArgs e)
        {
            //System.Collections.BitArray redBits = new System.Collections.BitArray(new bool[] { false, false, false, false, 
            //                                                                                    false, false, false, true});
            //byte[] tmpColorBytes;
            //redBits = Reverse(redBits);
            //// red
            //tmpColorBytes = new byte[redBits.Length / 8];
            //redBits.CopyTo(tmpColorBytes, 0);

            //MessageBox.Show(tmpColorBytes[0].ToString());


            string path = textBox2.Text;

            Bitmap bmp = (Bitmap)Bitmap.FromFile(path);

            byte[] tmpb = GenerateBitmapBytes(bmp);

            string tmps = "";

            foreach (byte b in tmpb)
            {
                tmps += b.ToString("X2") + " ";
            }

            MessageBox.Show(tmps);

        }

        public BitArray Reverse(BitArray array)
        {
            int length = array.Length;
            int mid = (length / 2);

            for (int i = 0; i < mid; i++)
            {
                bool bit = array[i];
                array[i] = array[length - i - 1];
                array[length - i - 1] = bit;
            }

            return array;
        }

        public byte[] GenerateBitmapBytes(Bitmap inputBmp)
        {
            List<byte> retval = new List<byte>(0);

            BitArray redBits = new BitArray(0);
            BitArray greenBits = new BitArray(0);
            BitArray blueBits = new BitArray(0);

            System.Drawing.Color pixelColor = System.Drawing.Color.Black;
            int currentBitIndex = 0;

            switch (((MainWindow)Owner).CurrentPlaybill.Color)
            {
                case 1: // R
                    {
                        #region Red
                        redBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        lock (inputBmp)
                        {
                            for (int y = 0; y < inputBmp.Height; y++)
                            {
                                for (int x = 0; x < inputBmp.Width; x++)
                                {
                                    pixelColor = inputBmp.GetPixel(x, y);
                                    currentBitIndex = (inputBmp.Width - x - 1) + ((inputBmp.Height - y - 1) * inputBmp.Width);

                                    if (pixelColor.R > 127)
                                        redBits[currentBitIndex] = true;
                                }
                            }
                        }
                        break;
                        #endregion
                    }
                case 2: // G
                    {
                        #region Green
                        redBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        greenBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        lock (inputBmp)
                        {
                            for (int y = 0; y < inputBmp.Height; y++)
                            {
                                for (int x = 0; x < inputBmp.Width; x++)
                                {
                                    pixelColor = inputBmp.GetPixel(x, y);
                                    currentBitIndex = (inputBmp.Width - x - 1) + ((inputBmp.Height - y - 1) * inputBmp.Width);

                                    if (pixelColor.G > 127)
                                        greenBits[currentBitIndex] = true;
                                }
                            }
                        }
                        break;
                        #endregion
                    }
                case 3: // RG
                    {
                        #region RG
                        redBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        greenBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        lock (inputBmp)
                        {
                            for (int y = 0; y < inputBmp.Height; y++)
                            {
                                for (int x = 0; x < inputBmp.Width; x++)
                                {
                                    pixelColor = inputBmp.GetPixel(x, y);
                                    currentBitIndex = (inputBmp.Width - x - 1) + ((inputBmp.Height - y - 1) * inputBmp.Width);

                                    if (pixelColor.R > 127)
                                        redBits[currentBitIndex] = true;
                                    if (pixelColor.G > 127)
                                        greenBits[currentBitIndex] = true;
                                }
                            }
                        }
                        break;
                        #endregion
                    }
                case 4: // B
                    {
                        #region Blue
                        redBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        greenBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        blueBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        lock (inputBmp)
                        {
                            for (int y = 0; y < inputBmp.Height; y++)
                            {
                                for (int x = 0; x < inputBmp.Width; x++)
                                {
                                    pixelColor = inputBmp.GetPixel(x, y);
                                    currentBitIndex = (inputBmp.Width - x - 1) + ((inputBmp.Height - y - 1) * inputBmp.Width);

                                    if (pixelColor.B > 127)
                                        blueBits[currentBitIndex] = true;
                                }
                            }
                        }
                        break;
                        #endregion
                    }
                case 5: // RB
                    {
                        #region RB
                        redBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        greenBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        blueBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        lock (inputBmp)
                        {
                            for (int y = 0; y < inputBmp.Height; y++)
                            {
                                for (int x = 0; x < inputBmp.Width; x++)
                                {
                                    pixelColor = inputBmp.GetPixel(x, y);
                                    currentBitIndex = (inputBmp.Width - x - 1) + ((inputBmp.Height - y - 1) * inputBmp.Width);

                                    if (pixelColor.R > 127)
                                        redBits[currentBitIndex] = true;
                                    if (pixelColor.B > 127)
                                        blueBits[currentBitIndex] = true;
                                }
                            }
                        }
                        break;
                        #endregion
                    }
                case 6: // GB
                    {
                        #region GB
                        redBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        greenBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        blueBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        lock (inputBmp)
                        {
                            for (int y = 0; y < inputBmp.Height; y++)
                            {
                                for (int x = 0; x < inputBmp.Width; x++)
                                {
                                    pixelColor = inputBmp.GetPixel(x, y);
                                    currentBitIndex = (inputBmp.Width - x - 1) + ((inputBmp.Height - y - 1) * inputBmp.Width);

                                    if (pixelColor.R > 127)
                                        redBits[currentBitIndex] = true;
                                    if (pixelColor.G > 127)
                                        greenBits[currentBitIndex] = true;
                                }
                            }
                        }
                        break;
                        #endregion
                    }
                case 7: // RGB
                    {
                        #region RGB
                        redBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        greenBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        blueBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        lock (inputBmp)
                        {
                            for (int y = 0; y < inputBmp.Height; y++)
                            {
                                for (int x = 0; x < inputBmp.Width; x++)
                                {
                                    pixelColor = inputBmp.GetPixel(x, y);
                                    currentBitIndex = (inputBmp.Width - x - 1) + ((inputBmp.Height - y - 1) * inputBmp.Width);

                                    if (pixelColor.R > 127)
                                        redBits[currentBitIndex] = true;
                                    if (pixelColor.G > 127)
                                        greenBits[currentBitIndex] = true;
                                    if (pixelColor.B > 127)
                                        blueBits[currentBitIndex] = true;
                                }
                            }
                        }
                        break;
                        #endregion
                    }
            }

            byte[] tmpColorBytes;

            // red
            tmpColorBytes = new byte[redBits.Length / 8];
            redBits.CopyTo(tmpColorBytes, 0);
            Array.Reverse(tmpColorBytes);
            retval.AddRange(tmpColorBytes);
            // green
            tmpColorBytes = new byte[greenBits.Length / 8];
            greenBits.CopyTo(tmpColorBytes, 0);
            Array.Reverse(tmpColorBytes);
            retval.AddRange(tmpColorBytes);
            // blue
            tmpColorBytes = new byte[blueBits.Length / 8];
            blueBits.CopyTo(tmpColorBytes, 0);
            Array.Reverse(tmpColorBytes);
            retval.AddRange(tmpColorBytes);

            return retval.ToArray();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void fromScanCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Color currentBackColor;

            if (((CheckBox)sender).Checked)
            {
                currentBackColor = Color.DarkOrange;

                markColorComboBox.BackColor = currentBackColor;
                markHeightComboBox.BackColor = currentBackColor;
                markWidthComboBox.BackColor = currentBackColor;
                markAutoscanButton.BackColor = currentBackColor;

                fromScanCheckBox.Checked = false;
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


    }
}
