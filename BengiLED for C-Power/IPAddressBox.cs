using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Net;

namespace BengiLED_for_C_Power
{
    public partial class IPAddressBox : UserControl
    {
        #region Private fields
        private IPAddress iPAddress;
        private bool addressSetting = false;
        #endregion

        #region Properties
        /// <summary>
        /// IP address that control operates on.
        /// </summary>
        public IPAddress IPAddress
        {
            get { return iPAddress; }
            set
            {
                addressSetting = true;

                if (value == null)
                    value = IPAddress.Any;

                iPAddress = value;
                byte[] ipBytes = iPAddress.GetAddressBytes();

                for (int i = 0; i < 4; i++)
                    ((TextBox)this.Controls[string.Format("textBox{0}", i + 1)]).Text = ipBytes[i].ToString();

                addressSetting = false;
            }
        }
        #endregion
        
        #region Methods

        public IPAddressBox()
        {
            InitializeComponent();
        }

        private void ipSegmentTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // get control name
            string controlName = ((TextBox)sender).Name;
            // get control number
            byte controlNumber = Convert.ToByte(controlName.Substring(controlName.LastIndexOf('x') + 1));
            // remove number from name
            controlName = controlName.Substring(0, controlName.Length - 1);


            if ((e.KeyValue >= (int)'0' && e.KeyValue <= (int)'9') && ((TextBox)sender).TextLength < 3)
            {
                // get atual text of ip segment
                string text = ((TextBox)sender).Text;
                // check if changed text would have value less or equal 255
                text = text.Remove(((TextBox)sender).SelectionStart, ((TextBox)sender).SelectionLength);
                text = text.Insert(((TextBox)sender).SelectionStart, ((char)e.KeyValue).ToString());
                int byteValue = int.Parse(text);

                if (byteValue > 255)
                {
                   
                    e.SuppressKeyPress = true;
                }
                else if (((TextBox)sender).Text == "0" && ((TextBox)sender).SelectionStart == 1) // remove zero from the beginning of string
                    ((TextBox)sender).Text = ((TextBox)sender).Text.Remove(0);
            }
            else
            {
                switch (e.KeyData)
                {
                    case Keys.Left:
                        if (controlNumber > 1)
                        {
                            if (((TextBox)sender).TextLength == 0 || (((TextBox)sender).SelectionStart) == 0)//(((TextBox)sender).TextLength - 1))
                            {
                                TextBox nextControl = (TextBox)this.Controls[string.Format("textBox{0}", --controlNumber)];
                                nextControl.Focus();
                                nextControl.SelectionLength = 0;
                                e.Handled = true;
                            }
                        }
                        break;
                    case Keys.Right:
                        if (controlNumber < 4)
                        {
                            //if (((TextBox)sender).TextLength == 0 || (((TextBox)sender).SelectionStart) == (0))
                            if (((TextBox)sender).TextLength == (((TextBox)sender).SelectionStart))
                            {
                                TextBox nextControl = (TextBox)this.Controls[string.Format("textBox{0}", ++controlNumber)];
                                nextControl.Focus();
                                nextControl.SelectionLength = 0;
                                e.Handled = true;
                            }
                        }
                        break;
                    case (Keys.RButton | Keys.MButton | Keys.Back | Keys.ShiftKey | Keys.Space | Keys.F17): // dot
                        if (controlNumber < 4)
                        {
                            TextBox nextControl = (TextBox)this.Controls[string.Format("textBox{0}", ++controlNumber)];
                            nextControl.Focus();
                            nextControl.SelectionLength = 0;
                        }
                        e.SuppressKeyPress = true;
                        e.Handled = true;
                        break;
                    case (Keys)110: // numpad dot
                        if (controlNumber < 4)
                        {
                            TextBox nextControl = (TextBox)this.Controls[string.Format("textBox{0}", ++controlNumber)];
                            nextControl.Focus();
                            nextControl.SelectionLength = 0;
                        }
                        e.SuppressKeyPress = true;
                        e.Handled = true;
                        break;
                    case Keys.Back:
                        break;
                    case Keys.Delete:
                        break;
                    default:
                        e.SuppressKeyPress = true;
                        break;
                }
            }
        }
        
        #endregion

        private void ipSegmentTextBox_Leave(object sender, EventArgs e)
        {
            //if(((TextBox)sender).TextLength == 0)
            //    ((TextBox)sender).Text = "0";
        }

        private void ipSegmentTextBox_TextChanged(object sender, EventArgs e)
        {
            if (((TextBox)sender).TextLength == 0)
            {
                ((TextBox)sender).Text = "0";
                ((TextBox)sender).SelectAll();
            }

            if (!addressSetting)
            {
                try
                {
                    IPAddress = System.Net.IPAddress.Parse(string.Format("{0}.{1}.{2}.{3}", 
                            textBox1.Text, textBox2.Text, textBox3.Text, textBox4.Text));
                }
                catch
                {
                    MessageBox.Show("Cannot parse address");
                }
            }
        }

        private void IPAddressBox_EnabledChanged(object sender, EventArgs e)
        {
            if (((IPAddressBox)sender).Enabled)
                ((IPAddressBox)sender).BackColor = Color.White;
            else
                ((IPAddressBox)sender).BackColor = System.Drawing.SystemColors.Control;
        }
    }
}
