using System;
using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace BengiLED_for_C_Power
{
    public partial class NetworkSearchWindow : Form
    {
        private bool found = false;
        private System.ComponentModel.BackgroundWorker bgWorker;
        string statusText = "Searching at: ";
        private ToolTip tool = new ToolTip();
        TabPage connect;
        TabPage search;
        TabPage menu;

        public NetworkSearchWindow()
        {
            InitializeComponent();
            SetTooltips();

            menu = tabControl1.TabPages[0];
            connect = tabControl1.TabPages[1];
            search = tabControl1.TabPages[2];

            tabControl1.TabPages.RemoveAt(2);
            tabControl1.TabPages.RemoveAt(1);

            this.bgWorker = new BackgroundWorker();
            bgWorker.WorkerReportsProgress = true;

            bgWorker.WorkerSupportsCancellation = true; // added line

            InitializeBgWorker();
           
        }

        public void SetTooltips()
        {
            tool.AutomaticDelay = 1000;
            tool.IsBalloon = true;
            tool.ToolTipIcon = ToolTipIcon.Info;
            tool.ToolTipTitle = "Information"; //"BengiLED context-sensitive help";

            tool.SetToolTip(IpAdressTextBox, "Ip address to use for creating connection with screen");
            //          tool.SetToolTip(netSearchComboBox, "The available networks in which program can search for the screen.");
        }

        private void InitializeBgWorker()
        {
            bgWorker.DoWork += new DoWorkEventHandler(bgWorker_DoWork);
            bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
            bgWorker.ProgressChanged += new ProgressChangedEventHandler(bgWorker_ProgressChanged);
        }

        void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            bool breakWhenFound = true;
            IPAddress ip = IPAddress.Parse(((object[])(e.Argument))[0].ToString());
            int minAddressVal = Int32.Parse(((object[])(e.Argument))[1].ToString());
            int maxAddressVal = Int32.Parse(((object[])(e.Argument))[2].ToString());



            NetworkCommunication.SocketsList.Clear();

            byte[] tmpNetIp = ip.GetAddressBytes();
            UInt32 net = (UInt32)tmpNetIp[0] << 24;
            net += (UInt32)tmpNetIp[1] << 16;
            net += (UInt32)tmpNetIp[2] << 8;

            string NetAddress;

            if (minAddressVal < 1)
                minAddressVal = 1;
            if (maxAddressVal > 254)
                maxAddressVal = 254;

            if (minAddressVal > maxAddressVal)
            {
                int t = minAddressVal;
                minAddressVal = maxAddressVal;
                maxAddressVal = t;
            }
            else if (minAddressVal == maxAddressVal)
            {
                if (minAddressVal > 2)
                    --minAddressVal;
                else if (maxAddressVal < 253)
                    maxAddressVal++;
            }

            System.Net.IPAddress testedIP = System.Net.IPAddress.Parse("0.0.0.0");
            AvailableSocket tmp;

            for (int i = minAddressVal; i <= maxAddressVal; ++i)
            {

                // added code
                if (bgWorker.CancellationPending == true)
                {
                    e.Cancel = true;
                    break;
                }
                // end of added code

                NetAddress = (net + i).ToString();

                int percentComplete = (int)((float)(i - minAddressVal) / (float)(maxAddressVal - minAddressVal) * 100);
                bgWorker.ReportProgress(percentComplete, NetAddress);

                tmp = NetworkCommunication.TestGivenSocket(NetAddress);

                if (!(tmp.Ip.Equals(testedIP)))
                {
                    NetworkCommunication.SocketsList.Add(tmp);
                    if (breakWhenFound == true)
                        break;
                }

            }
        }

        void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //statusLabel.Text = string.Format("{0}{1}",statusText,(e.ProgressPercentage.ToString() + "%"));
            statusLabel.Text = string.Format("{0}{1}", statusText, System.Net.IPAddress.Parse(e.UserState.ToString()).ToString());
        }

        void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {
                statusLabel.Text = "Canceled!";
            }
            else
                statusLabel.Text = "Done!";
        }


        // Added code
        private void cancelAsyncButton_Click(object sender, EventArgs e)
        {
            if (bgWorker.WorkerSupportsCancellation == true)
            {
                // Cancel the asynchronous operation.
                if (bgWorker.IsBusy == true)
                    bgWorker.CancelAsync();
                if (searchNetListBox.CheckedItems.Count > 0 || bgWorker.IsBusy == true)
                    System.Threading.Thread.Sleep(3500);
                this.searchButton.Text = "Search";
                this.searchButton.Click -= new System.EventHandler(cancelAsyncButton_Click);
                this.searchButton.Click += new EventHandler(searchButton_Click);

                this.searchNetListBox.Enabled = true;
                this.maxTextBox.Enabled = true;
                this.minTextBox.Enabled = true;

                ((MainWindow)(this.Owner)).logWindow.Message = ((MainWindow)(this.Owner)).logMessagesHashTable["logMessage_searchingByIpCanceled"].ToString();
            }
        }
        // End of added code

        private void connectButton_Click(object sender, EventArgs e)
        {

            if (IpAdressTextBox.Text != null && IpAdressTextBox.Text != "")
            {

                ((MainWindow)(this.Owner)).logWindow.Message = ((MainWindow)(this.Owner)).logMessagesHashTable["logMessage_searchingByIpAttemptingToConnect"].ToString();
                this.Cursor = Cursors.WaitCursor;

                Communication.CardID = 255;
                System.Net.IPAddress testedIP = System.Net.IPAddress.Parse("0.0.0.0");
                AvailableSocket tmp = NetworkCommunication.TestGivenSocket(IpAdressTextBox.Text);
                if (testedIP.Equals(tmp.Ip))
                {
                    ((MainWindow)(this.Owner)).logWindow.Message = ((MainWindow)(this.Owner)).logMessagesHashTable["logMessage_searchingByIpUnableToConnect"].ToString();
                    //MessageBox.Show("The screen was not found via network connection.\nProbably given IP was misstyped.", "BengiLED - Connection Failure", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    found = false;
                }
                else
                {
                    NetworkCommunication.SocketsList.Add(tmp);
                    ((MainWindow)(this.Owner)).logWindow.Message = string.Format("{0} {1}", ((MainWindow)(this.Owner)).logMessagesHashTable["logMessage_searchingByIpFoundIpIs"].ToString(), NetworkCommunication.SocketsList[0].Ip);
                    //MessageBox.Show(string.Format("The screen was found via network connection. IP is {0}", NetworkCommunication.SocketsList[0].Ip), "BengiLED - Connection Succes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    found = true;
                    this.Close();
                }

                this.Cursor = Cursors.Default;
            }
        }

        private void searchButton_Click(object sender, EventArgs e)
        {
            try
            {
                // added code
                this.searchButton.Text = "Cancel searching";
                this.searchButton.Click -= new System.EventHandler(this.searchButton_Click);
                this.searchButton.Click += new EventHandler(cancelAsyncButton_Click);
                // end of added code

                this.searchNetListBox.Enabled = false;
                this.maxTextBox.Enabled = false;
                this.minTextBox.Enabled = false;

                statusLabel.Text = string.Empty;

              bgWorker.RunWorkerAsync((object)new[] { (object)searchNetListBox.CheckedItems[0].ToString(), (object)minTextBox.Text, (object)maxTextBox.Text });
              ((MainWindow)(this.Owner)).logWindow.Message = ((MainWindow)(this.Owner)).logMessagesHashTable["logMessage_searchingByIpInSelectedNetwork"].ToString();
            }
            catch (Exception)
            {

                if (searchNetListBox.CheckedItems.Count == 0)
                    statusLabel.Text = ((MainWindow)(this.Owner)).logMessagesHashTable["logMessage_selectNetwork"].ToString();

                cancelAsyncButton_Click(sender, e);
            }
        }

        private void NetworkSearchWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (found == true)
                this.DialogResult = DialogResult.OK;
            else
                this.DialogResult = DialogResult.No;
        }

        private void NetworkSearchWindow_Load(object sender, EventArgs e)
        {
            statusLabel.Text = string.Empty;
            searchNetListBox.Items.Clear();

            NetworkInterface[] Interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface Interface in Interfaces)
            {
                if (Interface.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                Console.WriteLine(Interface.Description);
                UnicastIPAddressInformationCollection UnicastIPInfoCol = Interface.GetIPProperties().UnicastAddresses;
                foreach (UnicastIPAddressInformation UnicastIPInfo in UnicastIPInfoCol)
                {
                    if (UnicastIPInfo.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        try
                        {
                            byte[] adress = UnicastIPInfo.Address.GetAddressBytes();
                            byte[] mask = UnicastIPInfo.IPv4Mask.GetAddressBytes();

                            UInt32 adr = (UInt32)adress[0] << 24;
                            adr += (UInt32)adress[1] << 16;
                            adr += (UInt32)adress[2] << 8;
                            adr += (UInt32)adress[3];

                            UInt32 ma = (UInt32)mask[0] << 24;
                            ma += (UInt32)mask[1] << 16;
                            ma += (UInt32)mask[2] << 8;
                            ma += (UInt32)mask[3];

                            searchNetListBox.Items.Add(IPAddress.Parse((adr & ma).ToString()));
                        }
                        catch (NullReferenceException)
                        {
                            continue;
                        }
                    }
                }
            }

            if (searchNetListBox.Items.Count > 0)
            {
                searchNetListBox.SetItemChecked(0, true);
                searchNetListBox.SelectedIndex = 0;
            }

            if (searchNetListBox.Items.Count == 0)
                netSearchLabel.Text = "Computer is not connected to any network. Please check your connection";
            else if (searchNetListBox.Items.Count > 1)
                netSearchLabel.Text = "Computer is connected to more than 1 network,\r\nplease select a network to search screen:";
            else
                netSearchLabel.Text = "Computer is connected to only one network.\r\nThere is no need to select network";

            string ipFromConfig = ((MainWindow)this.Owner).ConfigHashtable["CurrentIP"].ToString();

            IpAdressTextBox.Text = ((MainWindow)this.Owner).ConfigHashtable["CurrentIP"].ToString();

            ((MainWindow)(this.Owner)).logWindow.Message = ((MainWindow)(this.Owner)).logMessagesHashTable["logMessage_networkSearchWindowInited"].ToString();
        }

        private void notConnectButton_Click(object sender, EventArgs e)
        {
            found = false;
            this.Close();
        }

        private void numbersOnlyTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
                e.Handled = true;
        }

        private void statusLabel_TextChanged(object sender, EventArgs e)
        {
            if (statusLabel.Text == "Done!")
            {
                if (NetworkCommunication.SocketsList.Count > 0)
                {
                    ((MainWindow)(this.Owner)).logWindow.Message = string.Format("{0} {1}", ((MainWindow)(this.Owner)).logMessagesHashTable["logMessage_searchingByIpFoundIpIs"].ToString(), NetworkCommunication.SocketsList[0].Ip);
                    //MessageBox.Show(string.Format("The screen was found via network connection. IP is {0}", NetworkCommunication.SocketsList[0].Ip), "BengiLED  - Connection Succes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    found = true;
                    this.Close();
                }
                else
                {
                    ((MainWindow)(this.Owner)).logWindow.Message = ((MainWindow)(this.Owner)).logMessagesHashTable["logMessage_searchingByIpNotFound"].ToString();
                    //MessageBox.Show("The screen was not found via network connection.\nProbably screen isn't in given net.", "BengiLED - Connection Failure", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    found = false;

                    cancelAsyncButton_Click(sender, e);
                }

            }
        }

        private void netSearchComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

            for (int i = 0; i < searchNetListBox.Items.Count; ++i)
                if (searchNetListBox.GetItemChecked(i) == true)
                    searchNetListBox.SetItemChecked(i, false);

            searchNetListBox.SetItemChecked(searchNetListBox.SelectedIndex, true);

            //      string[] split = netSearchComboBox.SelectedItem.ToString().Split('.');
            if (searchNetListBox.CheckedItems.Count > 0)
            {
                string[] split = searchNetListBox.CheckedItems[0].ToString().Split('.');

                minRangeLabel.Text = String.Format("{0}.{1}.{2}.", split[0], split[1], split[2]);
                maxRangelabel.Text = string.Format("{0}.{1}.{2}.", split[0], split[1], split[2]);
            }
        }

        private void selectConnectButton_Click(object sender, EventArgs e)
        {
            tabControl1.TabPages.RemoveAt(0);
            tabControl1.TabPages.Insert(0, connect);
        }

        private void selectSearchButton_Click(object sender, EventArgs e)
        {
            tabControl1.TabPages.RemoveAt(0);
            tabControl1.TabPages.Insert(0, search);
        }

        private void BackButton_Click(object sender, EventArgs e)
        {
            if (tabControl1.TabPages[0].Name == "searchTab" && bgWorker.IsBusy == true)
                cancelAsyncButton_Click(sender, e);

            tabControl1.TabPages.RemoveAt(0);
            tabControl1.TabPages.Insert(0, menu);
        }

        public bool TestDefaultIP()
        {
            string defaultIP = ((MainWindow)this.Owner).ConfigHashtable["CurrentIP"].ToString();

            Communication.Type = CommunicationType.Network;

            System.Net.IPAddress testedIP = System.Net.IPAddress.Parse("0.0.0.0");
            AvailableSocket tmp = NetworkCommunication.TestGivenSocket(defaultIP);
            if (testedIP.Equals(tmp.Ip))
            {
                ((MainWindow)(this.Owner)).logWindow.Message = ((MainWindow)(this.Owner)).logMessagesHashTable["logMessage_searchingByIpDefaultNotFound"].ToString();
                //MessageBox.Show("The screen was not found via network default IP address.", "BengiLED - Connection Failure", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Communication.Type = CommunicationType.None;
                
                return false;
            }
            else
            {
                NetworkCommunication.SocketsList.Add(tmp);
                ((MainWindow)(this.Owner)).logWindow.Message = string.Format("{0} {1}", ((MainWindow)(this.Owner)).logMessagesHashTable["logMessage_searchingByIpFoundIpIsDefault"].ToString(), NetworkCommunication.SocketsList[0].Ip);
                //MessageBox.Show(string.Format("The screen was found via network connection. IP is default {0}", NetworkCommunication.SocketsList[0].Ip), "BengiLED - Connection Succes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
        }

        private void TextBox_Leave(object sender, EventArgs e)
        {
            if (Int16.Parse(((TextBox)(sender)).Text) == 0)
                ((TextBox)(sender)).Text = "1";
            else if (Int16.Parse(((TextBox)(sender)).Text) > 254)
                ((TextBox)(sender)).Text = "254";
        }
    }
}
