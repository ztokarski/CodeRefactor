using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace BengiLED_for_C_Power
{
    public partial class BrightnessSettingsForm : Form
    {
        private bool allBrightnessAutoChanging;
        private bool allBrightnessValueChanging;
        private bool lmbDown;
        private bool getBrightnessOnReactive = false;


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
        
        public BrightnessSettingsForm()
        {
            InitializeComponent();
        }


        #region Brightness

        public void GetBrightness()
        {
            this.Cursor = Cursors.WaitCursor;

            if(Communication.Type == CommunicationType.None)
                ((MainWindow)Owner).SearchForScreen();//false);
            if (Communication.Type != CommunicationType.None)
            {
                //byte[] briVals = SerialCommunication.ReadBrightness();

                byte[] briVals = new byte[1];
                CommunicationResult result = Communication.GetBrightness(out briVals);

                AllBrightnessAutoChanging = true;
                AllBrightnessValueChanging = true;

                brightness04TrackBar.Value = (briVals[0] < 32) ? briVals[0] : brightness04TrackBar.Value;
                brightness48TrackBar.Value = (briVals[1] < 32) ? briVals[1] : brightness48TrackBar.Value;
                brightness812TrackBar.Value = (briVals[2] < 32) ? briVals[2] : brightness812TrackBar.Value;
                brightness1216TrackBar.Value = (briVals[3] < 32) ? briVals[3] : brightness1216TrackBar.Value;
                brightness1622TrackBar.Value = (briVals[4] < 32) ? briVals[4] : brightness1622TrackBar.Value;
                brightness2224TrackBar.Value = (briVals[5] < 32) ? briVals[5] : brightness2224TrackBar.Value;

                if (briVals[0] >= 32)
                    brightness04SensorCheckBox.Checked = true;
                if (briVals[1] >= 32)
                    brightness48SensorCheckBox.Checked = true;
                if (briVals[2] >= 32)
                    brightness812SensorCheckBox.Checked = true;
                if (briVals[3] >= 32)
                    brightness1216SensorCheckBox.Checked = true;
                if (briVals[4] >= 32)
                    brightness1622SensorCheckBox.Checked = true;
                if (briVals[5] >= 32)
                    brightness2224SensorCheckBox.Checked = true;

                //brightness04TrackBar.Value = (briVals[0] < 32) ? briVals[0] : brightness04TrackBar.Value;
                //brightness48TrackBar.Value = (briVals[4] < 32) ? briVals[4] : brightness48TrackBar.Value;
                //brightness812TrackBar.Value = (briVals[8] < 32) ? briVals[8] : brightness812TrackBar.Value;
                //brightness1216TrackBar.Value = (briVals[12] < 32) ? briVals[12] : brightness1216TrackBar.Value;
                //brightness1622TrackBar.Value = (briVals[16] < 32) ? briVals[16] : brightness1622TrackBar.Value;
                //brightness2224TrackBar.Value = (briVals[22] < 32) ? briVals[22] : brightness2224TrackBar.Value;

                //if (briVals[0] >= 32)
                //    brightness04SensorCheckBox.Checked = true;
                //if (briVals[4] >= 32)
                //    brightness48SensorCheckBox.Checked = true;
                //if (briVals[8] >= 32)
                //    brightness812SensorCheckBox.Checked = true;
                //if (briVals[12] >= 32)
                //    brightness1216SensorCheckBox.Checked = true;
                //if (briVals[16] >= 32)
                //    brightness1622SensorCheckBox.Checked = true;
                //if (briVals[22] >= 32)
                //    brightness2224SensorCheckBox.Checked = true;

                if (brightness04SensorCheckBox.Checked && brightness48SensorCheckBox.Checked && brightness812SensorCheckBox.Checked
                        && brightness1216SensorCheckBox.Checked && brightness1622SensorCheckBox.Checked && brightness2224SensorCheckBox.Checked)
                {
                    useLightSensorAllCheckBox.Checked = true;
                }
                else
                    useLightSensorAllCheckBox.Checked = false;

                AllBrightnessAutoChanging = false;
                AllBrightnessValueChanging = false;
            }
            ((MainWindow)Owner).logWindow.Hide();
            this.Activate();

            this.Cursor = Cursors.Default;
        }

        public void SetBrightness()
        {
            this.Cursor = Cursors.WaitCursor;

            if(Communication.Type == CommunicationType.None)
                ((MainWindow)Owner).SearchForScreen();//false);
            if (Communication.Type != CommunicationType.None)
            {
                List<byte> tmplist = new List<byte>(24);

                if (useLightSensorAllCheckBox.Checked)
                {
                    //for (int i = 0; i < 24; i++)
                    for (int i = 0; i < 6; i++)
                        tmplist.Add(255);
                }
                else
                {
                    byte value = (byte)brightness04TrackBar.Value;
                    //for (int i = 0; i < 4; i++)
                    tmplist.Add((brightness04SensorCheckBox.Checked) ? (byte)255 : value);
                    value = (byte)brightness48TrackBar.Value;
                    //for (int i = 4; i < 8; i++)
                    tmplist.Add((brightness48SensorCheckBox.Checked) ? (byte)255 : value);
                    value = (byte)brightness812TrackBar.Value;
                    //for (int i = 8; i < 12; i++)
                    tmplist.Add((brightness812SensorCheckBox.Checked) ? (byte)255 : value);
                    value = (byte)brightness1216TrackBar.Value;
                    //for (int i = 12; i < 16; i++)
                    tmplist.Add((brightness1216SensorCheckBox.Checked) ? (byte)255 : value);
                    value = (byte)brightness1622TrackBar.Value;
                    //for (int i = 16; i < 20; i++)
                    tmplist.Add((brightness1622SensorCheckBox.Checked) ? (byte)255 : value);
                    value = (byte)brightness2224TrackBar.Value;
                    //for (int i = 20; i < 24; i++)
                    tmplist.Add((brightness2224SensorCheckBox.Checked) ? (byte)255 : value);
                }
                CommunicationResult result = Communication.SetBrightness(tmplist.ToArray());
                if (result != CommunicationResult.Success)
                    MessageBox.Show(result.ToString());
                //SerialCommunication.WriteBrightness(tmplist.ToArray());
            }
            //ret: ;
            ((MainWindow)Owner).logWindow.Hide();
            this.Activate();

            this.Cursor = Cursors.Default;
        }

        private void brightnessAllTrackBar_Scroll(object sender, EventArgs e)
        {
            if (!AllBrightnessValueChanging)
            {
                AllBrightnessValueChanging = true;
                foreach (Control c in ((TrackBar)sender).Parent.Parent.Controls)
                {
                    if (c is GroupBox)
                    {
                        foreach (Control item in c.Controls)
                        {
                            if (item is Label)
                                item.Text = ((TrackBar)sender).Value.ToString();
                            else if (item is TrackBar)
                                ((TrackBar)item).Value = ((TrackBar)sender).Value;
                        }
                    }

                }

                //SetBrightness();

                AllBrightnessValueChanging = false;
            }
        }

        private void brightnessTrackBar_ValueChanged(object sender, EventArgs e)
        {
            //if (!lmbDown)
            {
                string trackBarName = ((TrackBar)sender).Name;
                string labelName = trackBarName.Substring(0, trackBarName.IndexOf("TrackBar"));

                ((TrackBar)sender).Parent.Controls[string.Format("{0}ValueLabel", labelName)].Text
                    = ((TrackBar)sender).Value.ToString();

                //if (!AllBrightnessValueChanging)
                //    SetBrightness();
            }
        }

        private void brightnessSensorCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            string checkBoxName = ((CheckBox)sender).Name;

            string groupName = checkBoxName.Substring(0, checkBoxName.IndexOf("SensorCheckBox"));

            ((CheckBox)sender).Parent.Controls[string.Format("{0}TrackBar", groupName)].Enabled
                = !((CheckBox)sender).Checked;
            ((CheckBox)sender).Parent.Controls[string.Format("{0}ValueLabel", groupName)].Enabled
                = !((CheckBox)sender).Checked;


            if (!AllBrightnessAutoChanging)
            {
                AllBrightnessAutoChanging = true;
                if (useLightSensorAllCheckBox.Checked)
                    useLightSensorAllCheckBox.Checked = false;
                AllBrightnessAutoChanging = false;


                SetBrightness();
            }
        }

        private void brightnessAllTrackBar_MouseUp(object sender, MouseEventArgs e)
        {
            //if (AllBrightnessValueChanging)
            if (lmbDown && (e.Button & System.Windows.Forms.MouseButtons.Left) == System.Windows.Forms.MouseButtons.Left)
            {
                SetBrightness();
                lmbDown = false;
                //AllBrightnessValueChanging = false;
            }
        }

        private void brightnessAllTrackBar_MouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & System.Windows.Forms.MouseButtons.Left) == System.Windows.Forms.MouseButtons.Left)
            {
                lmbDown = true;
            }
        }

        private void useLightSensorAllCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            brightnessAllTrackBar.Enabled = !((CheckBox)sender).Checked;
            brightnessAllValueLabel.Enabled = !((CheckBox)sender).Checked;

            if (!AllBrightnessAutoChanging)
            {
                AllBrightnessAutoChanging = true;

                brightness04SensorCheckBox.Checked = ((CheckBox)sender).Checked;
                brightness48SensorCheckBox.Checked = ((CheckBox)sender).Checked;
                brightness812SensorCheckBox.Checked = ((CheckBox)sender).Checked;
                brightness1216SensorCheckBox.Checked = ((CheckBox)sender).Checked;
                brightness1622SensorCheckBox.Checked = ((CheckBox)sender).Checked;
                brightness2224SensorCheckBox.Checked = ((CheckBox)sender).Checked;

                AllBrightnessAutoChanging = false;

                SetBrightness();
            }
        }
        
        #endregion

        private void BrightnessSettingsForm_Shown(object sender, EventArgs e)
        {
            ((MainWindow)this.Owner).SearchForScreen();//false);

            if (Communication.Type != CommunicationType.None)
            {
                brightnessSettingsGroupBox.Enabled = true;
                GetBrightness();
            }
            else
                brightnessSettingsGroupBox.Enabled = false;

            ((MainWindow)this.Owner).logWindow.Hide();
        }

        private void BrightnessSettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            getBrightnessOnReactive = true;

            if (e.CloseReason == CloseReason.UserClosing)
                e.Cancel = true;
        }

        private void BrightnessSettingsForm_Activated(object sender, EventArgs e)
        {
            if (getBrightnessOnReactive)
            {
                getBrightnessOnReactive = false;

                ((MainWindow)this.Owner).SearchForScreen();//false);

                if (Communication.Type != CommunicationType.None)
                {
                    brightnessSettingsGroupBox.Enabled = true;
                    GetBrightness();
                }
                else
                    brightnessSettingsGroupBox.Enabled = false;

                ((MainWindow)this.Owner).logWindow.Hide();
            }
        }

    }
}
