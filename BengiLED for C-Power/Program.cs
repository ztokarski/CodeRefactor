using System;

using System.Windows.Forms;
using System.Threading;
using System.Security.AccessControl;
using System.Security.Principal;

namespace BengiLED_for_C_Power
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //try
            {
                using (Mutex mutex = new Mutex(false, @"Global\" + appGuid))
                {
                    var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
                    var securitySettings = new MutexSecurity();
                    securitySettings.AddAccessRule(allowEveryoneRule);
                    mutex.SetAccessControl(securitySettings);


                    if (!mutex.WaitOne(0, false))
                    {
                        MessageBox.Show("Program already running!");
                        return;
                    }

                    GC.Collect();
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new MainWindow());
                }
            }

            //catch (Exception e)
            //{
            //    string info = "Click OK or press Ctrl+C to copy that message to your clipboard. Please send that message to support team. \nClick Cancel if you don't want to copy it.";

            //    string msgBoxText = string.Format("{0} \n-------------------\n\n", info);
            //    string exceptionContent = string.Format(" Date: {0} \n\n Exception message: {1} \n\n Source: {2} \n\n Stack trace: \n {3}",
            //        DateTime.Now.ToShortDateString(), e.Message, e.Source, e.StackTrace);

            //    DialogResult result = MessageBox.Show(string.Format("{0} {1}", msgBoxText, exceptionContent), "LedConfig exception report",
            //        MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk);

            //    if (result == DialogResult.OK)
            //    {
            //        System.Windows.Forms.Clipboard.SetText(exceptionContent);
            //    }
            //}
        }

        private static string appGuid = "E24E2357-7898-485B-AE38-94E37B9D05C5";
    }

   
}
