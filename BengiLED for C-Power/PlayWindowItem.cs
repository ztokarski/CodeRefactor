using System;
using System.Collections.Generic;
using System.Drawing;

namespace BengiLED_for_C_Power
{
    #region Type enum definition
    public enum PlayWindowItemType
    {
        Text,
        Bitmap,
        FormattedText,
        BitmapText,
        Clock,
        Temperature,
        Animator
    }
    #endregion
    [Serializable()]
    public class PlayWindowItem
    {
        

        #region Private fields
        //common fields
        private PlayWindowItemType itemType;
        private bool hasExtendedContent = true;
        private bool transparent = false;
        private byte transparentColor = 0x00;
        private string text = "atlantech";
        private int effect = 0;
        private int speed = 0;
        private int stay = 3;

        private int bitmaptextStay = 3;
        private int bitmapStay = 3;
        private int clockStay = 3;
        private int temperatureStay = 3;
        private int animationStay = 7;

        private int bitmaptextSpeed = 0;
        private int bitmapSpeed = 0;

        private int bitmaptextFontSize = 16;
        private int clockFontSize = 16;
        private int temperatureFontSize = 16;

        private int bitmapMode = 0;
        private int animationMode = 0;

        private string bitmaptextFontColor = "Red";
        private string clockFontColor = "Red";
        private string temperatureFontColor = "Red";

        private int bitmaptextEffect = 0;
        private int bitmapEffect = 0;

        private List<Bitmap> bitmapsList = null;
        private Point offset = new Point(0, 0);
        //text fields
        private int fontSize = 12;
        private int fontSizeIndex = 1;
        private UInt32 color = 0x0000FF;
        //bmp fields
        private int mode = 0;
        private int compress = 0;
        //bmptext fields
        private string fontname = "Arial";
        private string fontcolor = "Red";
        private string bgcolor = "Black";
        private string filepath = "testFont.bmp";
        private string rtf = null;
        private List<Bitmap> bitmapsForScreen = null;
        // Clock fields
        private Boolean[] attributes = new Boolean[16] { false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, false };
        private int dateFormatIndex = 1;
        // Temperature fields
        private byte tempatt = 1;
        // animator fields
        private int frameDelay = 0;

        #endregion

        #region Properties
        public PlayWindowItemType ItemType
        {
            get { return itemType; }
            set 
            { 
                itemType = value;
                if (value == PlayWindowItemType.Text)
                {
                    text = "atlantech";
                    HasExtendedContent = true;
                }
                else if (value == PlayWindowItemType.Bitmap)
                {
                    text = "";
                    HasExtendedContent = true;
                }
                else if (value == PlayWindowItemType.BitmapText)
                {
                    text = "atlantech";
                    HasExtendedContent = true;
                }
                else if (value == PlayWindowItemType.Clock)
                {
                    text = "";
                    DateFormatIndex = 1;
                    HasExtendedContent = false;
                }
                else if (value == PlayWindowItemType.Temperature)
                {
                    text = "";
                    HasExtendedContent = false;
                }
                else if (value == PlayWindowItemType.Animator)
                {
                    text = "";
                    HasExtendedContent = true;
                }
            }
        }

        public bool HasExtendedContent
        {
            get { return hasExtendedContent; }
            set { hasExtendedContent = value; }
        }

        public int DateFormatIndex
        {
            get { return dateFormatIndex; }
            set { dateFormatIndex = value; }
        }
        public int FontSizeIndex
        {
            get { return fontSizeIndex; }
            set { fontSizeIndex = value; }
        }

        public int FrameDelay
        {
            get { return frameDelay; }
            set { frameDelay = value; }
        }

        public bool Transparent
        {
            get { return transparent; }
            set { transparent = value; }
        }

        public byte TransparentColor
        {
            get { return transparentColor; }
            set { transparentColor = value; }
        }

        public Point Offset
        {
            get { return offset; }
            set { offset = value; }
        }

        public string Text
        {
            get { return text; }
            set { text = value; }
        }
        public int Effect
        {
            get
            {
                if (ItemType == PlayWindowItemType.BitmapText)
                    return bitmaptextEffect;
                else if (ItemType == PlayWindowItemType.Bitmap)
                    return bitmapEffect;
                else
                    return effect;
            }
            set
            {
                if (ItemType == PlayWindowItemType.BitmapText)
                    bitmaptextEffect = value;
                else if (ItemType == PlayWindowItemType.Bitmap)
                    bitmapEffect = value;
                else
                    effect = value;
            }
        }
        public int Speed
        {
            get
            {
                if (ItemType == PlayWindowItemType.BitmapText)
                    return bitmaptextSpeed;
                else if (ItemType == PlayWindowItemType.Bitmap)
                    return bitmapSpeed;
                else
                    return speed;
            }
            set
            {
                if (ItemType == PlayWindowItemType.BitmapText)
                    bitmaptextSpeed = value;
                else if (ItemType == PlayWindowItemType.Bitmap)
                    bitmapSpeed = value;
            }
        }
        public int Stay
        {
            get
            {
                if (ItemType == PlayWindowItemType.Animator)
                    return animationStay;
                else if (ItemType == PlayWindowItemType.Bitmap)
                    return bitmapStay;
                else if (ItemType == PlayWindowItemType.BitmapText)
                    return bitmaptextStay;
                else if (ItemType == PlayWindowItemType.Clock)
                    return clockStay;
                else if (ItemType == PlayWindowItemType.Temperature)
                    return temperatureStay;
                else
                    return stay;
            }
            set
            {
                if (ItemType == PlayWindowItemType.Animator)
                    animationStay = value;
                else if (ItemType == PlayWindowItemType.Bitmap)
                    bitmapStay = value;
                else if (ItemType == PlayWindowItemType.BitmapText)
                    bitmaptextStay = value;
                else if (ItemType == PlayWindowItemType.Clock)
                    clockStay = value;
                else if (ItemType == PlayWindowItemType.Temperature)
                    temperatureStay = value;
            }
        }
        public List<Bitmap> BitmapsList
        {
            get { return bitmapsList; }
            set 
            {
                if (value == null)
                    bitmapsList = value;
                else 
                {
                    if(bitmapsList != null)
                        bitmapsList.Clear();

                    bitmapsList = new List<Bitmap>();

                    //bitmapsList = value;
                    foreach (Bitmap b in value)
                    {
                        bitmapsList.Add(new Bitmap(b));    
                    }
                }
            }
        }

        //text properties
        public int FontSize
        {
            get
            {
                if (ItemType == PlayWindowItemType.BitmapText)
                    return bitmaptextFontSize;
                else if (ItemType == PlayWindowItemType.Clock)
                    return clockFontSize;
                else if (ItemType == PlayWindowItemType.Temperature)
                    return temperatureFontSize;
                else
                    return fontSize;
            }
            set
            {
                if (ItemType == PlayWindowItemType.BitmapText)
                    bitmaptextFontSize = value;
                else if (ItemType == PlayWindowItemType.Clock)
                    clockFontSize = value;
                else if (ItemType == PlayWindowItemType.Temperature)
                    temperatureFontSize = value;
                else
                    fontSize = value;
            }
        }
        public UInt32 Color
        {
            get { return color; }
            set { color = value; }
        }
        //bmp properties
        public int Mode
        {
            get
            {
                if (ItemType == PlayWindowItemType.Animator)
                    return animationMode;
                else if (ItemType == PlayWindowItemType.Bitmap)
                    return bitmapMode;
                else
                    return mode;
            }
            set
            {
                if (ItemType == PlayWindowItemType.Animator)
                    animationMode = value;
                else if (ItemType == PlayWindowItemType.Bitmap)
                    bitmapMode = value;
                else
                    mode = value;
            }
        }
        public int Compress
        {
            get { return compress; }
            set { compress = value; }
        }
        //bmptext properties
        public string FontName
        {
            get { return fontname; }
            set { fontname = value; }
        }
        public string FontColor
        {
            get
            {
                if (ItemType == PlayWindowItemType.BitmapText)
                    return bitmaptextFontColor;
                else if (ItemType == PlayWindowItemType.Clock)
                    return clockFontColor;
                else if (ItemType == PlayWindowItemType.Temperature)
                    return temperatureFontColor;
                else
                    return fontcolor;
            }
            set
            {
                if (ItemType == PlayWindowItemType.BitmapText)
                    bitmaptextFontColor = value;
                else if (ItemType == PlayWindowItemType.Clock)
                    clockFontColor = value;
                else if (ItemType == PlayWindowItemType.Temperature)
                    temperatureFontColor = value;
                else
                    fontcolor = value;
            }
        }
        public string BgColor
        {
            get { return bgcolor; }
            set { bgcolor = value; }
        }
        public string FilePath
        {
            get { return filepath; }
            set { filepath = value; }
        }
        public string Rtf
        {
            get { return rtf; }
            set { rtf = value; }
        }


        public List<Bitmap> BitmapsForScreen
        {
            get { return bitmapsForScreen; }
            set
            {
                if (value == null)
                    bitmapsForScreen = value;
                else
                {
                    if (bitmapsForScreen != null)
                        bitmapsForScreen.Clear();

                    bitmapsForScreen = new List<Bitmap>();

                    foreach (Bitmap b in value)
                    {
                        bitmapsForScreen.Add(new Bitmap(b));
                    }
                }
            }
        }
        // clock properties
        public Boolean[] Attributes
        {        
            get {return attributes; }
            set { attributes = value; }
        }
        //temperature properties
        public byte TempAtt
        {
            get { return tempatt; }
            set { tempatt = value; }
        }

        #endregion

        #region Methods
        public PlayWindowItem()
        {
            ItemType = PlayWindowItemType.BitmapText;
        }

        public PlayWindowItem(PlayWindowItemType type)
        {
            ItemType = type;
        }

        //constructor for text
        public PlayWindowItem(string text, int fontSize, UInt32 color, int effect, int speed, int stay)
        {
            ItemType = PlayWindowItemType.Text;
            Text = text;
            FontSize = fontSize;
            Color = color;
            Effect = effect;
            Speed = speed;
            Stay = stay;
            Offset = new Point(0, 0);
        }
        
        //constructor for bmp
        public PlayWindowItem(int mode, int compress, string filePath, int effect, int speed, int stay)
        {
            ItemType = PlayWindowItemType.Bitmap;
            Text = filePath;
            Mode = mode;
            Compress = compress;
            Effect = effect;
            Speed = speed;
            Stay = stay;
            Offset = new Point(0, 0);
        }

        //constructor for text bitmap
        public PlayWindowItem(string text, int fontSize, UInt32 color,int effect,int speed, int stay, int mode, int compress)
        {
            ItemType = PlayWindowItemType.BitmapText;
            Text = text;
            Effect = effect;
            Speed = speed;
            Stay = stay;
            Mode = 0;
            Compress = 0;
            Offset = new Point(0, 0);
        }

        public PlayWindowItem(System.Collections.Hashtable dane)
        {
            ItemType = PlayWindowItemType.BitmapText;
            FontSize = int.Parse(dane["BitmapTextFontSize"].ToString());
            BgColor = dane["BitmapTextBackColor"].ToString();

            bitmaptextEffect = int.Parse(dane["BitmapTextEffect"].ToString());
            bitmapEffect = int.Parse(dane["BitmapEffect"].ToString());

            FontName = dane["BitmapTextFontName"].ToString();

            bitmaptextStay = int.Parse(dane["BitmapTextStay"].ToString());
            bitmaptextSpeed = int.Parse(dane["BitmapTextSpeed"].ToString());
            bitmaptextFontSize = int.Parse(dane["BitmapTextFontSize"].ToString());

            bitmapMode = int.Parse(dane["BitmapMode"].ToString());
            bitmapSpeed = int.Parse(dane["BitmapSpeed"].ToString());
            bitmapStay = int.Parse(dane["BitmapStay"].ToString());

            animationStay = int.Parse(dane["AnimationStay"].ToString());
            animationMode = int.Parse(dane["AnimationMode"].ToString());

            attributes[0] = bool.Parse(dane["ClockYear"].ToString());
            attributes[1] = bool.Parse(dane["ClockMonth"].ToString());
            attributes[2] = bool.Parse(dane["ClockDay"].ToString());
            attributes[3] = bool.Parse(dane["ClockHour"].ToString());
            attributes[4] = bool.Parse(dane["ClockMinutes"].ToString());
            attributes[5] = bool.Parse(dane["ClockSeconds"].ToString());
            attributes[6] = bool.Parse(dane["ClockDayOfWeek"].ToString());
            attributes[7] = bool.Parse(dane["ClockHands"].ToString());
            attributes[8] = bool.Parse(dane["ClockShortDay"].ToString());
            attributes[9] = bool.Parse(dane["ClockShortYear"].ToString());
            attributes[10] = bool.Parse(dane["ClockMultiline"].ToString());
            attributes[14] = bool.Parse(dane["ClockMarks"].ToString());

            clockFontSize = int.Parse(dane["ClockFontSize"].ToString());
            clockStay = int.Parse(dane["ClockStay"].ToString());

            temperatureStay = int.Parse(dane["TemperatureStay"].ToString());
            temperatureFontSize = int.Parse(dane["TemperatureFontSize"].ToString());

            tempatt = byte.Parse(dane["TemperatureCelcius"].ToString());

            clockFontColor = dane["ClockFontColor"].ToString();
            temperatureFontColor = dane["TemperatureFontColor"].ToString();
            bitmaptextFontColor = dane["BitmapTextFontColor"].ToString();

        }

        #endregion
    }
}
