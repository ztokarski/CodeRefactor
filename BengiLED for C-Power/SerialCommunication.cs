using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO.Ports;
using System.IO;

namespace BengiLED_for_C_Power
{
    public class AvailablePort
    {
        public string PortName;
        public bool Tested;
        public int Baudrate;



        public AvailablePort(string name, int baudrate)
        {
            PortName = name;
            Baudrate = baudrate;
            Tested = false;
        }
    }
    
    public class SerialCommunication
    {
        #region Private fields
        private static int cardID = 255;//1;
        private static int baudrate = 115200;
        private static SerialPort controllerSerialPort = null;
        private static AvailablePort testingPort = new AvailablePort("test", 0);
        private static int baudrateToChange = -1;
        private static int timeout = 1000;
        #endregion

        #region Properties
        public static int Timeout
        {
            set { timeout = value; }
            get { return timeout; }
        }

        public static int CardID
        {
            get { return cardID; }
            set { cardID = value; }
        }
        public static int Baudrate
        {
            get { return baudrate; }
            set { baudrate = value; }
        }

        public static int BaudrateToChange
        {
            get
            {
                switch (baudrateToChange)
                {
                    case 0:
                        return 115200;
                    //break;
                    case 1:
                        return 57600;
                    //break;
                    case 2:
                        return 38400;
                    //break;
                    case 3:
                        return 19200;
                    //break;
                    case 4:
                        return 9600;
                    //break;
                    case 5:
                        return 4800;
                    //break;
                    case 6:
                        return 2400;
                    default:
                        return CurrentSerialPort.Baudrate;
                }
            }
            set { baudrateToChange = value; }
        }


        public static List<AvailablePort> PortsList = new List<AvailablePort>();
        public static List<AvailablePort> DevicesLists = new List<AvailablePort>();
        public static AvailablePort CurrentSerialPort
        {
            get 
            {
                return (testingPort.PortName == "test") ? DevicesLists[0] : testingPort; 
            }
            set
            {
                testingPort = value;
            }
        }
        public static byte WaitTimeMultiplikator
        {
            get { return (byte)(115200 / CurrentSerialPort.Baudrate); }
        }

        public static SerialPort ControllerSerialPort
        {
            get 
            {
                if (controllerSerialPort == null)
                {
                    controllerSerialPort = new SerialPort((SerialCommunication.CurrentSerialPort.PortName), SerialCommunication.CurrentSerialPort.Baudrate);
                    controllerSerialPort.ReadTimeout = Timeout;//2000;
                    controllerSerialPort.WriteTimeout = Timeout;//2000;
                }

                return controllerSerialPort;
            }
            set { controllerSerialPort = value; }
        }
        #endregion

        #region Methods
        private static List<byte> PreapereWritePackage(List<byte> packageToChange, int startIndex)
        {
            List<byte> newPackage = new List<byte>();

            for (int i = startIndex; i < packageToChange.Count; i++)
            {
                if (packageToChange[i] == 0xA5)
                    newPackage.AddRange(new byte[] { 0xAA, 0x05 });
                else if (packageToChange[i] == 0xAA)
                    newPackage.AddRange(new byte[] { 0xAA, 0x0A });
                else if (packageToChange[i] == 0xAE)
                    newPackage.AddRange(new byte[] { 0xAA, 0x0E });
                else
                    newPackage.Add(packageToChange[i]);
            }
            
            packageToChange.RemoveRange(startIndex, packageToChange.Count - startIndex);
            packageToChange.AddRange(newPackage);

            return packageToChange;//.ToArray();//retval;
        }
        private static List<byte> PreapereReadPackage(List<byte> packageToChange, int startIndex)
        {
            List<byte> newPackage = new List<byte>();

            for (int i = startIndex; i < packageToChange.Count; i++)
            {
                if (packageToChange[i] == 0xAA)
                {
                    if (packageToChange[i + 1] == 0x05)
                    {
                        newPackage.Add(0xA5);
                        i++;
                    }
                    else if (packageToChange[i + 1] == 0x0A)
                    {
                        newPackage.Add(0xAA);
                        i++;
                    }
                    else if (packageToChange[i + 1] == 0x0E)
                    {
                        newPackage.Add(0xAE);
                        i++;
                    }
                }
                else
                    newPackage.Add(packageToChange[i]);
            }

            packageToChange.RemoveRange(startIndex, packageToChange.Count - startIndex);
            packageToChange.AddRange(newPackage);

            return packageToChange;//.ToArray();//retval;
        }

        private static List<byte> PreapereReadPackage(List<byte> packageToChange, int startIndex, int responseLength)
        {
            List<byte> newPackage = new List<byte>();

            for (int i = startIndex; i < responseLength; i++)
            {
                if (packageToChange[i] == 0xAA)
                {
                    if (packageToChange[i + 1] == 0x05)
                    {
                        newPackage.Add(0xA5);
                        i++;
                    }
                    else if (packageToChange[i + 1] == 0x0A)
                    {
                        newPackage.Add(0xAA);
                        i++;
                    }
                    else if (packageToChange[i + 1] == 0x0E)
                    {
                        newPackage.Add(0xAE);
                        i++;
                    }
                }
                else
                    newPackage.Add(packageToChange[i]);
            }

            packageToChange.RemoveRange(startIndex, packageToChange.Count - startIndex);
            packageToChange.AddRange(newPackage);

            return packageToChange;//.ToArray();//retval;
        }

        #region Send
        public static CommunicationResult Send(byte[] dataPackage)
        {
            CommunicationResult retval = CommunicationResult.Success;

            try
            {
                if (ControllerSerialPort.IsOpen) 
                {
                    try
                    {
                        ControllerSerialPort.Close();
                    }
                    catch
                    {
                        retval = CommunicationResult.ConnectionCloseFailed;
                    }
                }
                ControllerSerialPort = null;

                ControllerSerialPort.Open();

                if (ControllerSerialPort.IsOpen)
                {
                    List<byte> requestBuffer = new List<byte>();


                    // add start of the package
                    requestBuffer.Add(0xA5);

                    // copy data buffer
                    requestBuffer.AddRange(dataPackage);

                    // prepare package to write
                    requestBuffer = PreapereWritePackage(requestBuffer, 1);

                    // add end of the package
                    requestBuffer.Add(0xAE);

                    // write command data by COM port
                    ControllerSerialPort.Write(requestBuffer.ToArray(), 0, requestBuffer.Count);
                }
                else
                    retval = CommunicationResult.ConnectionOpenFailed;
            }
            catch
            {
                retval = CommunicationResult.RequestWritingFailed;

                if (ControllerSerialPort.IsOpen)
                {
                    try
                    {
                        ControllerSerialPort.Close();
                    }
                    catch
                    {
                        retval = CommunicationResult.ConnectionCloseFailed;
                    }

                    ControllerSerialPort = null;
                }
            }

            return retval;
        }
        #endregion
        #region Receive
        public static CommunicationResult Receive(out byte[] responsePackage, out int responseLength)
        {
            CommunicationResult retval = CommunicationResult.Success;

            responsePackage = new byte[1024];
            responseLength = 0;

            try
            {
                //responseLength = ControllerSerialPort.Read(responsePackage, responseLength, responsePackage.Length);


                if (SerialCommunication.BaudrateToChange != SerialCommunication.CurrentSerialPort.Baudrate)
                {
                    Thread.Sleep(100);
                    //ControllerSerialPort.Close();
                    //ControllerSerialPort = null;
                    CurrentSerialPort.Baudrate = SerialCommunication.BaudrateToChange;
                    SerialCommunication.ControllerSerialPort.BaudRate = SerialCommunication.BaudrateToChange;
                    //ControllerSerialPort.Open();

                }
                //SerialCommunication.CurrentSerialPort.Baudrate = SerialCommunication.BaudrateToChange;
                //SerialCommunication.ControllerSerialPort.BaudRate = SerialCommunication.BaudrateToChange;

                List<byte> tmplist = new List<byte>();
                while(true)
                {
                    tmplist.Add((byte)ControllerSerialPort.ReadByte());
                    if (tmplist[tmplist.Count - 1] == 0xAE)
                        break;
                }

                responseLength = tmplist.Count;
                tmplist.CopyTo(responsePackage);

                //responseLength = ControllerSerialPort.Read(responsePackage, 
            }
            catch
            {
                if (ControllerSerialPort.IsOpen)
                {
                    try
                    {
                        ControllerSerialPort.Close();
                    }
                    catch
                    {
                        retval = CommunicationResult.ConnectionCloseFailed;
                    }
                }
                ControllerSerialPort = null;

                retval = CommunicationResult.ResponseReadingFailed;
                return retval;
            }

            List<byte> newPackage = PreapereReadPackage(new List<byte>(responsePackage), 1, responseLength);
            if (newPackage[0] == 0xA5 && newPackage[newPackage.Count-1] == 0xAE)
            {
                newPackage.RemoveAt(0);

                newPackage.RemoveAt(newPackage.Count - 1);
                responseLength = newPackage.Count;
                responsePackage = new byte[newPackage.Count];
                newPackage.CopyTo(responsePackage);
            }
            else
                retval = CommunicationResult.BadDataTryAgain;


            try
            {
                ControllerSerialPort.Close();

                if (ControllerSerialPort.IsOpen)
                    retval = CommunicationResult.ConnectionCloseFailed;
            }
            catch
            {
                retval = CommunicationResult.ConnectionCloseFailed;
            }

            ControllerSerialPort = null;

            return retval;
        }
        #endregion

        public static List<AvailablePort> CreatePortsList(int maxNumber)
        {
            PortsList.Clear();
            DevicesLists.Clear();

            using (SerialPort testSerialPort = new SerialPort())
            {
                testSerialPort.ReadTimeout = 1000;
                testSerialPort.WriteTimeout = 1000;
                for (int i = 1; i <= maxNumber; i++)
                {
                    string portName = "COM" + i.ToString();

                    testSerialPort.PortName = portName;
                    testSerialPort.BaudRate = Baudrate;

                    try
                    {
                        testSerialPort.Open();

                        if (testSerialPort.IsOpen)
                        {
                            //AvailablePort tmpPort = new AvailablePort();
                            CurrentSerialPort = new AvailablePort(portName, baudrate);

                            //CurrentSerialPort.PortName = portName;
                            //CurrentSerialPort.Tested = false;
                            //CurrentSerialPort.Baudrate = baudrate;

                            PortsList.Add(CurrentSerialPort);

                            testSerialPort.Close();
                        }
                    }
                    catch
                    {
                        if (testSerialPort.IsOpen)
                            testSerialPort.Close();
                    }
                }
            }

            return PortsList;
        }
        public static int CreateTestedPortsList(ref LogWindow lw, string message, ref System.Windows.Forms.ProgressBar pb, bool breakWhenFound)
        {
            DevicesLists.Clear();
            int[] portBaudrates = { 115200, 57600, 38400, 19200, 9600, 4800, 2400 };
            MainWindow.SetupProgressBar(ref pb, PortsList.Count * portBaudrates.Length);
            CommunicationResult result = CommunicationResult.Failed;

            Communication.CardID = 255;

            foreach (int baudrate in portBaudrates)
            {
                for (int i = 0; i < PortsList.Count; i++)
                {
                    MainWindow.WriteProgress(ref pb);


                    //AvailablePort port = PortsList[i];
                    //CurrentSerialPort = PortsList[i];

                    CurrentSerialPort = new AvailablePort(PortsList[i].PortName, baudrate);
                    MainWindow.Log(ref lw, String.Format(message, i + 1, PortsList.Count, CurrentSerialPort.PortName, baudrate));

                    //Communication.Type = CommunicationType.Serial;
                    result = Communication.TestController();

                    if (result == CommunicationResult.Success)
                    {

                        CurrentSerialPort = new AvailablePort(CurrentSerialPort.PortName, baudrate);
                        DevicesLists.Add(CurrentSerialPort);
                        if (breakWhenFound == true)
                        {
                            pb.Value = pb.Maximum;
                            Baudrate = baudrate;
                            break;
                        }
                    }
                }
                if ((result == CommunicationResult.Success) && (breakWhenFound == true))
                    break;
            }
            MainWindow.SetupProgressBar(ref pb, 0);
            CurrentSerialPort = new AvailablePort("test", 0);

            return DevicesLists.Count;
        }

        #endregion
    }
}
