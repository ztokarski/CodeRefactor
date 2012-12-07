using System.Drawing;

using System.Windows.Forms;

namespace BengiLED_for_C_Power
{
    public partial class LogWindow : Form
    {
        private string message;

        public string Message
        {
            get { return message; }
            set {
                message = value;
                logListBox.Items.Add(message);
                logListBox.SelectedIndex = logListBox.Items.Count - 1;
                logListBox.ClearSelected();
            }
        }

        public LogWindow()
        {
            InitializeComponent();
            this.Location = new Point(1200, 100);
        }

        public void Clearlog()
        {
            logListBox.Items.Clear();
        }

        private void logListBox_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            logListBox.Refresh();
            Application.DoEvents();
        }

        private void logListBox_Enter(object sender, System.EventArgs e)
        {
            logListBox.Refresh();
            Application.DoEvents();
        }

        private void LogWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
                e.Cancel = true;

        }
    }
}
