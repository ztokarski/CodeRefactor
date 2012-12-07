using System;
using System.Collections.Generic;
using System.Drawing;

namespace BengiLED_for_C_Power
{
    [Serializable()]
    public class PlayWindow
    {
        #region Private fields
        private int windowNumber = 0;
        private string windowName = "window0";
        private uint x = 0;
        private uint y = 0;
        private uint width = 96;
        private uint height = 16;
        private byte waiting = 1;
        #endregion

        #region Properties
        public string Name
        {
            get {return windowName;}
            set 
            {
                if (value == string.Empty)
                    windowName = "Window" + windowNumber;
                else
                    windowName = value;
            }
        }

        public int Number
        {
            get { return windowNumber; }
            set { windowNumber = value; }
        }

        public uint X
        {
            get {return x;}
            set {x = value;}
        }
        public uint Y
        {
            get {return y;}
            set {y = value;}
        }
        public uint Width
        {
            get {return width;}
            set {width = value;}
        }
        public uint Height
        {
            get { return height; }
            set { height = value; }
        }

        public byte Waiting
        {
            get { return waiting; }
            set { waiting = value; }
        }

        public List<PlayWindowItem> ItemsList;
        #endregion

        #region Methods

        #region Constructors and destructor
        /// <summary>
        /// Creates window with default settings.
        /// </summary>
        public PlayWindow()
        {
            ItemsList = new List<PlayWindowItem>();
        }
        /// <summary>
        /// Creates window with specified name
        /// </summary>
        /// <param name="windowName">Name for new window.</param>
        public PlayWindow(string windowName)
        {
            Name = windowName;

            ItemsList = new List<PlayWindowItem>();
        }

        public PlayWindow(string windowName, uint x, uint y, uint width, uint height)
        {
            Name = windowName;
            X = x;
            Y = y;
            Width = width;
            Height = height;

            ItemsList = new List<PlayWindowItem>();
        }

        ~PlayWindow()
        {
            ItemsList.Clear();
        }
        #endregion

        #region Adding items to window
        public void AddItem()
        {
            ItemsList.Add(new PlayWindowItem(PlayWindowItemType.BitmapText));
        }
        public void AddItem(PlayWindowItemType type)
        {
            ItemsList.Add(new PlayWindowItem(type));
        }

        public void AddItem(System.Collections.Hashtable dane)
        {
            ItemsList.Add(new PlayWindowItem(dane));
        }

        public void AddTextItem(string text, int fontSize, UInt32 color, int effect, int speed, int stay)
        {
            ItemsList.Add(new PlayWindowItem(text, fontSize, color, effect, speed, stay));
        }

        public void AddBMPItem(int mode, int compress, string filePath, int effect, int speed, int stay)
        {
            ItemsList.Add(new PlayWindowItem(mode, compress, filePath, effect, speed, stay));
        }

        public void AddBMPTextItem(string text, int fontSize, UInt32 color, int effect, int speed, int stay, int mode, int compress)
        {
            ItemsList.Add(new PlayWindowItem(text, fontSize, color, effect, speed, stay, mode, compress));
        }

        #endregion

        
        #endregion
    }
}
