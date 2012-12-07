using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Net.Sockets;

namespace BengiLED_for_C_Power
{
    public class AvailableSocket
    {
        public System.Net.IPAddress Ip;
        public int IPport;

        public AvailableSocket(System.Net.IPAddress ip, int port)
        {
            Ip = ip;
            IPport = port;
        }
    }

    public class NetworkCommunication
    {
        #region Private Fields

        private static int cardID = 1;
        private static int port = 5200;
        private static int commTimeOut = 300;
        private static System.Net.IPAddress  iDcode = System.Net.IPAddress.Parse("255.255.255.255");
        private static Socket controllerSocket;
        private static System.Net.IPEndPoint controllerEndPoint;
        private static AvailableSocket testingSocket = new AvailableSocket(System.Net.IPAddress.Any, 0);

        #endregion

        #region Properties

        public static int CardID
        {
            get { return cardID; }
            set { cardID = value; }
        }
        public static List<AvailableSocket> SocketsList = new List<AvailableSocket>();
        public static System.Net.IPAddress IDcode
        {
            get { return iDcode; }
            set { iDcode = value; } 
        }
        public static int CommTimeOut
        {
            get { return commTimeOut; }
            set { commTimeOut = value; }
        }

        public static Socket ControllerSocket
        {
            get 
            {
                if (controllerSocket == null)
                {
                    controllerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    controllerSocket.ReceiveTimeout = commTimeOut;
                    controllerSocket.SendTimeout = commTimeOut;
                }
                
                return controllerSocket;
            }
            set { controllerSocket = value; }
        }

        public static System.Net.IPEndPoint ControllerEndPoint
        {
            get
            {
                if(controllerEndPoint == null)
                    controllerEndPoint = new System.Net.IPEndPoint(CurrentSocket.Ip, CurrentSocket.IPport);

                return controllerEndPoint;
            }
            set { controllerEndPoint = value; }
        }

        public static AvailableSocket CurrentSocket
        {
            get { return (testingSocket.Ip == System.Net.IPAddress.Any) ? SocketsList[0] : testingSocket; }
            set { testingSocket = value; }
        }

//        public static List<AvailablePort> DevicesLists = new List<AvailablePort>();
        // Maybe list of All sockets'll be here, but I think it's not good option.

        #endregion

        #region Methods

        #region Send
        public static CommunicationResult Send(byte[] dataPackages)
        {
            CommunicationResult retval = CommunicationResult.Success;

            try
            {
                IAsyncResult result = ControllerSocket.BeginConnect(ControllerEndPoint, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(commTimeOut, true);
            }
            catch
            {
                ControllerSocket.Close();
                ControllerSocket = null;
                retval = CommunicationResult.ConnectionOpenFailed;
            }

            if (ControllerSocket.Connected)
            {
                List<byte> requestBuffer = new List<byte>();
                int packageCount = 0;

                // add network communication ID code
                requestBuffer.AddRange(IDcode.GetAddressBytes());

                // add 4 bytes with command bytes count
                packageCount = dataPackages.Length;
                requestBuffer.AddRange(BitConverter.GetBytes(packageCount));

                // add command bytes
                requestBuffer.AddRange(dataPackages);

                // write create file data
                ControllerSocket.Send(requestBuffer.ToArray());
            }
            else
            {
                ControllerSocket.Close();
                ControllerSocket = null;
                retval = CommunicationResult.ConnectionOpenFailed;
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

            responseLength = ControllerSocket.Receive(responsePackage);

            responsePackage = PreapereReadPackage(responsePackage, responseLength).ToArray();
            responseLength = responsePackage.Length;

            
            ControllerSocket.Close();
            ControllerSocket = null;

            if (ControllerSocket.Connected)
                retval = CommunicationResult.ConnectionCloseFailed;    


            return retval;
        }
        #endregion


        private static List<byte> PreapereReadPackage(byte[] packageToChange, int responseLength)
        {
            List<byte> retval = new List<byte>();

            for (int i = IDcode.GetAddressBytes().Length + 4; i < responseLength; i++)
                retval.Add(packageToChange[i]);

            return retval;
        }
        public static AvailableSocket TestGivenSocket(string ipAdr)
        {
            
            AvailableSocket tmp = new AvailableSocket(System.Net.IPAddress.Parse(ipAdr), port);

            try
            {
                SocketsList.Clear();
                                
                try
                {
                    ControllerEndPoint = null;
                    CurrentSocket = tmp;

                    Communication.Type = CommunicationType.Network;
                    CommunicationResult result = Communication.TestController();
                    if(result != CommunicationResult.Success)
                        tmp.Ip = System.Net.IPAddress.Parse("0.0.0.0");
                }
                catch
                {
                    tmp.Ip = System.Net.IPAddress.Parse("0.0.0.0");
                } 

            }
            catch (Exception)
            {
                tmp.Ip = System.Net.IPAddress.Parse("0.0.0.0");
            }

            ControllerEndPoint = null;
            CurrentSocket = new AvailableSocket(System.Net.IPAddress.Any, 0);

            return tmp;
        }

        public static int SearchSockets(bool breakWhenFound, System.Net.IPAddress ip, int minAddressVal, int maxAddressVal)
        {
            SocketsList.Clear();

           byte[] tmpNetIp = ip.GetAddressBytes();
           UInt32 net = (UInt32)tmpNetIp[0] << 24;
           net += (UInt32)tmpNetIp[1] << 16;
           net += (UInt32)tmpNetIp[2] << 8;
          // codeID += (UInt32)tmpID[3];

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

            Communication.CardID = 255;

            for (int i = minAddressVal; i < maxAddressVal; ++i)
            {
                NetAddress = (net+i).ToString();

                tmp = TestGivenSocket(NetAddress);

               if (!(tmp.Ip.Equals(testedIP)))
                {
                    SocketsList.Add(tmp);
                    if (breakWhenFound == true)
                        break;
                }
            }


            return SocketsList.Count;
        }
        #endregion

    }
}
