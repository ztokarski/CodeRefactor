using System;

using System.Windows.Forms;

namespace BengiLED_for_C_Power
{
    public partial class AlertWindow : Form
    {
        public volatile bool ShouldCloseNow;

        public AlertWindow()
        {
            InitializeComponent();
        }

        private void AlertWindow_Load(object sender, EventArgs e)
        {
            ShouldCloseNow = false;

            tmrCheckIfNeedToCloseDialog.Start();
        }

        private void trmCheckIfNeedToCloseDialog_Tick(object sender, EventArgs e)
        {
            if (ShouldCloseNow)
                Close();
        }

        private void AlertWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!ShouldCloseNow)
                ShouldCloseNow = true;
        }
    }
}
