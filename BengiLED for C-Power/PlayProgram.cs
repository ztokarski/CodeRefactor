using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Collections;

namespace BengiLED_for_C_Power
{
    [Serializable()]
    public class PlayProgram
    {
        #region Private fields
        private string programName = "program";
        private string fileName = "program.lpb";
        private ushort width = 96;
        private ushort height = 16;
        private byte color = 1;
        private int repeatTimes = 1;
        private int playTime = 0;
        // limited-time play fields
        private bool ltp = false;
        private bool[] weekdays = new bool[7] { false, false, false, false, false, false, false };
        private byte beginminute = 0;
        private byte beginhour = 0;
        private byte endhour = 0;
        private byte endminute = 0;


        #endregion

        #region Properties
        public static byte[] FileHeader
        {
            get { return new byte[] { 0x4C, 0x50, 0x00, 0x01 }; }
        }
        public int RepeatTimes
        {
            get { return repeatTimes; }
            set { repeatTimes = value; }
        }
        public int PlayTime
        {
            get { return playTime; }
            set { playTime = value; }
        }

        public string Name
        {
            get {return programName;}
            set 
            {
                programName = value;
                //fileName = value + ".lpb";
            }
        }
        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }
        public ushort Width
        {
            get { return width; }
            set 
            { 
                width = value;
                if (this.WindowsList != null)
                {
                    foreach (PlayWindow w in this.WindowsList)
                    {
                        if (w.X + w.Width > value)
                        { 
                            uint tmp = ((w.X + w.Width) - value);
                            if (tmp <= w.X)
                                w.X -= tmp;
                            else
                            {
                                tmp -= w.X;
                                w.X = 0;
                                w.Width -= tmp;
                            }
                        }
                    }
                }
            }
        }

        public ushort Height
        {
            get { return height; }
            set
            {
                height = value;
                if (this.WindowsList != null)
                {
                    foreach (PlayWindow w in this.WindowsList)
                    {
                        if (w.Y + w.Height > value)
                        {
                            uint tmp = ((w.Y + w.Height) - value);
                            if (tmp <= w.Y)
                                w.Y -= tmp;
                            else
                            {
                                tmp -= w.Y;
                                w.Y = 0;
                                w.Height -= tmp;
                            }
                        }
                    }
                }
            }
        }

        public byte Color
        {
            get { return color; }
            set { color = value; }
        }

        public bool LTP
        {
            get { return ltp; }
            set { ltp = value; }
        }

        public byte BeginMinute
        {
            get { return beginminute; }
            set { beginminute = value; }
        }

        public byte BeginHour
        {
            get { return beginhour; }
            set { beginhour = value; }
        }

        public byte EndMinute
        {
            get { return endminute; }
            set { endminute = value; }
        }

        public byte EndHour
        {
            get { return endhour; }
            set { endhour = value; }
        }

        public bool[] WeekDays
        {
            get { return weekdays; }
            set { weekdays = value; }
        }

        public List<PlayWindow> WindowsList;
        #endregion

        #region Methods
        public PlayProgram(string programName)
        {
            Name = programName;
            FileName = Name + ".lpb";
            weekdays = new bool[7] { false, false, false, false, false, false, false };

            WindowsList = new List<PlayWindow>();
        }
        public PlayProgram(string programName, ushort width, ushort height, byte color)
        {
            Name = programName;
            FileName = Name + ".lpb";
            Width = width;
            Height = height;
            Color = color;
            weekdays = new bool[7] { false, false, false, false, false, false, false };

            WindowsList = new List<PlayWindow>();
        }

        ~PlayProgram()
        {
            WindowsList.Clear();
        }


        public byte[] GenerateBitmapBytes(Bitmap inputBmp)
        {
            List<byte> retval = new List<byte>(0);

            BitArray redBits = new BitArray(0);
            BitArray greenBits = new BitArray(0);
            BitArray blueBits = new BitArray(0);

            System.Drawing.Color pixelColor = System.Drawing.Color.Black;
            int currentBitIndex = 0;

            switch (this.Color)
            {
                case 1: // R
                    {
                        #region Red
                        redBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        lock (inputBmp)
                        {
                            for (int y = 0; y < inputBmp.Height; y++)
                            {
                                for (int x = 0; x < inputBmp.Width; x++)
                                {
                                    pixelColor = inputBmp.GetPixel(x, y);
                                    currentBitIndex = (inputBmp.Width - x - 1) + ((inputBmp.Height - y - 1) * inputBmp.Width);

                                    if (pixelColor.R > 127)
                                        redBits[currentBitIndex] = true;
                                }
                            }
                        }
                        break;
                        #endregion
                    }
                case 2: // G
                    {
                        #region Green
                        redBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        greenBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        lock (inputBmp)
                        {
                            for (int y = 0; y < inputBmp.Height; y++)
                            {
                                for (int x = 0; x < inputBmp.Width; x++)
                                {
                                    pixelColor = inputBmp.GetPixel(x, y);
                                    currentBitIndex = (inputBmp.Width - x - 1) + ((inputBmp.Height - y - 1) * inputBmp.Width);

                                    if (pixelColor.G > 127)
                                        greenBits[currentBitIndex] = true;
                                }
                            }
                        }
                        break;
                        #endregion
                    }
                case 3: // RG
                    {
                        #region RG
                        redBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        greenBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        lock (inputBmp)
                        {
                            for (int y = 0; y < inputBmp.Height; y++)
                            {
                                for (int x = 0; x < inputBmp.Width; x++)
                                {
                                    pixelColor = inputBmp.GetPixel(x, y);
                                    currentBitIndex = (inputBmp.Width - x - 1) + ((inputBmp.Height - y - 1) * inputBmp.Width);

                                    if (pixelColor.R > 127)
                                        redBits[currentBitIndex] = true;
                                    if (pixelColor.G > 127)
                                        greenBits[currentBitIndex] = true;
                                }
                            }
                        }
                        break;
                        #endregion
                    }
                case 4: // B
                    {
                        #region Blue
                        redBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        greenBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        blueBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        lock (inputBmp)
                        {
                            for (int y = 0; y < inputBmp.Height; y++)
                            {
                                for (int x = 0; x < inputBmp.Width; x++)
                                {
                                    pixelColor = inputBmp.GetPixel(x, y);
                                    currentBitIndex = (inputBmp.Width - x - 1) + ((inputBmp.Height - y - 1) * inputBmp.Width);

                                    if (pixelColor.B > 127)
                                        blueBits[currentBitIndex] = true;
                                }
                            }
                        }
                        break;
                        #endregion
                    }
                case 5: // RB
                    {
                        #region RB
                        redBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        greenBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        blueBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        lock (inputBmp)
                        {
                            for (int y = 0; y < inputBmp.Height; y++)
                            {
                                for (int x = 0; x < inputBmp.Width; x++)
                                {
                                    pixelColor = inputBmp.GetPixel(x, y);
                                    currentBitIndex = (inputBmp.Width - x - 1) + ((inputBmp.Height - y - 1) * inputBmp.Width);

                                    if (pixelColor.R > 127)
                                        redBits[currentBitIndex] = true;
                                    if (pixelColor.B > 127)
                                        blueBits[currentBitIndex] = true;
                                }
                            }
                        }
                        break;
                        #endregion
                    }
                case 6: // GB
                    {
                        #region GB
                        redBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        greenBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        blueBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        lock (inputBmp)
                        {
                            for (int y = 0; y < inputBmp.Height; y++)
                            {
                                for (int x = 0; x < inputBmp.Width; x++)
                                {
                                    pixelColor = inputBmp.GetPixel(x, y);
                                    currentBitIndex = (inputBmp.Width - x - 1) + ((inputBmp.Height - y - 1) * inputBmp.Width);

                                    if (pixelColor.R > 127)
                                        redBits[currentBitIndex] = true;
                                    if (pixelColor.G > 127)
                                        greenBits[currentBitIndex] = true;
                                }
                            }
                        }
                        break;
                        #endregion
                    }
                case 7: // RGB
                    {
                        #region RGB
                        redBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        greenBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        blueBits = new BitArray(inputBmp.Width * inputBmp.Height, false);
                        lock (inputBmp)
                        {
                            for (int y = 0; y < inputBmp.Height; y++)
                            {
                                for (int x = 0; x < inputBmp.Width; x++)
                                {
                                    pixelColor = inputBmp.GetPixel(x, y);
                                    currentBitIndex = (inputBmp.Width - x - 1) + ((inputBmp.Height - y - 1) * inputBmp.Width);

                                    if (pixelColor.R > 127)
                                        redBits[currentBitIndex] = true;
                                    if (pixelColor.G > 127)
                                        greenBits[currentBitIndex] = true;
                                    if (pixelColor.B > 127)
                                        blueBits[currentBitIndex] = true;
                                }
                            }
                        }
                        break;
                        #endregion
                    }
            }

            byte[] tmpColorBytes;

            // red
            tmpColorBytes = new byte[redBits.Length / 8];
            redBits.CopyTo(tmpColorBytes, 0);
            Array.Reverse(tmpColorBytes);
            retval.AddRange(tmpColorBytes);
            // green
            tmpColorBytes = new byte[greenBits.Length / 8];
            greenBits.CopyTo(tmpColorBytes, 0);
            Array.Reverse(tmpColorBytes);
            retval.AddRange(tmpColorBytes);
            // blue
            tmpColorBytes = new byte[blueBits.Length / 8];
            blueBits.CopyTo(tmpColorBytes, 0);
            Array.Reverse(tmpColorBytes);
            retval.AddRange(tmpColorBytes);

            return retval.ToArray();
        }

        public void SaveToFile(string fileName)
        {
            FileName = fileName;
            string tempDirectory = string.Format("{0}/temp", MainWindow.programSettingsFolder); //"temp";

            List<byte> programFileBytes = new List<byte>();
            int itemsCount = 0, extendedItemsCount = 0;
            int windowsInfoOffsetIndex = 0x30, itemsInfoOffsetIndex = 0,
                extendedItemsInfOffsetIndex = 0, extendedItemsContentOffsetIndex = 0;
            int previousWindowItemsCount = 0;
            BitArray colorBits = new BitArray(new byte[] { this.Color });

            // program file header
            programFileBytes.AddRange(FileHeader);
            // windows count
            programFileBytes.AddRange(BitConverter.GetBytes((Int16)this.WindowsList.Count));
            // items count and extended (with more content) items count
            foreach (PlayWindow window in WindowsList)
            {
                itemsCount += window.ItemsList.Count;

                foreach (PlayWindowItem item in window.ItemsList)
                {
                    if (item.HasExtendedContent)
                        extendedItemsCount++;
                }
            }
            programFileBytes.AddRange(BitConverter.GetBytes((Int16)extendedItemsCount));
            // background info (?)
            programFileBytes.AddRange(new byte[] { 0xFF, 0xFF });
            // (?)
            programFileBytes.AddRange(new byte[] { 0x00, 0x00 });
            // program repeat times
            programFileBytes.AddRange(BitConverter.GetBytes((Int16)this.RepeatTimes));
            // program play time (seconds)
            programFileBytes.AddRange(BitConverter.GetBytes((Int16)this.PlayTime));

            // window info offset
            programFileBytes.AddRange(BitConverter.GetBytes((Int32)windowsInfoOffsetIndex));
            // items info offset
            itemsInfoOffsetIndex = windowsInfoOffsetIndex + (16*this.WindowsList.Count);
            programFileBytes.AddRange(BitConverter.GetBytes((Int32)itemsInfoOffsetIndex));
            // extended items info offset
            extendedItemsInfOffsetIndex = itemsInfoOffsetIndex + (16 * itemsCount);
            programFileBytes.AddRange(BitConverter.GetBytes((Int32)extendedItemsInfOffsetIndex));
            // extended items content offset
            extendedItemsContentOffsetIndex = extendedItemsInfOffsetIndex + (8 * extendedItemsCount);
            programFileBytes.AddRange(BitConverter.GetBytes((Int32)extendedItemsContentOffsetIndex));
            // 16 zeros
            programFileBytes.AddRange(new byte[16]);
            
            // lines for each window
            int windowNumber = 0;
            foreach(PlayWindow window in WindowsList)
            {
                window.Number = windowNumber++;

                // window number
                programFileBytes.Add(Convert.ToByte(window.Number));
                // waiting mode (still = 00, loop = 01, hide = 02)
                programFileBytes.Add(window.Waiting);
                // left
                programFileBytes.AddRange(BitConverter.GetBytes((Int16)window.X));
                // top
                programFileBytes.AddRange(BitConverter.GetBytes((Int16)window.Y));
                // width
                programFileBytes.AddRange(BitConverter.GetBytes((Int16)window.Width));
                // height
                programFileBytes.AddRange(BitConverter.GetBytes((Int16)window.Height));
                // items count
                programFileBytes.AddRange(BitConverter.GetBytes((Int16)window.ItemsList.Count));
                // items offset (16*previousWindowItemsCount)
                programFileBytes.AddRange(BitConverter.GetBytes((Int32)16*previousWindowItemsCount));

                // update previousWindowItemsCount with current window's items count
                previousWindowItemsCount = window.ItemsList.Count;
            }

            int imagesCount = 0;
            int currentContentOffset = 0;
            int extendedItemContentSize = 0;
            List<byte> extendedItemsInfoBytes = new List<byte>();
            List<byte> extendedItemsContentBytes = new List<byte>();

            foreach (PlayWindow window in WindowsList)
            {
                // line for each item
                int itemIndex = 0;
                foreach(PlayWindowItem item in window.ItemsList)
                {
                    int result = -1;
                    if (item.ItemType == PlayWindowItemType.Text)
                    {
                        //   result = CP5200_Program_AddText(programHandle, windowNumber, Marshal.StringToHGlobalAnsi(item.Text), 
                        //          item.FontSize, item.Color, item.Effect, item.Speed, item.Stay);
                    }
                    else if (item.ItemType == PlayWindowItemType.BitmapText)
                    {
                        // item's type byte - 01 = bitmap
                        programFileBytes.Add(0x01);
                        // transparent status and color
                        programFileBytes.Add(Convert.ToByte((item.Transparent) ? 0x80 : 0x00));
                        //programFileBytes.Add(Convert.ToByte(item.TransparentColor));
                        // image number in program
                        programFileBytes.AddRange(BitConverter.GetBytes((Int16)imagesCount++));
                        // effect
                        programFileBytes.AddRange(BitConverter.GetBytes((Int16)item.Effect));
                        // speed
                        programFileBytes.AddRange(BitConverter.GetBytes((Int16)item.Speed));
                        // stay or scroll repeat
                        programFileBytes.AddRange(BitConverter.GetBytes((Enum.GetName(typeof(EffectType), item.Effect).Contains("Scroll")) 
                            ? (Int16)(item.Stay - 1) : (Int16)item.Stay));
                        // fill rest of line with zeros
                        programFileBytes.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00});

                        // extended item information line

                        // content size (with header)
                        int colorMultiplicator = (colorBits[2]) ? 3 : ((colorBits[1]) ? 2 : ((colorBits[0]) ? 1 : 0));
                        extendedItemContentSize = (int)(10 + ((window.Width * window.Height / 8) * colorMultiplicator * item.BitmapsForScreen.Count));
                        extendedItemsInfoBytes.AddRange(BitConverter.GetBytes((Int32)extendedItemContentSize));
                        extendedItemsInfoBytes.RemoveAt(extendedItemsInfoBytes.Count - 1); // size should take only 3 bytes
                        // item type
                        extendedItemsInfoBytes.Add(0x01);
                        // offset of this item's content
                        extendedItemsInfoBytes.AddRange(BitConverter.GetBytes((Int32)currentContentOffset));
                        
                        // update current item's content offset
                        currentContentOffset += extendedItemContentSize;


                        // extended content

                        // header
                        // width
                        extendedItemsContentBytes.AddRange(BitConverter.GetBytes((Int16)window.Width));
                        // height
                        extendedItemsContentBytes.AddRange(BitConverter.GetBytes((Int16)window.Height));
                        // red bitmaps count
                        extendedItemsContentBytes.AddRange(BitConverter.GetBytes((Int16)((colorBits[0]) ? item.BitmapsForScreen.Count : 0x00)));
                        // green bitmaps count
                        extendedItemsContentBytes.AddRange(BitConverter.GetBytes((Int16)((colorBits[1]) ? item.BitmapsForScreen.Count : 0x00)));
                        // blue bitmaps count
                        extendedItemsContentBytes.AddRange(BitConverter.GetBytes((Int16)((colorBits[2]) ? item.BitmapsForScreen.Count : 0x00)));


                        itemIndex++;
                        try
                        {
                            int bla = 0;
                            foreach (Bitmap b in item.BitmapsForScreen)
                            {
                                // bitmap bytes
                                extendedItemsContentBytes.AddRange(GenerateBitmapBytes(b));
                            }
                        }
                        catch
                        {
                            System.Windows.Forms.MessageBox.Show(string.Format("Bitmaptext generating problem! \nProgram name: {0}\nItem number: {1}", this.Name, itemIndex));
                        }
                    }
                    else if (item.ItemType == PlayWindowItemType.Bitmap)
                    {
                        // item's type byte - 01 = bitmap
                        programFileBytes.Add(0x01);
                        // transparent status and color
                        programFileBytes.Add(Convert.ToByte((item.Transparent) ? 0x80 : 0x00));
                        //programFileBytes.Add(Convert.ToByte(item.TransparentColor));
                        // image number in program
                        programFileBytes.AddRange(BitConverter.GetBytes((Int16)imagesCount++));
                        // effect
                        programFileBytes.AddRange(BitConverter.GetBytes((Int16)item.Effect));
                        // speed
                        programFileBytes.AddRange(BitConverter.GetBytes((Int16)item.Speed));
                        // stay or scroll repeat
                        programFileBytes.AddRange(BitConverter.GetBytes((Enum.GetName(typeof(EffectType), item.Effect).Contains("Scroll"))
                            ? (Int16)(item.Stay - 1) : (Int16)item.Stay));
                        // fill rest of line with zeros
                        programFileBytes.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

                        // extended item information line

                        // content size (with header)
                        int colorMultiplicator = (colorBits[2]) ? 3 : ((colorBits[1]) ? 2 : ((colorBits[0]) ? 1 : 0));
                        extendedItemContentSize = (int)(10 + ((window.Width * window.Height / 8) * colorMultiplicator * item.BitmapsForScreen.Count));
                        extendedItemsInfoBytes.AddRange(BitConverter.GetBytes((Int32)extendedItemContentSize));
                        extendedItemsInfoBytes.RemoveAt(extendedItemsInfoBytes.Count - 1); // size should take only 3 bytes
                        // item type
                        extendedItemsInfoBytes.Add(0x01);
                        // offset of this item's content
                        extendedItemsInfoBytes.AddRange(BitConverter.GetBytes((Int32)currentContentOffset));

                        // update current item's content offset
                        currentContentOffset += extendedItemContentSize;


                        // extended content

                        // header
                        // width
                        extendedItemsContentBytes.AddRange(BitConverter.GetBytes((Int16)window.Width));
                        // height
                        extendedItemsContentBytes.AddRange(BitConverter.GetBytes((Int16)window.Height));
                        // red bitmaps count
                        extendedItemsContentBytes.AddRange(BitConverter.GetBytes((Int16)((colorBits[0]) ? item.BitmapsForScreen.Count : 0x00)));
                        // green bitmaps count
                        extendedItemsContentBytes.AddRange(BitConverter.GetBytes((Int16)((colorBits[1]) ? item.BitmapsForScreen.Count : 0x00)));
                        // blue bitmaps count
                        extendedItemsContentBytes.AddRange(BitConverter.GetBytes((Int16)((colorBits[2]) ? item.BitmapsForScreen.Count : 0x00)));

                        // bitmap bytes
                        foreach (Bitmap b in item.BitmapsForScreen)
                            extendedItemsContentBytes.AddRange(GenerateBitmapBytes(b));
                    }
                    else if (item.ItemType == PlayWindowItemType.Clock)
                    {
                        int clockAttributes = 0;
                        // generate 2 bytes from clock attributes
                        uint att = goodNumber(item);
                        // include date format index
                        clockAttributes = (int)att | (((item.DateFormatIndex + 1) * 8) << 8);
                        // include transparent info
                        clockAttributes = clockAttributes | (((item.Transparent) ? 1 : 0) * (2 << 14)); 

                        // item's type byte - 05 = clock
                        programFileBytes.Add(0x05);
                        // ? - 00
                        programFileBytes.Add(0x00);
                        // FF FF if there is no caption, 00 00 if there is
                        programFileBytes.AddRange(new byte[] { 0xFF, 0xFF });
                        // clock attributes
                        byte[] clockAttributesBytes = BitConverter.GetBytes((Int16)clockAttributes);
                        Array.Reverse(clockAttributesBytes);
                        programFileBytes.AddRange(clockAttributesBytes);
                        // font size index
                        programFileBytes.Add(Convert.ToByte(item.FontSizeIndex));
                        // font color
                        programFileBytes.AddRange(BitConverter.GetBytes((Int32)item.Color));
                        programFileBytes.RemoveAt(programFileBytes.Count - 1); //color has only 3 bytes
                        // stay
                        programFileBytes.AddRange(BitConverter.GetBytes((Int16)item.Stay));
                        // timezone adjustment
                        programFileBytes.AddRange(new byte[] { 0x00, 0x00 });
                        // fill rest of line with zeros
                        programFileBytes.AddRange(new byte[] { 0x00, 0x00 });
                    }
                    else if (item.ItemType == PlayWindowItemType.Temperature)
                    {
                        // item's type byte - 06 = temperature
                        programFileBytes.Add(0x06);
                        // ? - 00
                        programFileBytes.Add(0x00);
                        // FF FF if there is no caption, 00 00 if there is
                        programFileBytes.AddRange(new byte[] { 0xFF, 0xFF });
                        // temperature type 
                        programFileBytes.Add(item.TempAtt);
                        // ? - 00
                        programFileBytes.Add(0x00);
                        // font size index
                        programFileBytes.Add(Convert.ToByte(item.FontSizeIndex));
                        // font color
                        programFileBytes.AddRange(BitConverter.GetBytes((Int32)item.Color));
                        programFileBytes.RemoveAt(programFileBytes.Count - 1); //color has only 3 bytes
                        // stay
                        programFileBytes.AddRange(BitConverter.GetBytes((Int16)item.Stay));
                        // fill rest of line with zeros
                        programFileBytes.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });
                    }
                    else if (item.ItemType == PlayWindowItemType.Animator)
                    {
                        // item's type byte - 04 = bitmap
                        programFileBytes.Add(0x04);
                        // transparent status and color
                        programFileBytes.Add(0x00);//Convert.ToByte(item.TransparentColor));
                        // image number in program
                        programFileBytes.AddRange(BitConverter.GetBytes((Int16)imagesCount++));
                        // frames count
                        programFileBytes.AddRange(BitConverter.GetBytes((Int16)item.BitmapsForScreen.Count));
                        // repeat
                        programFileBytes.AddRange(BitConverter.GetBytes((Int16)(item.Stay - 1)));
                        // ? = 00 00
                        programFileBytes.AddRange(new byte[] { 0x00, 0x00 });
                        // fill rest of line with zeros
                        programFileBytes.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

                        // extended item information line

                        // content size (with header)
                        int colorMultiplicator = (colorBits[2]) ? 3 : ((colorBits[1]) ? 2 : ((colorBits[0]) ? 1 : 0));
                        extendedItemContentSize = (int)(((10 + (window.Width * window.Height / 8) * colorMultiplicator) * item.BitmapsForScreen.Count));
                        extendedItemsInfoBytes.AddRange(BitConverter.GetBytes((Int32)extendedItemContentSize));
                        extendedItemsInfoBytes.RemoveAt(extendedItemsInfoBytes.Count - 1); // size should take only 3 bytes
                        // item type
                        extendedItemsInfoBytes.Add(0x04);
                        // offset of this item's content
                        extendedItemsInfoBytes.AddRange(BitConverter.GetBytes((Int32)currentContentOffset));

                        // update current item's content offset
                        currentContentOffset += extendedItemContentSize;


                        // extended content

                        foreach (Bitmap b in item.BitmapsForScreen)
                        {
                            // header
                            // frame delay
                            extendedItemsContentBytes.AddRange(BitConverter.GetBytes((Int16)item.FrameDelay));
                            // ? = 00 00 00
                            extendedItemsContentBytes.AddRange(new byte[] { 0x00, 0x00, 0x00 });
                            // width
                            extendedItemsContentBytes.AddRange(BitConverter.GetBytes((Int16)window.Width));
                            // height
                            extendedItemsContentBytes.AddRange(BitConverter.GetBytes((Int16)window.Height));
                            // color
                            extendedItemsContentBytes.Add(this.Color);

                            // bitmap bytes
                            extendedItemsContentBytes.AddRange(GenerateBitmapBytes(b));
                        }
                    }
                }
            }

            programFileBytes.AddRange(extendedItemsInfoBytes);
            programFileBytes.AddRange(extendedItemsContentBytes);

            System.IO.FileStream programFile = new System.IO.FileStream(string.Format("{0}/{1}", tempDirectory, FileName), System.IO.FileMode.Create);
            programFile.Write(programFileBytes.ToArray(), 0, programFileBytes.Count);
            programFile.Close();
        }

        public void AddPlayWindow(string windowName)
        {
            //WindowsList.Add(new PlayWindow(windowName));
            WindowsList.Add(new PlayWindow(windowName, 0, 0, this.Width, this.Height));
        }

        public void AddPlayWindow(string windowName, uint x, uint y, uint width, uint height)
        { 
            WindowsList.Add(new PlayWindow(windowName, x, y, width, height));
        }

        public uint goodNumber(PlayWindowItem item)
        {
            Boolean[] item_attributes = item.Attributes;

            double auxiliary = 0;
            byte tmp = 0;

            for (int j = 0; j < 16; ++j)
            {
                if (item_attributes[j] == true)
                    tmp = 1;
                else
                    tmp = 0;
                    
                auxiliary += (System.Math.Pow(2, j) * tmp);
            
                    //auxiliary += (j == 13) ? (System.Math.Pow(2, j) * tmp) * 2 : (System.Math.Pow(2, j) * tmp);
            }

            return (uint)auxiliary;
        }

        #endregion
    }
}
