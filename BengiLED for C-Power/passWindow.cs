using System;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace BengiLED_for_C_Power
{

    public partial class passWindow : Form
    {
        private string password;// = "1321";
        private bool remember;

        public passWindow(string pass)
        {
            InitializeComponent();

            password = pass;
            remember = false;
        }

        private void passBox_TextChanged(object sender, EventArgs e)
        {
            if (this.Text != "")
                okButton.Enabled = true;
            else
                okButton.Enabled = false;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            bool allowed = false;
           
            using (MD5 md5Hash = MD5.Create())
            {
                //string hash = "8e036cc193d0af59aa9b22821248292b";

                if (MainWindow.VerifyMd5Hash(md5Hash, passBox.Text, password))
                    allowed = true;
            }
            
            //if (passBox.Text == password)
            if(allowed)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                Form mainForm = this.Owner;

                while(!(mainForm is MainWindow))
                    mainForm = mainForm.Owner;

                MessageBox.Show(((MainWindow)mainForm).messageBoxesHashTable["messageBoxMessage_passwordAccessDenied"].ToString(), 
                    ((MainWindow)mainForm).messageBoxesHashTable["messageBoxTitle_passwordAccessDenied"].ToString(), 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void rememberPaswdCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            remember = rememberPaswdCheckBox.Checked;
        }

        private void passWindow_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible == false && remember == false)
            {
                passBox.Text = null;
                okButton.Enabled = false;
            }
        }

        private void passBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return && passBox.Text != "")
            {
                EventArgs args = new EventArgs();
                okButton_Click(this, args);
            }
        }
        
    }
}
