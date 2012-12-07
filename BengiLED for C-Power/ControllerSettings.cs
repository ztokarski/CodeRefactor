using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;

namespace BengiLED_for_C_Power
{
    [Serializable()]
    public class ControllerSettings
    {
        #region Private fields
        private NetworkConfiguration netConfig;
        private ControllerType series = ControllerType.unknown;
        private ControllerType type = ControllerType.unknown;
        private int number = 0;
        private bool hasNetwork = false;
        private int id = 255;
        private int baudrate = 0;
        #endregion

        #region Public fields
        public int[] maxHeightGlobalArray = new int[2];
        public int[][] maxWidthGlobalArray = new int[2][];
        public int[] configListsValues = new int[12];
        public int[] configMaxValues = new int[13];
        public byte[] reservedValues = new byte[20];
        public byte[] screenConfigBytes = new byte[1];
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets information about availibility of network card in controller.
        /// </summary>
        public bool HasNetwork
        {
            get { return hasNetwork; }
            set { hasNetwork = value; }
        }
        /// <summary>
        /// Network configurationof the controller.
        /// </summary>
        public NetworkConfiguration NetConfig
        {
            get { return netConfig; }
            set { netConfig = value; }
        }
        /// <summary>
        /// Controller series - MARK-XX or C-PowerX200
        /// </summary>
        public ControllerType Series
        {
            get { return series; }
            set
            {
                if (Enum.IsDefined(typeof(ControllerType), value))
                    series = value;
                else
                    series = ControllerType.unknown;
            }
        }
        /// <summary>
        /// Controller type.
        /// </summary>
        public ControllerType Type
        {
            get { return type; }
            set 
            {
                if (Enum.IsDefined(typeof(ControllerType), value))
                    type = value;
                else
                {
                    type = ControllerType.unknown;
                    number = (int)value;
                }
            }
        }

        /// <summary>
        /// Controller name.
        /// </summary>
        public string Name
        {
            get 
            {
                if (Type == ControllerType.unknown && Series == ControllerType.MARK_XX)
                    return string.Format("MARK-{0}", number);
                else
                    return Enum.GetName(typeof(ControllerType), Type).Replace('_', '-'); 
            }
        }

        public int ID
        {
            get { return id; }
            set { id = value; }
        }
        public int Baudrate
        {
            get { return baudrate; }
            set { baudrate = value; }
        }
        #endregion

        #region Methods

        public void SaveToFile(System.IO.Stream streamfile)
        {
            BinaryFormatter formatter = new BinaryFormatter();

            formatter.Serialize(streamfile, this);
            streamfile.Close();

        }

        public static ControllerSettings LoadFromFile(System.IO.Stream streamfile)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            streamfile.Position = 0;
            streamfile.Seek(0, System.IO.SeekOrigin.Begin);
            return (ControllerSettings)formatter.Deserialize(streamfile);
        }

        #endregion
    }

    [Serializable()]
    public class NetworkConfiguration
    {
        #region Private fields
        private IPAddress controllerIP;
        private int port;
        private IPAddress gatewayIP;
        private IPAddress subnetMask;
        private IPAddress networkIDCode;
        #endregion

        #region Properties
        public IPAddress ControllerIP
        {
            get { return controllerIP; }
            set { controllerIP = value; }
        }
        public int Port
        {
            get { return port; }
            set { port = value; }
        }
        public IPAddress GatewayIP
        {
            get { return gatewayIP; }
            set { gatewayIP = value; }
        }
        public IPAddress SubnetMask
        {
            get { return subnetMask; }
            set { subnetMask = value; }
        }
        public IPAddress NetworkIDCode
        {
            get { return networkIDCode; }
            set { networkIDCode = value; }
        }
        #endregion

        #region Methods

        public NetworkConfiguration(IPAddress controllerIP, int port, IPAddress gatewayIP, IPAddress subnetMask, IPAddress networkIDCode)
        {
            this.ControllerIP = controllerIP;
            this.Port = port;
            this.GatewayIP = gatewayIP;
            this.SubnetMask = subnetMask;
            this.NetworkIDCode = networkIDCode;
        }

        #endregion
    }

    #region ControllerType
    public enum ControllerType
    {
        unknown = -1,
        C_Power1200 = 18,
        C_Power2200 = 33,
        C_Power3200 = 34,
        C_Power4200 = 50,
        C_Power5200 = 51,
        C_PowerX200 = 200,
        MARK_24 = 24,
        MARK_56 = 56,
        MARK_120 = 120,
        MARK_XX = 77
    }
    #endregion

}
