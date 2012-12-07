using System;
using System.Collections.Generic;

using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace BengiLED_for_C_Power
{
    public class PreviewAnimation
    {
        #region Private fields
        private PlayWindowItem playbillItem;
        private List<Bitmap> inputBitmaps;
        private EffectType effect;
        private short stayTime;
        private short speed;
        private WindowOnPreview parentWindow;
        private System.Windows.Forms.Timer UpdateTimer;
        private bool stayNow = false;
        public int currentBitmap = 0;
        
        private int textBitmapHeight = 0;
        private int currentX = 0, currentY = 0, borderX = 0, borderY = 0, ratio = 1,  modulo = 0;
        private int plusX = 0, plusY = 0,  slidegcd = 0, slideH = 1, slideW = 1, slideDif = 0, slideStep = 0;
        private int shutterStep = 0, chessboardStep = 0;
        private float borderXf = 0, borderYf = 0, ratiof = 1;
        private Point center, startPoint;
        private Rectangle oldRect, newRect;
        private bool firstSnowInNewLine = false;
        private int blinkTimes = 4;
        private double oldAngle = 0, newAngle = 0, angleStep = 0;
        private short clockDirection = +1; 
        private Color maskColor = Color.FromArgb(255, 255, 255);
        private double clockRadius = 0;
        private int roundedClockRadius = 0;
        private Bitmap tempBitmap = null;
        private int scrollTimes = -1;
        private bool drawToMemory = true;
        private List<Bitmap> memoryBitmaps = null;
        private Bitmap staticImage = null;
        #endregion

        public bool lastFrame = false;
        public bool running = false;
        public bool animateNext = false;
        public bool start = false;

        public Color[][][] staticBitmapPixels;
        public Color[][] frameBitmapPixels;
        public List<ColorAtPixel> changedPixels = null;
        public Size frameSize;

        private DateTime animationStart, animationEnd;


        #region Properties
        public PlayWindowItem PlaybillItem
        {
            get { return playbillItem; }
            set { playbillItem = value; }
        }
        public int TextBitmapHeight 
        {
            get { return textBitmapHeight; }
            set { textBitmapHeight = value; }
        }
        public List<Bitmap> InputBitmaps
        {
            get { return inputBitmaps; }
            set 
            {
                if (parentWindow.animationTimer != null)
                {
                    parentWindow.animationTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                    parentWindow.animationTimer.Dispose();
                    parentWindow.animationTimer = null;
                }

                if (value == null || value.Contains(null))
                    inputBitmaps = null;
                else
                {
                    Cursor currentPreviewCursor = parentWindow.Owner.Cursor;
                    Cursor currentMainCursor = ((MainWindow)parentWindow.Owner.Owner).Cursor;
                    ((MainWindow)parentWindow.Owner.Owner).Cursor = parentWindow.Owner.Cursor = Cursors.WaitCursor;

                    //ClearLEDs(null);
                    if (inputBitmaps != null)
                        Clear();


                    staticBitmapPixels = new Color[value.Count][][];

                    inputBitmaps = new List<Bitmap>();
                    TextBitmapHeight = (value[0].Height < parentWindow.Height / (PreviewWindow.LedDiam + PreviewWindow.Space)) ? value[0].Height : parentWindow.Height / (PreviewWindow.LedDiam + PreviewWindow.Space);
                    for (int i = 0; i < value.Count; i++)
                    {
                        Point tmppt = new Point(0, 0);
                        if (PlaybillItem.ItemType != PlayWindowItemType.BitmapText)
                            PlaybillItem.Offset = new Point(0, 0);

                        tmppt = new Point(PlaybillItem.Offset.X, PlaybillItem.Offset.Y);
                        PlaybillItem.Offset = new Point(0, 0);


                        inputBitmaps.Add(new Bitmap(parentWindow.Width / (PreviewWindow.LedDiam + PreviewWindow.Space),
                                parentWindow.Height
                                / (PreviewWindow.LedDiam + PreviewWindow.Space)
                                , System.Drawing.Imaging.PixelFormat.Format32bppArgb));

                        if (PlaybillItem.ItemType == PlayWindowItemType.BitmapText)
                        {
                            //fontColors = new List<ColorAtPixel>();

                            using (Bitmap backgroundYBitmap = new Bitmap(value[i], value[i].Width, 1), 
                                backgroundXBitmap = new Bitmap(1, inputBitmaps[i].Height))
                            {
                                using (Graphics backgroundYGraphics = Graphics.FromImage(backgroundYBitmap), 
                                    backgroundXGraphics = Graphics.FromImage(backgroundXBitmap))
                                {
                                    using (Graphics tmpg = Graphics.FromImage(inputBitmaps[i]))
                                    {
                                        if(value[i].GetPixel(0, 0).ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                            backgroundXGraphics.Clear(value[i].GetPixel(0, 0));
                                        else
                                            backgroundXGraphics.Clear(value[i].GetPixel(0, inputBitmaps[i].Height-1));


                                        tmpg.DrawImageUnscaledAndClipped(backgroundYBitmap, new Rectangle(PlaybillItem.Offset.X, 0, value[i].Width, inputBitmaps[i].Height));
                                        tmpg.DrawImage(backgroundXBitmap, new Rectangle(0, 0, PlaybillItem.Offset.X, inputBitmaps[i].Height));

                                        backgroundXGraphics.Clear(value[i].GetPixel(value[i].Width - 1, value[i].Height - 1));
                                        tmpg.DrawImage(backgroundXBitmap, new Rectangle(value[i].Width + PlaybillItem.Offset.X, 0, inputBitmaps[i].Width, inputBitmaps[i].Height));

                                        tmpg.DrawImageUnscaledAndClipped(value[i], new Rectangle(PlaybillItem.Offset.X, PlaybillItem.Offset.Y, value[i].Width, value[i].Height));

                                        int currentBitmapHeight = (value[i].Height < parentWindow.Height / (PreviewWindow.LedDiam + PreviewWindow.Space)) ? value[i].Height : parentWindow.Height / (PreviewWindow.LedDiam + PreviewWindow.Space);
                                        if (currentBitmapHeight < parentWindow.Height / (PreviewWindow.LedDiam + PreviewWindow.Space))
                                            tmpg.DrawImageUnscaledAndClipped(backgroundYBitmap, new Rectangle(PlaybillItem.Offset.X, PlaybillItem.Offset.Y + currentBitmapHeight - 1, value[i].Width, 1));
                                    }
                                }
                            }

                            PlaybillItem.Offset = new Point(tmppt.X, tmppt.Y);
                        }
                        else if (PlaybillItem.ItemType == PlayWindowItemType.Bitmap || PlaybillItem.ItemType == PlayWindowItemType.Animator
                             || PlaybillItem.ItemType == PlayWindowItemType.Temperature || PlaybillItem.ItemType == PlayWindowItemType.Clock)
                        {
                            using ( Graphics tmpg = Graphics.FromImage(inputBitmaps[i]))
                            {
                                int widthDiff = 0, heightDiff = 0;
                                
                                tmpg.Clear(PreviewWindow.OffLEDColor);

                                if (PlaybillItem.ItemType == PlayWindowItemType.Temperature || PlaybillItem.ItemType == PlayWindowItemType.Clock)
                                    PlaybillItem.Mode = 0;

                                switch (PlaybillItem.Mode)
                                {
                                    case 0: //center
                                        widthDiff = (inputBitmaps[i].Width - value[i].Width) / 2;
                                        heightDiff = (inputBitmaps[i].Height - value[i].Height) / 2;
                                        tmpg.DrawImage(value[i], widthDiff, heightDiff);
                                        break;
                                    case 1: //zoom
                                        double ratio = 1.0;
                                        int newWidth = 0, newHeight = 0;

                                        widthDiff = (inputBitmaps[i].Width - value[i].Width) / 2;
                                        heightDiff = (inputBitmaps[i].Height - value[i].Height) / 2;


                                        if ((inputBitmaps[i].Width / (double)value[i].Width) <= (inputBitmaps[i].Height / (double)value[i].Height))
                                            ratio = (inputBitmaps[i].Width / (double)value[i].Width);
                                        else
                                            ratio = (inputBitmaps[i].Height / (double)value[i].Height);

                                        newWidth = (int)(ratio * value[i].Width);
                                        newHeight = (int)(ratio * value[i].Height);

                                        widthDiff = (inputBitmaps[i].Width - newWidth) / 2;
                                        heightDiff = (inputBitmaps[i].Height - newHeight) / 2;

                                        tmpg.DrawImage(value[i], new Rectangle(widthDiff, heightDiff, newWidth, newHeight));
                                        break;
                                    case 2: //stretch
                                        tmpg.DrawImage(value[i], new Rectangle(0, 0, inputBitmaps[i].Width, inputBitmaps[i].Height));
                                        break;
                                }
                            }
                        }



                        //inputBitmaps[i].LockBits();
                        
                        staticBitmapPixels[i] = new Color[inputBitmaps[i].Width][];
                        //frameBitmapPixels = new Color[inputBitmaps[i].Width][];

                        for(int x = 0; x < inputBitmaps[i].Width; x++)
                        {
                            staticBitmapPixels[i][x] = new Color[inputBitmaps[i].Height];
                            //frameBitmapPixels[x] = new Color[inputBitmaps[i].Height];
                        }

                        for (int x = 0; x < inputBitmaps[i].Width; x++)
                        {
                            for (int y = 0; y < inputBitmaps[i].Height; y++)
                            {
                                Color currentPixel =  parentWindow.Owner.ApplyColorType(inputBitmaps[i].GetPixel(x, y));
                                staticBitmapPixels[i][x][y] = currentPixel;
                                //if (staticBitmapPixels[i][x][y].ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                //    MessageBox.Show(staticBitmapPixels[i][x][y].Name);
                                //frameBitmapPixels[x][y] = currentPixel;
                            }
                        }
                    }

                    frameSize = new Size(inputBitmaps[0].Width, inputBitmaps[0].Height);
                    parentWindow.Owner.Cursor = currentPreviewCursor;
                    ((MainWindow)parentWindow.Owner.Owner).Cursor = currentMainCursor;
                }
            }
        }

        public EffectType Effect
        {
            get { return effect; }
            set 
            {
                try
                {
                    if (Enum.IsDefined(typeof(EffectType), value))
                        effect = value;
                    else
                        effect = EffectType.Instant;
                }
                catch
                {
                    effect = EffectType.Instant;
                }
            }
        }

        public short Speed
        {
            get { return speed; }
            set { speed = value; }
        }

        public short StayTime
        {
            get { return stayTime; }
            set { stayTime = value; }
        }
        #endregion
                
        #region Methods
        public PreviewAnimation(WindowOnPreview parent, PlayWindowItem item, List<Bitmap> bitmaps, EffectType e, short effectSpeed, short stay)
        {
            parentWindow = parent;
            PlaybillItem = item;
            Effect = e; 
            InputBitmaps = bitmaps;
                        
            Speed = effectSpeed;
            StayTime = stay;
        }

        ~PreviewAnimation()
        {
            StopAnimation();
            if (InputBitmaps != null)
            {
                foreach (Bitmap b in InputBitmaps)
                    b.Dispose();
            }
        }

        public void Clear()
        {
            StopAnimation();
            if (InputBitmaps != null)
            {
                foreach (Bitmap b in InputBitmaps)
                    b.Dispose();

                InputBitmaps.Clear();
            }
            InputBitmaps = null;

            if (staticImage != null)
                staticImage.Dispose();
        }

        public void StopAnimation()
        {
            if (UpdateTimer != null)
            {
                UpdateTimer.Stop();
                UpdateTimer = null;
            }
            currentBitmap = 0;
            running = false;
        }


        public void PauseAnimation()
        {
            if (UpdateTimer != null)
            {
                UpdateTimer.Stop();
            }
        }

        public void UnpauseAnimation()
        {
            if (UpdateTimer != null)
            {
                UpdateTimer.Start();
            }
        }



        public void ShowStatic()
        {
            //using(Graphics wndGr = Graphics.FromImage(parentWindow.windowBitmap))
            {
           //     wndGr.DrawImageUnscaled(staticImage, 0,0);
            }
            //if (false)
            {
                ClearLEDs(null);
                if (InputBitmaps != null && InputBitmaps.Count > 0)
                {
                    //for (int x = 0; x < parentWindow.windowBitmap.Width; x++)
                    //{
                    //    for (int y = 0; y < parentWindow.windowBitmap.Height; y++)
                    //    {
                    //        Color c = staticBitmapPixels[currentBitmap][x][y];

                    //        DrawLED(x, y, c);
                    //    }
                    //}

                    //for (int x = 0; x < frameSize.Width; x++)
                    //{
                    //    for (int y = 0; y < frameSize.Height; y++)
                    //    {
                    //        Color c = staticBitmapPixels[currentBitmap][x][y];

                    //        DrawLED(x, y, c);
                    //    }
                    //}


                    try
                    {
                        int tmpX = (frameSize.Width > frameSize.Width)
                            ? frameSize.Width : frameSize.Width;
                        int tmpY = (frameSize.Height > frameSize.Height)
                            ? frameSize.Height : frameSize.Height;

                        for (int x = 0; x < tmpX; x++)
                        {
                            for (int y = 0; y < tmpY; y++)
                            {
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                                if (MainWindow.oldAnimation)
                                    DrawLED(x, y, c);
                                else
                                {
                                    //if (frameBitmapPixels[x][y].ToArgb() != c.ToArgb())
                                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                    {
                                        frameBitmapPixels[x][y] = c;
                                        changedPixels.Add(new ColorAtPixel(c, x, y));
                                    }
                                }
                            }
                        }

                    }
                    catch { /*System.Diagnostics.Debugger.Break();*/ }
                }
            }
        }

        private bool IsPointInPolygon(int npol, double[] xp, double[] yp, double x, double y)
        {
            int i, j;
            bool c = false;
            for (i = 0, j = npol - 1; i < npol; j = i++)
            {
                if ((((yp[i] <= y) && (y < yp[j])) ||
                     ((yp[j] <= y) && (y < yp[i]))) &&
                    (x < (xp[j] - xp[i]) * (y - yp[i]) / (yp[j] - yp[i]) + xp[i]))
                    c = !c;
            }
            return c;
        }
    

        public void Animate(bool animateAll)
        {
            if(parentWindow.Owner.Visible)
            {
                animationStart = DateTime.Now;

                running = true;
                animateNext = animateAll;
                if (PlaybillItem.ItemType == PlayWindowItemType.Animator || PlaybillItem.ItemType == PlayWindowItemType.Clock
                    || PlaybillItem.ItemType == PlayWindowItemType.Temperature)
                {
                    Effect = EffectType.Instant;

                    if (PlaybillItem.ItemType == PlayWindowItemType.Animator && scrollTimes < 0)
                    {
                        scrollTimes = PlaybillItem.Stay-1;//StayTime;
                    }
                }
                if ((Effect == EffectType.ScrollLeftCont || Effect == EffectType.ScrollRightCont
                     || Effect == EffectType.ScrollLeft || Effect == EffectType.ScrollRight
                      || Effect == EffectType.ScrollUpCont || Effect == EffectType.ScrollDownCont
                       || Effect == EffectType.ScrollUp || Effect == EffectType.ScrollDown) && scrollTimes < 0)
                    scrollTimes = StayTime-1;

                // reseting frame
                changedPixels = new List<ColorAtPixel>();
                frameBitmapPixels = new Color[frameSize.Width][];

                for (int x = 0; x < frameSize.Width; x++)
                    frameBitmapPixels[x] = new Color[frameSize.Height];

                for (int x = 0; x < frameSize.Width; x++)
                {
                    for (int y = 0; y < frameSize.Height; y++)
                    {
                        //frameBitmapPixels[x][y] = staticBitmapPixels[currentBitmap][x][y];

                        changedPixels.Add(new ColorAtPixel(frameBitmapPixels[x][y], x, y));
                    }
                }

                parentWindow.testbmp = new Bitmap(frameSize.Width, frameSize.Height);
                using (Graphics testgr = Graphics.FromImage(parentWindow.testbmp))
                {
                    testgr.Clear(PreviewWindow.OffLEDColor);
                }

                SetupTimer();
                SetupVariables();
            }
        }

        private void SetupVariables()
        {

            //ClearLEDs(null);
            try
            {
                switch (Effect)
                {
                    #region Static
                    case EffectType.Instant:
                        ClearLEDs(null);
                        break;
                    case EffectType.Blink:
                        ClearLEDs(null);
                        blinkTimes = 9;
                        break;
                    case EffectType.BlinkInverse:
                        ClearLEDs(null);
                        blinkTimes = 9;
                        break;
                    #endregion
                    #region Open simple
                    case EffectType.OpenLeft:
                        ClearLEDs(null);
                        currentX = 0;
                        break;
                    case EffectType.OpenRight:
                        ClearLEDs(null);
                        currentX = frameSize.Width - 1;
                        break;
                    case EffectType.OpenLeftRight:
                        ClearLEDs(null);
                        currentX = 0;
                        break;
                    case EffectType.OpenUpDown:
                        ClearLEDs(null);
                        currentY = 0;
                        break;
                    case EffectType.LaserDown:
                        ClearLEDs(null);
                        currentY = 0;
                        break;
                    case EffectType.LaserUp:
                        ClearLEDs(null);
                        currentY = 0;
                        break;
                    case EffectType.BlindHorizontial:
                        ClearLEDs(null);
                        currentX = (frameSize.Width >= 8) ? 7 :frameSize.Width - 1;
                        break;
                    case EffectType.ShutterVertical:
                        ClearLEDs(null);
                        currentX = 0;

                        if (frameSize.Width < 8)
                        {
                            shutterStep = frameSize.Width;
                        }
                        else if (frameSize.Width < 24)
                        {
                            shutterStep = frameSize.Width / 2;
                        }
                        else if (frameSize.Width < 32)
                        {
                            shutterStep = 8;
                        }
                        else if (frameSize.Width < 64)
                        {
                            shutterStep = 16;
                        }
                        else if (frameSize.Width >= 64)
                        {
                            shutterStep = 32;
                        }

                        break;
                    case EffectType.ShutterHorizontal:
                        ClearLEDs(null);
                        currentY = 0;

                        if (frameSize.Height < 8)
                        {
                            shutterStep = frameSize.Height;
                        }
                        else if (frameSize.Height < 24)
                        {
                            shutterStep = frameSize.Height / 2;
                        }
                        else if (frameSize.Height < 32)
                        {
                            shutterStep = 8;
                        }
                        else if (frameSize.Height < 64)
                        {
                            shutterStep = 16;
                        }
                        else if (frameSize.Height >= 64)
                        {
                            shutterStep = 32;
                        }

                        break;
                    case EffectType.ChessboardHorizontal:
                        ClearLEDs(null);
                        currentX = 0;
                        chessboardStep = frameSize.Width / 8;
                        break;
                    case EffectType.OpenHorizontal:
                        ClearLEDs(null);
                        center = new Point(frameSize.Width / 2,
                            frameSize.Height / 2);
                        plusX = 0;
                        break;
                    case EffectType.OpenVertical:
                        ClearLEDs(null);
                        center = new Point(frameSize.Width / 2,
                            frameSize.Height / 2);
                        plusY = 0;
                        break;
                    case EffectType.OpenTopLeft:
                        ClearLEDs(null);
                        center = new Point(frameSize.Width / 2,
                            frameSize.Height / 2);

                        if (frameSize.Width > frameSize.Height)
                        {
                            modulo = frameSize.Width % frameSize.Height;

                            if (modulo > 0)
                            {
                                int tmp = frameSize.Height - modulo;
                                ratiof = frameSize.Width / (float)tmp;
                            }
                            else
                                ratiof = frameSize.Width / (float)frameSize.Height;

                            plusY = 1;
                            plusX = 1;
                            borderXf = ratiof * plusY;

                            if (modulo > 0)
                            {
                                // borderXf++;
                            }

                        }
                        else if ( frameSize.Height >= frameSize.Width)
                        {

                            if (modulo > 0)
                            {
                                int tmp = frameSize.Width - modulo;
                                ratiof = frameSize.Height / (float)tmp;
                            }
                            else
                                ratiof = frameSize.Height / (float)frameSize.Width;
                            plusX = 1;
                            plusY = 1;
                            borderYf = ratiof * plusX;

                            modulo = frameSize.Height % frameSize.Width;
                            if (modulo > 0)
                            {
                                // borderYf++;
                            }
                        }
                        //oldRect = Rectangle.FromLTRB(center.X, center.Y, center.X, center.Y);
                        break;
                    case EffectType.OpenTopRight:
                        ClearLEDs(null);
                        center = new Point(frameSize.Width / 2,
                            frameSize.Height / 2);

                        if (frameSize.Width > frameSize.Height)
                        {
                            modulo = frameSize.Width % frameSize.Height;

                            if (modulo > 0)
                            {
                                int tmp = frameSize.Height - modulo;
                                ratiof = frameSize.Width / (float)tmp;
                            }
                            else
                                ratiof = frameSize.Width / (float)frameSize.Height;

                            plusY = 1;
                            plusX = 1;
                            borderXf = ratiof * plusY;

                            if (modulo > 0)
                            {
                                // borderXf++;
                            }

                        }
                        else if ( frameSize.Height >= frameSize.Width)
                        {

                            if (modulo > 0)
                            {
                                int tmp = frameSize.Width - modulo;
                                ratiof = frameSize.Height / (float)tmp;
                            }
                            else
                                ratiof = frameSize.Height / (float)frameSize.Width;
                            plusX = 1;
                            plusY = 1;
                            borderYf = ratiof * plusX;

                            modulo = frameSize.Height % frameSize.Width;
                            if (modulo > 0)
                            {
                                // borderYf++;
                            }
                        }
                        //oldRect = Rectangle.FromLTRB(center.X, center.Y, center.X, center.Y);
                        break;
                    case EffectType.OpenBottomLeft:
                        ClearLEDs(null);
                        center = new Point(frameSize.Width / 2,
                            frameSize.Height / 2);

                        if (frameSize.Width > frameSize.Height)
                        {
                            modulo = frameSize.Width % frameSize.Height;

                            if (modulo > 0)
                            {
                                int tmp = frameSize.Height - modulo;
                                ratiof = frameSize.Width / (float)tmp;
                            }
                            else
                                ratiof = frameSize.Width / (float)frameSize.Height;

                            plusY = 1;
                            plusX = 1;
                            borderXf = ratiof * plusY;

                            if (modulo > 0)
                            {
                                // borderXf++;
                            }

                        }
                        else if ( frameSize.Height >= frameSize.Width)
                        {

                            if (modulo > 0)
                            {
                                int tmp = frameSize.Width - modulo;
                                ratiof = frameSize.Height / (float)tmp;
                            }
                            else
                                ratiof = frameSize.Height / (float)frameSize.Width;
                            plusX = 1;
                            plusY = 1;
                            borderYf = ratiof * plusX;

                            modulo = frameSize.Height % frameSize.Width;
                            if (modulo > 0)
                            {
                                // borderYf++;
                            }
                        }
                        //oldRect = Rectangle.FromLTRB(center.X, center.Y, center.X, center.Y);
                        break;
                    case EffectType.OpenBottomRight:
                        ClearLEDs(null);
                        center = new Point(frameSize.Width / 2,
                            frameSize.Height / 2);

                        if (frameSize.Width > frameSize.Height)
                        {
                            modulo = frameSize.Width % frameSize.Height;

                            if (modulo > 0)
                            {
                                int tmp = frameSize.Height - modulo;
                                ratiof = frameSize.Width / (float)tmp;
                            }
                            else
                                ratiof = frameSize.Width / (float)frameSize.Height;

                            plusY = 1;
                            plusX = 1;
                            borderXf = ratiof * plusY;

                            if (modulo > 0)
                            {
                                // borderXf++;
                            }

                        }
                        else if ( frameSize.Height >= frameSize.Width)
                        {

                            if (modulo > 0)
                            {
                                int tmp = frameSize.Width - modulo;
                                ratiof = frameSize.Height / (float)tmp;
                            }
                            else
                                ratiof = frameSize.Height / (float)frameSize.Width;
                            plusX = 1;
                            plusY = 1;
                            borderYf = ratiof * plusX;

                            modulo = frameSize.Height % frameSize.Width;
                            if (modulo > 0)
                            {
                                // borderYf++;
                            }
                        }
                        //oldRect = Rectangle.FromLTRB(center.X, center.Y, center.X, center.Y);
                        break;
                    case EffectType.OpenSlash:
                        ClearLEDs(null);
                        center = new Point(frameSize.Width / 2,
                            frameSize.Height / 2);

                        if (frameSize.Width > frameSize.Height)
                        {
                            modulo = frameSize.Width % frameSize.Height;

                            if (modulo > 0)
                            {
                                int tmp = frameSize.Height - modulo;
                                ratiof = frameSize.Width / (float)tmp;
                            }
                            else
                                ratiof = frameSize.Width / (float)frameSize.Height;

                            plusY = 1;
                            plusX = 1;
                            borderXf = ratiof * plusY;

                            if (modulo > 0)
                            {
                                // borderXf++;
                            }

                        }
                        else if ( frameSize.Height >= frameSize.Width)
                        {

                            if (modulo > 0)
                            {
                                int tmp = frameSize.Width - modulo;
                                ratiof = frameSize.Height / (float)tmp;
                            }
                            else
                                ratiof = frameSize.Height / (float)frameSize.Width;
                            plusX = 1;
                            plusY = 1;
                            borderYf = ratiof * plusX;

                            modulo = frameSize.Height % frameSize.Width;
                            if (modulo > 0)
                            {
                                // borderYf++;
                            }
                        }
                        //oldRect = Rectangle.FromLTRB(center.X, center.Y, center.X, center.Y);
                        break;
                    case EffectType.OpenBackslash:
                        ClearLEDs(null);
                        center = new Point(frameSize.Width / 2,
                            frameSize.Height / 2);

                        if (frameSize.Width > frameSize.Height)
                        {
                            modulo = frameSize.Width % frameSize.Height;

                            if (modulo > 0)
                            {
                                int tmp = frameSize.Height - modulo;
                                ratiof = frameSize.Width / (float)tmp;
                            }
                            else
                                ratiof = frameSize.Width / (float)frameSize.Height;

                            plusY = 1;
                            plusX = 1;
                            borderXf = ratiof * plusY;

                            if (modulo > 0)
                            {
                                // borderXf++;
                            }

                        }
                        else if ( frameSize.Height >= frameSize.Width)
                        {

                            if (modulo > 0)
                            {
                                int tmp = frameSize.Width - modulo;
                                ratiof = frameSize.Height / (float)tmp;
                            }
                            else
                                ratiof = frameSize.Height / (float)frameSize.Width;
                            plusX = 1;
                            plusY = 1;
                            borderYf = ratiof * plusX;

                            modulo = frameSize.Height % frameSize.Width;
                            if (modulo > 0)
                            {
                                // borderYf++;
                            }
                        }
                        //oldRect = Rectangle.FromLTRB(center.X, center.Y, center.X, center.Y);
                        break;
                    #endregion
                    #region Shift and scroll
                    case EffectType.ShiftLeft:
                        if (currentBitmap == 0)
                            ClearLEDs(null);
                        currentX = frameSize.Width - 1;
                        break;
                    case EffectType.ShiftRight:
                        if (currentBitmap == 0)
                            ClearLEDs(null);
                        currentX = 0;
                        break;
                    case EffectType.ShiftUp:
                        if (currentBitmap == 0)
                            ClearLEDs(null);
                        currentY = frameSize.Height - 1;
                        break;
                    case EffectType.ShiftDown:
                        if (currentBitmap == 0)
                            ClearLEDs(null);
                        currentY = 0;
                        break;
                    case EffectType.ScrollUp:
                        //if (currentBitmap == 0 && scrollTimes <= 0)
                        //    ClearLEDs(null);
                        currentY = frameSize.Height - 1;
                        currentBitmap = 0;
                        break;
                    case EffectType.ScrollDown:
                        //if (currentBitmap == 0 && scrollTimes <= 0)
                        //    ClearLEDs(null);
                        currentY = 0;
                        currentBitmap = 0;//InputBitmaps.Count - 1;
                        break;
                    case EffectType.ScrollUpCont:
                        //if (currentBitmap == 0 && scrollTimes <= 0)
                        //    ClearLEDs(null);
                        currentY = frameSize.Height - 1;
                        currentBitmap = 0;
                        break;
                    case EffectType.ScrollDownCont:
                        //if (currentBitmap == 0 && scrollTimes <= 0)
                        //    ClearLEDs(null);
                        currentY = 0;
                        currentBitmap = 0;//InputBitmaps.Count - 1;
                        break;
                    case EffectType.ScrollLeftCont:
                        //if (currentBitmap == 0 && scrollTimes <= 0)
                        //    ClearLEDs(null);
                        currentX = frameSize.Width - 1;
                        currentBitmap = 0;
                        break;
                    case EffectType.ScrollRightCont:
                        //if (currentBitmap == 0 && scrollTimes <= 0)
                        //    ClearLEDs(null);
                        currentX = 0;//frameSize.Width - 1;
                        currentBitmap = InputBitmaps.Count-1;
                        break;
                    case EffectType.ScrollLeft:
                        //if (currentBitmap == 0 && scrollTimes <= 0)
                        //    ClearLEDs(null);
                        currentX = frameSize.Width - 1;
                        currentBitmap = 0;
                        break;
                    case EffectType.ScrollRight:
                        //if (currentBitmap == 0 && scrollTimes <= 0)
                        //    ClearLEDs(null);
                        currentX = 0;//frameSize.Width - 1;
                        currentBitmap = InputBitmaps.Count - 1;
                        break;
                    #endregion
                    #region Clock
                    case EffectType.OpenClockwise:
                        ClearLEDs(null);
                        clockRadius = Math.Pow((Math.Pow(frameSize.Width / 2, 2)
                            + Math.Pow(frameSize.Height / 2, 2)), 0.5);
                        roundedClockRadius = Convert.ToInt32(Math.Round(clockRadius + 1, 0));
                        angleStep = 0;
                        clockDirection = +1;
                        break;
                    case EffectType.OpenAntiClockwise:
                        ClearLEDs(null);
                        clockRadius = Math.Pow((Math.Pow(frameSize.Width / 2, 2)
                            + Math.Pow(frameSize.Height / 2, 2)), 0.5);
                        roundedClockRadius = Convert.ToInt32(Math.Round(clockRadius + 1, 0));
                        angleStep = 0;
                        clockDirection = -1;
                        break;
                    case EffectType.WindmillClockwise:
                        ClearLEDs(null);
                        clockRadius = Math.Pow((Math.Pow(frameSize.Width / 2, 2)
                            + Math.Pow(frameSize.Height / 2, 2)), 0.5);
                        roundedClockRadius = Convert.ToInt32(Math.Round(clockRadius + 1, 0));
                        angleStep = 0;
                        clockDirection = +1;
                        break;
                    case EffectType.WindmillAntiClockwise:
                        ClearLEDs(null);
                        clockRadius = Math.Pow((Math.Pow(frameSize.Width / 2, 2)
                            + Math.Pow(frameSize.Height / 2, 2)), 0.5);
                        roundedClockRadius = Convert.ToInt32(Math.Round(clockRadius + 1, 0));
                        angleStep = 0;
                        clockDirection = -1;
                        break;
                    case EffectType.Open2Fan:
                        ClearLEDs(null);
                        clockRadius = Math.Pow((Math.Pow(frameSize.Width / 2, 2)
                            + Math.Pow(frameSize.Height / 2, 2)), 0.5);
                        roundedClockRadius = Convert.ToInt32(Math.Round(clockRadius + 1, 0));
                        angleStep = 0;
                        clockDirection = +1;
                        break;
                    #endregion
                    #region From center
                    case EffectType.RectangleOut:
                        ClearLEDs(null);
                        center = new Point(frameSize.Width / 2,
                            frameSize.Height / 2);

                        if (frameSize.Width > frameSize.Height)
                        {
                            modulo = frameSize.Width % frameSize.Height;
                            if (modulo > 0)
                            {
                                int tmp = frameSize.Height - modulo;
                                ratio = frameSize.Width / tmp;
                            }
                            else
                                ratio = frameSize.Width / frameSize.Height;
                            
                            
                            plusY = 1;
                            plusX = 1;
                            borderX = ratio * plusY;
                        }
                        else if ( frameSize.Height >= frameSize.Width)
                        {
                            modulo = frameSize.Width % frameSize.Height;
                            if (modulo > 0)
                            {
                                int tmp = frameSize.Width - modulo;
                                ratio = frameSize.Height / tmp;
                            }
                            else
                                ratio =  frameSize.Height / frameSize.Width;
                            
                            plusX = 1;
                            plusY = 1;
                            borderY = ratio * plusX;
                        }
                        oldRect = Rectangle.FromLTRB(center.X, center.Y, center.X, center.Y);
                        break;
                    case EffectType.RectangleIn:
                        ClearLEDs(null);
                        center = new Point(frameSize.Width / 2,
                            frameSize.Height / 2);

                        if (frameSize.Width > frameSize.Height)
                        {
                            modulo = frameSize.Width % frameSize.Height;
                            if (modulo > 0)
                            {
                                int tmp = frameSize.Height - modulo;
                                ratio = frameSize.Width / tmp;
                            }
                            else
                                ratio = frameSize.Width / frameSize.Height;


                            plusY = 1;
                            plusX = 1;
                            borderX = ratio * plusY;
                        }
                        else if ( frameSize.Height >= frameSize.Width)
                        {
                            modulo = frameSize.Width % frameSize.Height;
                            if (modulo > 0)
                            {
                                int tmp = frameSize.Width - modulo;
                                ratio = frameSize.Height / tmp;
                            }
                            else
                                ratio =  frameSize.Height / frameSize.Width;

                            plusX = 1;
                            plusY = 1;
                            borderY = ratio * plusX;
                        }
                        oldRect = Rectangle.FromLTRB(center.X, center.Y, center.X, center.Y);
                        break;
                    case EffectType.CornerOut:
                        ClearLEDs(null);
                        center = new Point(frameSize.Width / 2,
                            frameSize.Height / 2);

                        if (frameSize.Width > frameSize.Height)
                        {
                            modulo = frameSize.Width % frameSize.Height;

                            if (modulo > 0)
                            {
                                int tmp = frameSize.Height - modulo;
                                ratiof = frameSize.Width / (float)tmp;
                            }
                            else
                                ratiof = frameSize.Width / (float)frameSize.Height;

                            plusY = 1;
                            plusX = 1;
                            borderXf = ratiof * plusY;

                            if (modulo > 0)
                            {
                               // borderXf++;
                            }

                        }
                        else if ( frameSize.Height >= frameSize.Width)
                        {

                            if (modulo > 0)
                            {
                                int tmp = frameSize.Width - modulo;
                                ratiof = frameSize.Height / (float)tmp;
                            }
                            else
                                ratiof = frameSize.Height / (float)frameSize.Width;
                            plusX = 1;
                            plusY = 1;
                            borderYf = ratiof * plusX;

                            modulo = frameSize.Height % frameSize.Width;
                            if (modulo > 0)
                            {
                               // borderYf++;
                            }
                        }
                        //oldRect = Rectangle.FromLTRB(center.X, center.Y, center.X, center.Y);
                        break;
                    case EffectType.CornerIn:
                        ClearLEDs(null);
                        center = new Point(frameSize.Width / 2,
                            frameSize.Height / 2);

                        if (frameSize.Width > frameSize.Height)
                        {
                            modulo = frameSize.Width % frameSize.Height;

                            if (modulo > 0)
                            {
                                int tmp = frameSize.Height - modulo;
                                ratiof = frameSize.Width / (float)tmp;
                            }
                            else
                                ratiof = frameSize.Width / (float)frameSize.Height;

                            plusY = 1;
                            plusX = 1;
                            borderXf = ratiof * plusY;

                            if (modulo > 0)
                            {
                                // borderXf++;
                            }

                        }
                        else if ( frameSize.Height >= frameSize.Width)
                        {

                            if (modulo > 0)
                            {
                                int tmp = frameSize.Width - modulo;
                                ratiof = frameSize.Height / (float)tmp;
                            }
                            else
                                ratiof = frameSize.Height / (float)frameSize.Width;
                            plusX = 1;
                            plusY = 1;
                            borderYf = ratiof * plusX;

                            modulo = frameSize.Height % frameSize.Width;
                            if (modulo > 0)
                            {
                                // borderYf++;
                            }
                        }
                        break;
                    case EffectType.RoundOut:
                        ClearLEDs(null);
                        center = new Point(frameSize.Width / 2,
                            frameSize.Height / 2);

                        if (frameSize.Width > frameSize.Height)
                        {
                            //ratio = frameSize.Width / frameSize.Height;
                            modulo = frameSize.Width % frameSize.Height;
                            if (modulo > 0)
                            {
                                int tmp = frameSize.Height - modulo;
                                ratio = frameSize.Width / tmp;
                            }
                            else
                                ratio = frameSize.Width / frameSize.Height;
                            
                            
                            plusY = 1;
                            plusX = 1;
                            borderX = ratio * plusY;
                        }
                        else if ( frameSize.Height >= frameSize.Width)
                        {
                            //ratio =  frameSize.Height / frameSize.Width;
                            modulo = frameSize.Width % frameSize.Height;
                            if (modulo > 0)
                            {
                                int tmp = frameSize.Width - modulo;
                                ratio = frameSize.Height / tmp;
                            }
                            else
                                ratio =  frameSize.Height / frameSize.Width;
                            
                            plusX = 1;
                            plusY = 1;
                            borderY = ratio * plusX;
                        }
                        break;
                    #endregion
                    #region Slides
                    case EffectType.SlideZebraHorizontal:
                        if (currentBitmap == 0)
                            ClearLEDs(null);
                        currentX = frameSize.Width - 1;
                        break;
                    case EffectType.SlideZebraVertical:
                        if (currentBitmap == 0)
                            ClearLEDs(null);
                        currentY = frameSize.Height - 1;
                        break;
                    case EffectType.SlideTopLeft:
                        ClearLEDs(null);


                        if (frameSize.Width > frameSize.Height)
                        {
                            //ratio = frameSize.Width / frameSize.Height;
                            modulo = frameSize.Width % frameSize.Height;
                            if (modulo > 0)
                            {
                                int tmp = frameSize.Height - modulo;
                                ratio = frameSize.Width / tmp;
                                modulo = tmp;
                            }
                            else
                                ratio = frameSize.Width / frameSize.Height;


                            plusY = 1;
                            plusX = 1;
                            borderX = ratio * plusY;
                        }
                        else if ( frameSize.Height >= frameSize.Width)
                        {
                            //ratio =  frameSize.Height / frameSize.Width;
                            modulo = frameSize.Width % frameSize.Height;
                            if (modulo > 0)
                            {
                                int tmp = frameSize.Width - modulo;
                                ratio = frameSize.Height / tmp;
                                modulo = tmp;
                            }
                            else
                                ratio =  frameSize.Height / frameSize.Width;

                            plusX = 1;
                            plusY = 1;
                            borderY = ratio * plusX;
                        }
                        

                        //if (frameSize.Width > frameSize.Height)
                        //{
                        //    slidegcd = gcd(parentWindow.windowBitmap.Width, parentWindow.windowBitmap.Height);

                        //    slideW = parentWindow.windowBitmap.Width / slidegcd;
                        //    slideH = parentWindow.windowBitmap.Height / slidegcd;

                        //    slideDif = slideW - slideH;
                        //    slideStep = 0;

                        //    modulo = frameSize.Width % frameSize.Height;
                        //    if (modulo > 0)
                        //    {
                        //        int tmp = frameSize.Height - modulo;
                        //        ratio = frameSize.Width / tmp;
                        //    }
                        //    else
                        //        ratio = frameSize.Width / frameSize.Height;
                            
                        //    //ratio = frameSize.Width / frameSize.Height;
                        //    plusY = 0;
                        //    plusX = ratio * plusY;
                        //}
                        //else if ( frameSize.Height >= frameSize.Width)
                        //{
                        //    slidegcd = gcd(parentWindow.windowBitmap.Height, parentWindow.windowBitmap.Width);

                        //    slideW = parentWindow.windowBitmap.Width / slidegcd;
                        //    slideH = parentWindow.windowBitmap.Height / slidegcd;

                        //    slideDif = slideH - slideW;
                        //    slideStep = 0;


                        //    modulo = frameSize.Height % frameSize.Width;
                        //    if (modulo > 0)
                        //    {
                        //        int tmp = frameSize.Width - modulo;
                        //        ratio = frameSize.Height / tmp;
                        //    }
                        //    else
                        //        ratio =  frameSize.Height / frameSize.Width;
                            

                        //    //ratio =  frameSize.Height / frameSize.Width;
                        //    plusX = 0;
                        //    plusY = ratio * plusX;
                        //}
                        break;
                    case EffectType.SlideTopRight:
                        ClearLEDs(null);

                        if (frameSize.Width > frameSize.Height)
                        {
                            //ratio = frameSize.Width / frameSize.Height;
                            modulo = frameSize.Width % frameSize.Height;
                            if (modulo > 0)
                            {
                                int tmp = frameSize.Height - modulo;
                                ratio = frameSize.Width / tmp;
                                modulo = tmp;
                            }
                            else
                                ratio = frameSize.Width / frameSize.Height;


                            plusY = 1;
                            plusX = 1;
                            borderX = ratio * plusY;
                        }
                        else if ( frameSize.Height >= frameSize.Width)
                        {
                            //ratio =  frameSize.Height / frameSize.Width;
                            modulo = frameSize.Width % frameSize.Height;
                            if (modulo > 0)
                            {
                                int tmp = frameSize.Width - modulo;
                                ratio = frameSize.Height / tmp;
                                modulo = tmp;
                            }
                            else
                                ratio =  frameSize.Height / frameSize.Width;

                            plusX = 1;
                            plusY = 1;
                            borderY = ratio * plusX;
                        }
                        
                        //if (frameSize.Width > frameSize.Height)
                        //{
                        //    ratio = frameSize.Width / frameSize.Height;
                        //    plusY = 0;
                        //    plusX = ratio * plusY;
                        //}
                        //else if ( frameSize.Height >= frameSize.Width)
                        //{
                        //    ratio =  frameSize.Height / frameSize.Width;
                        //    plusX = 0;
                        //    plusY = ratio * plusX;
                        //}
                        break;
                    case EffectType.SlideBottomLeft:
                        ClearLEDs(null);

                        if (frameSize.Width > frameSize.Height)
                        {
                            //ratio = frameSize.Width / frameSize.Height;
                            modulo = frameSize.Width % frameSize.Height;
                            if (modulo > 0)
                            {
                                int tmp = frameSize.Height - modulo;
                                ratio = frameSize.Width / tmp;
                                modulo = tmp;
                            }
                            else
                                ratio = frameSize.Width / frameSize.Height;


                            plusY = 1;
                            plusX = 1;
                            borderX = ratio * plusY;
                        }
                        else if ( frameSize.Height >= frameSize.Width)
                        {
                            //ratio =  frameSize.Height / frameSize.Width;
                            modulo = frameSize.Width % frameSize.Height;
                            if (modulo > 0)
                            {
                                int tmp = frameSize.Width - modulo;
                                ratio = frameSize.Height / tmp;
                                modulo = tmp;
                            }
                            else
                                ratio =  frameSize.Height / frameSize.Width;

                            plusX = 1;
                            plusY = 1;
                            borderY = ratio * plusX;
                        }
                        
                        //if (frameSize.Width > frameSize.Height)
                        //{
                        //    ratio = frameSize.Width / frameSize.Height;
                        //    plusY = 0;
                        //    plusX = ratio * plusY;
                        //}
                        //else if ( frameSize.Height >= frameSize.Width)
                        //{
                        //    ratio =  frameSize.Height / frameSize.Width;
                        //    plusX = 0;
                        //    plusY = ratio * plusX;
                        //}
                        break;
                    case EffectType.SlideBottomRight:
                        ClearLEDs(null);


                        if (frameSize.Width > frameSize.Height)
                        {
                            //ratio = frameSize.Width / frameSize.Height;
                            modulo = frameSize.Width % frameSize.Height;
                            if (modulo > 0)
                            {
                                int tmp = frameSize.Height - modulo;
                                ratio = frameSize.Width / tmp;
                                modulo = tmp;
                            }
                            else
                                ratio = frameSize.Width / frameSize.Height;


                            plusY = 1;
                            plusX = 1;
                            borderX = ratio * plusY;
                        }
                        else if ( frameSize.Height >= frameSize.Width)
                        {
                            //ratio =  frameSize.Height / frameSize.Width;
                            modulo = frameSize.Width % frameSize.Height;
                            if (modulo > 0)
                            {
                                int tmp = frameSize.Width - modulo;
                                ratio = frameSize.Height / tmp;
                                modulo = tmp;
                            }
                            else
                                ratio =  frameSize.Height / frameSize.Width;

                            plusX = 1;
                            plusY = 1;
                            borderY = ratio * plusX;
                        }
                        

                        //if (frameSize.Width > frameSize.Height)
                        //{
                        //    ratio = frameSize.Width / frameSize.Height;
                        //    plusY = 0;
                        //    plusX = ratio * plusY;
                        //}
                        //else if ( frameSize.Height >= frameSize.Width)
                        //{
                        //    ratio =  frameSize.Height / frameSize.Width;
                        //    plusX = 0;
                        //    plusY = ratio * plusX;
                        //}
                        break;
                    #endregion
                    #region Zebra
                    case EffectType.ZebraHorizontal:
                        ClearLEDs(null);
                        plusX = 0;
                        break;
                    case EffectType.ZebraVertical:
                        ClearLEDs(null);
                        plusY = 0;
                        break;
                    #endregion
                    #region Expand
                    case EffectType.ExpandTop:
                        ClearLEDs(null);
                        currentY = 1;
                        break;
                    case EffectType.ExpandBottom:
                        ClearLEDs(null);
                        currentY = 1;
                        break;
                    case EffectType.ExpandVertical:
                        ClearLEDs(null);
                        currentY = 1;
                        break;
                    #endregion
                    #region Other
                    case EffectType.ScrapeUp:
                        ClearLEDs(null);
                        currentY = frameSize.Height / 2 - 1;
                        borderY = frameSize.Height - 1;
                        break;
                    case EffectType.SnowFall:
                        ClearLEDs(null);
                        currentY = frameSize.Height - 1;
                        borderY = frameSize.Height - 1;
                        currentX = 0;
                        break;
                    case EffectType.SlewingOut:
                        ClearLEDs(null);
                        center = new Point(frameSize.Width / 2,
                            frameSize.Height / 2);

                        currentX = frameSize.Width / 8;
                        currentY = frameSize.Height / 8;
                        startPoint = new Point(-currentX, -currentY);
                        startPoint.Offset(center);

                        newRect = new Rectangle(startPoint, new Size(currentX, currentY));
                        //oldRect = Rectangle.FromLTRB(center.X - plusX, center.Y - plusY, center.X + plusX, center.Y + plusY);
                        break;
                    case EffectType.Bubbles:
                        ClearLEDs(null);
                        currentY = 0;
                        borderY = 0;//frameSize.Height - 1;
                        currentX = 0;
                        break;
                    #endregion
                }
            }
            catch { /*System.Diagnostics.Debugger.Break();*/ }
        }

        #region Animations
        #region Static
        private void AnimateInstant()
        {
            if (lastFrame)
                return;

            if (InputBitmaps != null && InputBitmaps.Count > 0)
            {
                DateTime start = DateTime.Now;

                int tmpX = (frameSize.Width > frameSize.Width)
                    ? frameSize.Width : frameSize.Width;
                int tmpY = (frameSize.Height > frameSize.Height)
                    ? frameSize.Height : frameSize.Height;

                for (int x = 0; x < tmpX; x++)
                {
                    for (int y = 0; y < tmpY; y++)
                    {
                        Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                        if (MainWindow.oldAnimation)
                            DrawLED(x, y, c);
                        else
                        {
                            //if (frameBitmapPixels[x][y].ToArgb() != c.ToArgb())
                            if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                            {
                                frameBitmapPixels[x][y] = c;
                                changedPixels.Add(new ColorAtPixel(c, x, y));
                            }
                        }
                    }
                }

                DateTime end = DateTime.Now;
                //MessageBox.Show((((TimeSpan)(end - start)).TotalMilliseconds).ToString(), "AnimateInstant");
                //Console.WriteLine("AnimateInstant - {0}ms",(((TimeSpan)(end - start)).TotalMilliseconds).ToString());
            }
            lastFrame = true;
        }
        private void AnimateBlink()
        {
            if (lastFrame)
                return;

            if (blinkTimes % 2 == 0)
            {
                for (int y = 0; y < frameSize.Height; y++)
                {
                    for (int x = 0; x < frameSize.Width; x++)
                    {
                        Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                        if (MainWindow.oldAnimation)
                            DrawLED(x, y, staticBitmapPixels[currentBitmap][x][y]);
                        else
                        {
                            //if (frameBitmapPixels[x][y].ToArgb() != c.ToArgb())
                            if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                            {
                                frameBitmapPixels[x][y] = c;
                                changedPixels.Add(new ColorAtPixel(c, x, y));
                            }
                        }
                    }
                }
            }
            else
            {
                for (int y = 0; y < frameSize.Height; y++)
                {
                    for (int x = 0; x < frameSize.Width; x++)
                    {
                        if (MainWindow.oldAnimation)
                            DrawLED(x, y, PreviewWindow.OffLEDColor);
                        else
                        {
                            //changedPixels.Clear();
                            Color c = PreviewWindow.OffLEDColor;//parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);
                            //if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                            //if (frameBitmapPixels[x][y].ToArgb() != c.ToArgb())
                            {
                                frameBitmapPixels[x][y] = c;
                                changedPixels.Add(new ColorAtPixel(c, x, y));
                            }
                        }
                    }
                }
            }
            if (blinkTimes == 0)
            {
                lastFrame = true;
            }
            else
                blinkTimes--;
        }
        private void AnimateBlinkInverse()
        {
            if (lastFrame)
                return;

            if (blinkTimes % 2 == 0)
            {
                for (int y = 0; y < frameSize.Height; y++)
                {
                    for (int x = 0; x < frameSize.Width; x++)
                    {
                        Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                        if (MainWindow.oldAnimation)
                            DrawLED(x, y, staticBitmapPixels[currentBitmap][x][y]);
                        else
                        {
                            //if (frameBitmapPixels[x][y].ToArgb() != c.ToArgb())
                            if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                            {
                                frameBitmapPixels[x][y] = c;
                                changedPixels.Add(new ColorAtPixel(c, x, y));
                            }
                        }
                    }
                }
            }
            else
            {
                for (int y = 0; y < frameSize.Height; y++)
                {
                    for (int x = 0; x < frameSize.Width; x++)
                    {
                        Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);
                        c = parentWindow.Owner.ApplyColorType(Color.FromArgb(255 - c.R, 255 - c.G, 255 - c.B));
                            
                        if (MainWindow.oldAnimation)
                            DrawLED(x, y, c);
                        else
                        {
                            if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                            //if (frameBitmapPixels[x][y].ToArgb() != c.ToArgb())
                            {
                                frameBitmapPixels[x][y] = c;
                                changedPixels.Add(new ColorAtPixel(c, x, y));
                            }
                        }
                    }
                }
            }
            if (blinkTimes == 0)
            {
                lastFrame = true;
            }
            else
                blinkTimes--;
        }
        
        #endregion
        #region Open simple
        private void AnimateOpenLeft()
        {
            if (lastFrame)
                return;

            if (currentX == 0)
                changedPixels.Clear();

            for (int y = 0; y < frameSize.Height; y++)
            {
                if (MainWindow.oldAnimation)
                    DrawLED(currentX, y, staticBitmapPixels[currentBitmap][currentX][y]);
                else
                {
                    int x = currentX;
                    Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    {
                        frameBitmapPixels[x][y] = c;
                        changedPixels.Add(new ColorAtPixel(c, x, y));
                    }
                }
            }

            if (currentX == frameSize.Width - 1)
            {
                lastFrame = true;
            }
            else
                currentX++;
        }
        private void AnimateOpenRight()
        {
            if (lastFrame)
                return;

            for (int y = 0; y < frameSize.Height; y++)
            {
                if (MainWindow.oldAnimation)
                    DrawLED(currentX, y, staticBitmapPixels[currentBitmap][currentX][y]);
                else
                {
                    int x = currentX;
                    Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    {
                        frameBitmapPixels[x][y] = c;
                        changedPixels.Add(new ColorAtPixel(c, x, y));
                    }
                }
            }
            if (currentX == 0)
            {
                lastFrame = true;
            }
            else
                currentX--;
        }
        private void AnimateOpenHorizontal()
        {
            if (lastFrame)
                return;

            for (int y = 0; y < frameSize.Height; y++)
            {

                if (MainWindow.oldAnimation)
                {
                    if (center.X - plusX >= 0)
                        DrawLED(center.X - plusX, y, staticBitmapPixels[currentBitmap][center.X - plusX][y]);
                    if (center.X + plusX < frameSize.Width)
                        DrawLED(center.X + plusX, y, staticBitmapPixels[currentBitmap][center.X + plusX][y]);
                }
                else
                {
                    if (center.X - plusX >= 0)
                    {
                        int x = center.X - plusX;
                        Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                        if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                        {
                            frameBitmapPixels[x][y] = c;
                            changedPixels.Add(new ColorAtPixel(c, x, y));
                        }
                    }
                    if (center.X + plusX < frameSize.Width)
                    {
                        int x = center.X + plusX;
                        Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                        if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                        {
                            frameBitmapPixels[x][y] = c;
                            changedPixels.Add(new ColorAtPixel(c, x, y));
                        }
                    }
                }
            }
            if ((center.X - plusX <= 0) && (center.X + plusX >= frameSize.Width - 1))
            {
                lastFrame = true;
            }
            else
                plusX++;
        }
        private void AnimateOpenVertical()
        {
            if (lastFrame)
                return;

            for (int x = 0; x < frameSize.Width; x++)
            {
                if (MainWindow.oldAnimation)
                {
                    if (center.Y - plusY >= 0)
                        DrawLED(x, center.Y - plusY, staticBitmapPixels[currentBitmap][x][center.Y - plusY]);
                    if (center.Y + plusY < frameSize.Height)
                        DrawLED(x, center.Y + plusY, staticBitmapPixels[currentBitmap][x][center.Y + plusY]);
                }
                else
                {
                    if (center.Y - plusY >= 0)
                    {
                        int y = center.Y - plusY;
                        Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                        if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                        {
                            frameBitmapPixels[x][y] = c;
                            changedPixels.Add(new ColorAtPixel(c, x, y));
                        }
                    }
                    if (center.Y + plusY < frameSize.Height)
                    {
                        int y = center.Y + plusY;
                        Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                        if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                        {
                            frameBitmapPixels[x][y] = c;
                            changedPixels.Add(new ColorAtPixel(c, x, y));
                        }
                    }
                }
            }
            if ((center.Y - plusY <= 0) && (center.Y + plusY >= frameSize.Height - 1))
            {
                lastFrame = true;
            }
            else
                plusY++;
        }
        private void AnimateOpenLeftRight()
        {
            if (lastFrame)
                return;

            if (currentX == 0)
                changedPixels.Clear();

            for (int y = 0; y < frameSize.Height; y++)
            {
                if (MainWindow.oldAnimation)
                {
                    DrawLED(currentX, y, staticBitmapPixels[currentBitmap][currentX][y]);
                    DrawLED((frameSize.Width - 1) - currentX, y, staticBitmapPixels[currentBitmap][(frameSize.Width - 1) - currentX][y]);
                }
                else
                {
                    int x = currentX;
                    Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    {
                        frameBitmapPixels[x][y] = c;
                        changedPixels.Add(new ColorAtPixel(c, x, y));
                    }

                    x = (frameSize.Width - 1) - currentX;
                    c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    {
                        frameBitmapPixels[x][y] = c;
                        changedPixels.Add(new ColorAtPixel(c, x, y));
                    }
                }
            }
            if (currentX >= (frameSize.Width - 1)/2)
            {
                lastFrame = true;
            }
            else
                currentX++;
        }
        private void AnimateOpenUpDown()
        {
            if (lastFrame)
                return;

            if (currentY == 0)
                changedPixels.Clear();

            for (int x = 0; x < frameSize.Width; x++)
            {
                if (MainWindow.oldAnimation)
                {
                    int y = currentY;
                    DrawLED(x, y, staticBitmapPixels[currentBitmap][x][y]);
                    y = (frameSize.Height - 1) - currentY;
                    DrawLED(x, y, staticBitmapPixels[currentBitmap][x][y]);
                }
                else
                {
                    int y = currentY;
                    Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    {
                        frameBitmapPixels[x][y] = c;
                        changedPixels.Add(new ColorAtPixel(c, x, y));
                    }

                    y = (frameSize.Height - 1) - currentY;
                    c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    {
                        frameBitmapPixels[x][y] = c;
                        changedPixels.Add(new ColorAtPixel(c, x, y));
                    }
                }
            }
            if (currentY >= (frameSize.Height - 1) / 2)
            {
                lastFrame = true;
            }
            else
                currentY++;
        }

        private void AnimateOpenTopLeft()
        {
            if (lastFrame)
                return;

            if (frameSize.Width > frameSize.Height)
            {
                //ratiof = frameSize.Width / (float)frameSize.Height;
                if (plusX >= borderXf)
                    plusY++;
                plusX++;
                if (plusX >= borderXf)
                {
                    borderXf = ratiof * plusY;

                    //if (modulo > 0)
                    //{
                    //    modulo--;
                    //    borderXf++;
                    //}
                }
            }
            else if ( frameSize.Height >= frameSize.Width)
            {
                //ratiof = frameSize.Height / (float)frameSize.Width;
                if (plusY >= borderYf)
                    plusX++;
                plusY++;
                if (plusY >= borderY)
                {
                    borderY = ratio * plusX;

                    //if (modulo > 0)
                    //{
                    //    modulo--;
                    //    borderY++;
                    //}
                }
            }

            //double[] xtab = new double[4] { center.X - plusX, center.X, center.X + plusX, center.X };
            //double[] ytab = new double[4] { center.Y, center.Y - plusY, center.Y, center.Y + plusY };

            double[] xtab = new double[4] { - plusX, 0, plusX, 0 };
            double[] ytab = new double[4] { 0, - plusY, 0, plusY };


            for (int x =- plusX; x <=  plusX; x++)
            {
                for (int y = - plusY; y <= plusY; y++)
                {
                    if ((x >= 0 && x < frameSize.Width)
                        && (y >= 0 && y < frameSize.Height))
                    {

                        //if (newRect.Contains(x, y) && !oldRect.Contains(x, y))
                        if (IsPointInPolygon(4, xtab, ytab, x, y))
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, y, staticBitmapPixels[currentBitmap][x][y]);
                            else
                            {
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                    }
                }
            }

            if ((IsPointInPolygon(4, xtab, ytab, 0, 0))
                && (IsPointInPolygon(4, xtab, ytab, 0, frameSize.Height - 1))
                && (IsPointInPolygon(4, xtab, ytab, frameSize.Width - 1, 0))
                && (IsPointInPolygon(4, xtab, ytab, frameSize.Width - 1, frameSize.Height - 1)))
            {
                lastFrame = true;
            }
        }
        private void AnimateOpenTopRight()
        {
            if (lastFrame)
                return;

            if (frameSize.Width > frameSize.Height)
            {
                //ratiof = frameSize.Width / (float)frameSize.Height;
                if (plusX >= borderXf)
                    plusY++;
                plusX++;
                if (plusX >= borderXf)
                {
                    borderXf = ratiof * plusY;

                    //if (modulo > 0)
                    //{
                    //    modulo--;
                    //    borderXf++;
                    //}
                }
            }
            else if ( frameSize.Height >= frameSize.Width)
            {
                //ratiof = frameSize.Height / (float)frameSize.Width;
                if (plusY >= borderYf)
                    plusX++;
                plusY++;
                if (plusY >= borderY)
                {
                    borderY = ratio * plusX;

                    //if (modulo > 0)
                    //{
                    //    modulo--;
                    //    borderY++;
                    //}
                }
            }

            //double[] xtab = new double[4] { center.X - plusX, center.X, center.X + plusX, center.X };
            //double[] ytab = new double[4] { center.Y, center.Y - plusY, center.Y, center.Y + plusY };

            int transformX = frameSize.Width - 1; //0
            int transformY = 0; //frameSize.Height - 1;

            double[] xtab = new double[4] { transformX - plusX, transformX, transformX + plusX, transformX };
            double[] ytab = new double[4] { transformY, transformY - plusY, transformY, transformY + plusY };


            for (int x = transformX - plusX; x <= transformX + plusX; x++)
            {
                for (int y = transformY - plusY; y <= transformY + plusY; y++)
                {
                    if ((x >= 0 && x < frameSize.Width)
                        && (y >= 0 && y < frameSize.Height))
                    {

                        //if (newRect.Contains(x, y) && !oldRect.Contains(x, y))
                        if (IsPointInPolygon(4, xtab, ytab, x, y))
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, y, staticBitmapPixels[currentBitmap][x][y]);
                            else
                            {
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                    }
                }
            }

            if ((IsPointInPolygon(4, xtab, ytab, 0, 0))
                && (IsPointInPolygon(4, xtab, ytab, 0, frameSize.Height - 1))
                && (IsPointInPolygon(4, xtab, ytab, frameSize.Width - 1, 0))
                && (IsPointInPolygon(4, xtab, ytab, frameSize.Width - 1, frameSize.Height - 1)))
            {
                lastFrame = true;
            }
        }
        private void AnimateOpenBottomLeft()
        {
            if (lastFrame)
                return;

            if (frameSize.Width > frameSize.Height)
            {
                //ratiof = frameSize.Width / (float)frameSize.Height;
                if (plusX >= borderXf)
                    plusY++;
                plusX++;
                if (plusX >= borderXf)
                {
                    borderXf = ratiof * plusY;

                    //if (modulo > 0)
                    //{
                    //    modulo--;
                    //    borderXf++;
                    //}
                }
            }
            else if ( frameSize.Height >= frameSize.Width)
            {
                //ratiof = frameSize.Height / (float)frameSize.Width;
                if (plusY >= borderYf)
                    plusX++;
                plusY++;
                if (plusY >= borderY)
                {
                    borderY = ratio * plusX;

                    //if (modulo > 0)
                    //{
                    //    modulo--;
                    //    borderY++;
                    //}
                }
            }

            //double[] xtab = new double[4] { center.X - plusX, center.X, center.X + plusX, center.X };
            //double[] ytab = new double[4] { center.Y, center.Y - plusY, center.Y, center.Y + plusY };

            int transformX = 0; //frameSize.Width - 1;
            int transformY = frameSize.Height - 1; //0;

            double[] xtab = new double[4] { transformX - plusX, transformX, transformX + plusX, transformX };
            double[] ytab = new double[4] { transformY, transformY - plusY, transformY, transformY + plusY };


            for (int x = transformX - plusX; x <= transformX + plusX; x++)
            {
                for (int y = transformY - plusY; y <= transformY + plusY; y++)
                {
                    if ((x >= 0 && x < frameSize.Width)
                        && (y >= 0 && y < frameSize.Height))
                    {

                        //if (newRect.Contains(x, y) && !oldRect.Contains(x, y))
                        if (IsPointInPolygon(4, xtab, ytab, x, y))
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, y, staticBitmapPixels[currentBitmap][x][y]);
                            else
                            {
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                    }
                }
            }

            if ((IsPointInPolygon(4, xtab, ytab, 0, 0))
                && (IsPointInPolygon(4, xtab, ytab, 0, frameSize.Height - 1))
                && (IsPointInPolygon(4, xtab, ytab, frameSize.Width - 1, 0))
                && (IsPointInPolygon(4, xtab, ytab, frameSize.Width - 1, frameSize.Height - 1)))
            {
                lastFrame = true;
            }
        }
        private void AnimateOpenBottomRight()
        {
            if (lastFrame)
                return;

            if (frameSize.Width > frameSize.Height)
            {
                //ratiof = frameSize.Width / (float)frameSize.Height;
                if (plusX >= borderXf)
                    plusY++;
                plusX++;
                if (plusX >= borderXf)
                {
                    borderXf = ratiof * plusY;

                    //if (modulo > 0)
                    //{
                    //    modulo--;
                    //    borderXf++;
                    //}
                }
            }
            else if ( frameSize.Height >= frameSize.Width)
            {
                //ratiof = frameSize.Height / (float)frameSize.Width;
                if (plusY >= borderYf)
                    plusX++;
                plusY++;
                if (plusY >= borderY)
                {
                    borderY = ratio * plusX;

                    //if (modulo > 0)
                    //{
                    //    modulo--;
                    //    borderY++;
                    //}
                }
            }

            //double[] xtab = new double[4] { center.X - plusX, center.X, center.X + plusX, center.X };
            //double[] ytab = new double[4] { center.Y, center.Y - plusY, center.Y, center.Y + plusY };

            int transformX = frameSize.Width - 1; //0;
            int transformY = frameSize.Height - 1; //0;

            double[] xtab = new double[4] { transformX - plusX, transformX, transformX + plusX, transformX };
            double[] ytab = new double[4] { transformY, transformY - plusY, transformY, transformY + plusY };


            for (int x = transformX - plusX; x <= transformX + plusX; x++)
            {
                for (int y = transformY - plusY; y <= transformY + plusY; y++)
                {
                    if ((x >= 0 && x < frameSize.Width)
                        && (y >= 0 && y < frameSize.Height))
                    {

                        //if (newRect.Contains(x, y) && !oldRect.Contains(x, y))
                        if (IsPointInPolygon(4, xtab, ytab, x, y))
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, y, staticBitmapPixels[currentBitmap][x][y]);
                            else
                            {
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                    }
                }
            }

            if ((IsPointInPolygon(4, xtab, ytab, 0, 0))
                && (IsPointInPolygon(4, xtab, ytab, 0, frameSize.Height - 1))
                && (IsPointInPolygon(4, xtab, ytab, frameSize.Width - 1, 0))
                && (IsPointInPolygon(4, xtab, ytab, frameSize.Width - 1, frameSize.Height - 1)))
            {
                lastFrame = true;
            }
        }
        private void AnimateOpenBackslash()
        {
            if (lastFrame)
                return;

            if (frameSize.Width > frameSize.Height)
            {
                //ratiof = frameSize.Width / (float)frameSize.Height;
                if (plusX >= borderXf)
                    plusY++;
                plusX++;
                if (plusX >= borderXf)
                {
                    borderXf = ratiof * plusY;

                    //if (modulo > 0)
                    //{
                    //    modulo--;
                    //    borderXf++;
                    //}
                }
            }
            else if ( frameSize.Height >= frameSize.Width)
            {
                //ratiof = frameSize.Height / (float)frameSize.Width;
                if (plusY >= borderYf)
                    plusX++;
                plusY++;
                if (plusY >= borderY)
                {
                    borderY = ratio * plusX;

                    //if (modulo > 0)
                    //{
                    //    modulo--;
                    //    borderY++;
                    //}
                }
            }

            //double[] xtab = new double[4] { center.X - plusX, center.X, center.X + plusX, center.X };
            //double[] ytab = new double[4] { center.Y, center.Y - plusY, center.Y, center.Y + plusY };

            int transformX = 0; //frameSize.Width - 1;
            int transformY = frameSize.Height - 1; //0;

            double[] xtab = new double[4] { transformX - plusX, transformX, transformX + plusX, transformX };
            double[] ytab = new double[4] { transformY, transformY - plusY, transformY, transformY + plusY };


            for (int x = transformX - plusX; x <= transformX + plusX; x++)
            {
                for (int y = transformY - plusY; y <= transformY + plusY; y++)
                {
                    if ((x >= 0 && x < frameSize.Width)
                        && (y >= 0 && y < frameSize.Height))
                    {

                        //if (newRect.Contains(x, y) && !oldRect.Contains(x, y))
                        if (IsPointInPolygon(4, xtab, ytab, x, y))
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, y, staticBitmapPixels[currentBitmap][x][y]);
                            else
                            {
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                    }
                }
            }

            bool ftl = IsPointInPolygon(4, xtab, ytab, 0, 0);
            bool fbl = IsPointInPolygon(4, xtab, ytab, 0, frameSize.Height - 1);
            bool ftr = IsPointInPolygon(4, xtab, ytab, frameSize.Width - 1, 0);
            bool fbr = IsPointInPolygon(4, xtab, ytab, frameSize.Width - 1, frameSize.Height - 1);
            //---------------------



            transformX = frameSize.Width - 1; //0; //
            transformY = 0; //frameSize.Height - 1; //

            xtab = new double[4] { transformX - plusX, transformX, transformX + plusX, transformX };
            ytab = new double[4] { transformY, transformY - plusY, transformY, transformY + plusY };


            for (int x = transformX - plusX; x <= transformX + plusX; x++)
            {
                for (int y = transformY - plusY; y <= transformY + plusY; y++)
                {
                    if ((x >= 0 && x < frameSize.Width)
                        && (y >= 0 && y < frameSize.Height))
                    {

                        //if (newRect.Contains(x, y) && !oldRect.Contains(x, y))
                        if (IsPointInPolygon(4, xtab, ytab, x, y))
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, y, staticBitmapPixels[currentBitmap][x][y]);
                            else
                            {
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                    }
                }
            }

            bool stl = IsPointInPolygon(4, xtab, ytab, 0, 0);
            bool sbl = IsPointInPolygon(4, xtab, ytab, 0, frameSize.Height - 1);
            bool str = IsPointInPolygon(4, xtab, ytab, frameSize.Width - 1, 0);
            bool sbr = IsPointInPolygon(4, xtab, ytab, frameSize.Width - 1, frameSize.Height - 1);

            //-------------------------------------------

            if ((ftl && stl) && (fbr && sbr) && (ftr || str) && (fbl || sbl))
            {
                lastFrame = true;
            }
        }
        private void AnimateOpenSlash()
        {
            if (lastFrame)
                return;

            if (frameSize.Width > frameSize.Height)
            {
                //ratiof = frameSize.Width / (float)frameSize.Height;
                if (plusX >= borderXf)
                    plusY++;
                plusX++;
                if (plusX >= borderXf)
                {
                    borderXf = ratiof * plusY;

                    //if (modulo > 0)
                    //{
                    //    modulo--;
                    //    borderXf++;
                    //}
                }
            }
            else if ( frameSize.Height >= frameSize.Width)
            {
                //ratiof = frameSize.Height / (float)frameSize.Width;
                if (plusY >= borderYf)
                    plusX++;
                plusY++;
                if (plusY >= borderY)
                {
                    borderY = ratio * plusX;

                    //if (modulo > 0)
                    //{
                    //    modulo--;
                    //    borderY++;
                    //}
                }
            }

            //double[] xtab = new double[4] { center.X - plusX, center.X, center.X + plusX, center.X };
            //double[] ytab = new double[4] { center.Y, center.Y - plusY, center.Y, center.Y + plusY };

            int transformX = frameSize.Width - 1; //0;
            int transformY = frameSize.Height - 1; //0;

            double[] xtab = new double[4] { transformX - plusX, transformX, transformX + plusX, transformX };
            double[] ytab = new double[4] { transformY, transformY - plusY, transformY, transformY + plusY };


            for (int x = transformX - plusX; x <= transformX + plusX; x++)
            {
                for (int y = transformY - plusY; y <= transformY + plusY; y++)
                {
                    if ((x >= 0 && x < frameSize.Width)
                        && (y >= 0 && y < frameSize.Height))
                    {

                        //if (newRect.Contains(x, y) && !oldRect.Contains(x, y))
                        if (IsPointInPolygon(4, xtab, ytab, x, y))
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, y, staticBitmapPixels[currentBitmap][x][y]);
                            else
                            {
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                    }
                }
            }


            bool ftl = IsPointInPolygon(4, xtab, ytab, 0, 0);
            bool fbl = IsPointInPolygon(4, xtab, ytab, 0, frameSize.Height - 1);
            bool ftr = IsPointInPolygon(4, xtab, ytab, frameSize.Width - 1, 0);
            bool fbr = IsPointInPolygon(4, xtab, ytab, frameSize.Width - 1, frameSize.Height - 1);
            //---------------------



            transformX = 0; //frameSize.Width - 1; //
            transformY = 0; //frameSize.Height - 1; //

            xtab = new double[4] { transformX - plusX, transformX, transformX + plusX, transformX };
            ytab = new double[4] { transformY, transformY - plusY, transformY, transformY + plusY };


            for (int x = transformX - plusX; x <= transformX + plusX; x++)
            {
                for (int y = transformY - plusY; y <= transformY + plusY; y++)
                {
                    if ((x >= 0 && x < frameSize.Width)
                        && (y >= 0 && y < frameSize.Height))
                    {

                        //if (newRect.Contains(x, y) && !oldRect.Contains(x, y))
                        if (IsPointInPolygon(4, xtab, ytab, x, y))
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, y, staticBitmapPixels[currentBitmap][x][y]);
                            else
                            {
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                    }
                }
            }

            bool stl = IsPointInPolygon(4, xtab, ytab, 0, 0);
            bool sbl = IsPointInPolygon(4, xtab, ytab, 0, frameSize.Height - 1);
            bool str = IsPointInPolygon(4, xtab, ytab, frameSize.Width - 1, 0);
            bool sbr = IsPointInPolygon(4, xtab, ytab, frameSize.Width - 1, frameSize.Height - 1);

            //-------------------------------------------

            if ((ftl || stl) && (fbr || sbr) && (ftr && str) && (fbl && sbl))
            {
                lastFrame = true;
            }
        }
        
        #endregion
        #region Blind, shutter, chessboard
        private void AnimateBlindHorizontial()
        {
            if (lastFrame)
                return;

            for (int y = 0; y < frameSize.Height; y++)
            {
                if (MainWindow.oldAnimation)
                    DrawLED(currentX, y, staticBitmapPixels[currentBitmap][currentX][y]);
                else
                {
                    int x = currentX;
                    Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    {
                        frameBitmapPixels[x][y] = c;
                        changedPixels.Add(new ColorAtPixel(c, x, y));
                    }
                }
            }

            if (currentX == frameSize.Width - 8 || currentX >= frameSize.Width)
            {
                lastFrame = true;
            }
            else if (currentX % 8 == 0)
                currentX += 15;
            else
                currentX--;
        }
        private void AnimateShutterVertical()
        {
            if (lastFrame)
                return;

            if (currentX == 0)
                changedPixels.Clear();

            for (int y = 0; y < frameSize.Height; y++)
            {
                for (int xaddition = currentX; xaddition < frameSize.Width; xaddition += shutterStep)
                {

                    if (MainWindow.oldAnimation)
                        DrawLED(xaddition, y, staticBitmapPixels[currentBitmap][xaddition][y]);
                    else
                    {
                        int x = xaddition;
                        Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                        if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                        {
                            frameBitmapPixels[x][y] = c;
                            changedPixels.Add(new ColorAtPixel(c, x, y));
                        }
                    }
                }
            }

            if (currentX >= shutterStep)
            {
                lastFrame = true;
            }
            else
                currentX++;
        }
        private void AnimateShutterHorizontal()
        {
            if (lastFrame)
                return;

            if (currentY == 0)
                changedPixels.Clear();

            for (int x = 0; x < frameSize.Width; x++)
            {
                for (int yaddition = currentY; yaddition < frameSize.Height; yaddition += shutterStep)
                {

                    if (MainWindow.oldAnimation)
                        DrawLED(x, yaddition, staticBitmapPixels[currentBitmap][x][yaddition]);
                    else
                    {
                        int y = yaddition;
                        Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                        if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                        {
                            frameBitmapPixels[x][y] = c;
                            changedPixels.Add(new ColorAtPixel(c, x, y));
                        }
                    }
                }
            }

            if (currentY >= shutterStep)
            {
                lastFrame = true;
            }
            else
                currentY++;
        }
        private void AnimateChessboardHorizontal()
        {
            if (lastFrame)
                return;

            if (currentX == 0)
                changedPixels.Clear();

            for (int y = 0; y < frameSize.Height; y++)
            {
                for (int xaddition = currentX; xaddition < frameSize.Width; xaddition += chessboardStep*2)
                {
                    if ((y / chessboardStep) % 2 > 0)
                    {
                        xaddition -= chessboardStep;
                    }

                    if (xaddition >= 0)
                    {
                        if (MainWindow.oldAnimation)
                            DrawLED(xaddition, y, staticBitmapPixels[currentBitmap][xaddition][y]);
                        else
                        {
                            int x = xaddition;
                            Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                            if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                            {
                                frameBitmapPixels[x][y] = c;
                                changedPixels.Add(new ColorAtPixel(c, x, y));
                            }
                        }
                    }

                    if ((y / chessboardStep) % 2 > 0)
                    {
                        xaddition += chessboardStep;
                    }
                }


                if ((y / chessboardStep) % 2 > 0 && currentX >= chessboardStep)
                {
                    int x = frameSize.Width - (chessboardStep - (currentX - chessboardStep));
                    if (x < frameSize.Width)
                    {
                        if (MainWindow.oldAnimation)
                            DrawLED(x, y, staticBitmapPixels[currentBitmap][x][y]);
                        else
                        {
                            //int x = xaddition + chessboardStep;
                            Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                            if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                            {
                                frameBitmapPixels[x][y] = c;
                                changedPixels.Add(new ColorAtPixel(c, x, y));
                            }
                        }
                    }
                }
            }

            if (currentX >= chessboardStep*2)
            {
                lastFrame = true;
            }
            else
                currentX++;
        }
        #endregion
        #region Shift and scroll
        private void AnimateShiftLeft()
        {
            if (lastFrame)
                return;


            if (MainWindow.oldAnimation)
            {
                using (Bitmap currentWindowImage = new Bitmap((frameSize.Width - 1) * (PreviewWindow.LedDiam + PreviewWindow.Space),
                    frameSize.Height * (PreviewWindow.LedDiam + PreviewWindow.Space)))
                {
                    using (Graphics windowGraphics = Graphics.FromImage(parentWindow.windowBitmap), currentWindowGraphics = Graphics.FromImage(currentWindowImage))
                    {
                        currentWindowGraphics.DrawImageUnscaledAndClipped(parentWindow.windowBitmap, new Rectangle(-1 * (PreviewWindow.LedDiam + PreviewWindow.Space), 0, currentWindowImage.Width + 1 * (PreviewWindow.LedDiam + PreviewWindow.Space), currentWindowImage.Height));
                        windowGraphics.DrawImageUnscaled(currentWindowImage, 0, 0);

                    }
                }
            }
            else
            {
                for (int i = 0; i < changedPixels.Count; i++)
                {
                    //if (changedPixels[i].PixelPoint.X > currentX)
                        changedPixels[i].PixelPoint = new Point(changedPixels[i].PixelPoint.X -1, changedPixels[i].PixelPoint.Y);
                }
            }
         
            for (int y = 0; y < frameSize.Height; y++)
            {
                for (int x = 0; x < frameSize.Width - currentX; x++)
                {
                    //DrawLED(x + currentX, y, staticBitmapPixels[currentBitmap][x][y]);
                }
                if (MainWindow.oldAnimation)
                {
                    DrawLED(frameSize.Width - 1, y, staticBitmapPixels[currentBitmap][frameSize.Width - currentX - 1][y]);
                }
                else 
                {
                    int x = frameSize.Width - 1;
                    Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][frameSize.Width - currentX - 1][y]);

                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    {
                        frameBitmapPixels[x][y] = c;
                        changedPixels.Add(new ColorAtPixel(c, x, y));
                    }
                }
            }
            if (currentX == 0)
            {
                lastFrame = true;
            }
            else
                currentX--;
        }
        private void AnimateShiftRight()
        {
            if (lastFrame)
                return;

            if (MainWindow.oldAnimation)
            {
                Graphics windowGraphics = Graphics.FromImage(parentWindow.windowBitmap);


                Bitmap currentWindowImage = new Bitmap((frameSize.Width) * (PreviewWindow.LedDiam + PreviewWindow.Space),
                    frameSize.Height * (PreviewWindow.LedDiam + PreviewWindow.Space));
                Graphics currentWindowGraphics = Graphics.FromImage(currentWindowImage);
                currentWindowGraphics.DrawImageUnscaledAndClipped(parentWindow.windowBitmap, new Rectangle(1 * (PreviewWindow.LedDiam + PreviewWindow.Space), 0,
                    currentWindowImage.Width - 1 * (PreviewWindow.LedDiam + PreviewWindow.Space), currentWindowImage.Height));
                windowGraphics.DrawImageUnscaled(currentWindowImage, 0, 0);


                //Bitmap currentWindowImage = new Bitmap((parentWindow.windowBitmap.Width - 1) * (PreviewWindow.LedDiam + PreviewWindow.Space),
                //    parentWindow.windowBitmap.Height * (PreviewWindow.LedDiam + PreviewWindow.Space));
                //Graphics currentWindowGraphics = Graphics.FromImage(currentWindowImage);
                //currentWindowGraphics.DrawImageUnscaledAndClipped(parentWindow.windowBitmap, new Rectangle(1 * (PreviewWindow.LedDiam + PreviewWindow.Space), 0, 
                //    currentWindowImage.Width - 1 * (PreviewWindow.LedDiam + PreviewWindow.Space), currentWindowImage.Height));
                //windowGraphics.DrawImageUnscaled(currentWindowImage, 0, 0);

                currentWindowImage.Dispose();
                currentWindowGraphics.Dispose();
                windowGraphics.Dispose();
            }
            else
            {
                for (int i = 0; i < changedPixels.Count; i++)
                {
                    //if (changedPixels[i].PixelPoint.X > currentX)
                    changedPixels[i].PixelPoint = new Point(changedPixels[i].PixelPoint.X + 1, changedPixels[i].PixelPoint.Y);
                }
                //int y = borderY + 1;
                //Color c = pixelColor;//parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                //if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                //{
                //    frameBitmapPixels[x][y] = c;
                //    changedPixels.Add(new ColorAtPixel(c, x, y));
                //}
            }
         



            for (int y = 0; y < frameSize.Height; y++)
            {
                for (int x = frameSize.Width - 1; x > currentX; x--)
                {
                    //DrawLED(frameSize.Width - (x + currentX), y, staticBitmapPixels[currentBitmap][x][y]);
                }
                
                if (MainWindow.oldAnimation)
                {
                   DrawLED(0, y, staticBitmapPixels[currentBitmap][frameSize.Width - currentX - 1][y]);
                }
                else
                {
                    int x = 0;
                    Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][frameSize.Width - currentX - 1][y]);

                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    {
                        frameBitmapPixels[x][y] = c;
                        changedPixels.Add(new ColorAtPixel(c, x, y));
                    }
                }
            }
            if (currentX == frameSize.Width - 1)
            {
                lastFrame = true;
            }
            else
                currentX++;
        }
        private void AnimateShiftUp()
        {
            if (lastFrame)
                return;

            if (MainWindow.oldAnimation)
            {
                Graphics windowGraphics = Graphics.FromImage(parentWindow.windowBitmap);

                Bitmap currentWindowImage = new Bitmap((frameSize.Width * (PreviewWindow.LedDiam + PreviewWindow.Space)),
                    //parentWindow.windowBitmap.Height - 
                    ((frameSize.Height - 1) * (PreviewWindow.LedDiam + PreviewWindow.Space)));
                Graphics currentWindowGraphics = Graphics.FromImage(currentWindowImage);
                currentWindowGraphics.DrawImageUnscaledAndClipped(parentWindow.windowBitmap, new Rectangle(0, -1 * (PreviewWindow.LedDiam + PreviewWindow.Space),
                    currentWindowImage.Width, currentWindowImage.Height + 1 * (PreviewWindow.LedDiam + PreviewWindow.Space)));
                windowGraphics.DrawImageUnscaled(currentWindowImage, 0, 0);

                currentWindowImage.Dispose();
                currentWindowGraphics.Dispose();
                windowGraphics.Dispose();
            }
            else
            {
                for (int i = 0; i < changedPixels.Count; i++)
                {
                    //if (changedPixels[i].PixelPoint.X > currentX)
                    changedPixels[i].PixelPoint = new Point(changedPixels[i].PixelPoint.X, changedPixels[i].PixelPoint.Y - 1);
                }
            }



            for (int x = 0; x < frameSize.Width; x++)
            {
                for (int y = 0; y < frameSize.Height - currentY; y++)
                {
                    //if (MainWindow.oldAnimation)
                    //{
                    //    DrawLED(x, y + currentY, staticBitmapPixels[currentBitmap][x][y]);
                    //}
                    //else
                    //{
                    //    Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                    //    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    //    {
                    //        frameBitmapPixels[x][y + currentY] = c;
                    //        changedPixels.Add(new ColorAtPixel(c, x, y + currentY));
                    //    }
                    //}
                }
                
                if (MainWindow.oldAnimation)
                {
                    DrawLED(x, frameSize.Height - 1, staticBitmapPixels[currentBitmap][x][frameSize.Height - currentY - 1]);
                }
                else
                {
                    int y = frameSize.Height - 1;
                    Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][frameSize.Height - currentY - 1]);

                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    {
                        frameBitmapPixels[x][y] = c;
                        changedPixels.Add(new ColorAtPixel(c, x, y));
                    }
                }
            }
            if (currentY == 0)
            {
                lastFrame = true;
            }
            else
                currentY--;
        }
        private void AnimateShiftDown()
        {
            if (lastFrame)
                return;

            if (MainWindow.oldAnimation)
            {
                Graphics windowGraphics = Graphics.FromImage(parentWindow.windowBitmap);

                Bitmap currentWindowImage = new Bitmap(((frameSize.Width) * (PreviewWindow.LedDiam + PreviewWindow.Space)),
                    (frameSize.Height * (PreviewWindow.LedDiam + PreviewWindow.Space))
                    //parentWindow.windowBitmap.Height// - (1 * (PreviewWindow.LedDiam + PreviewWindow.Space))
                    );
                Graphics currentWindowGraphics = Graphics.FromImage(currentWindowImage);
                currentWindowGraphics.DrawImageUnscaledAndClipped(parentWindow.windowBitmap, new Rectangle(0, 1 * (PreviewWindow.LedDiam + PreviewWindow.Space),
                    currentWindowImage.Width, currentWindowImage.Height// + 1 * (PreviewWindow.LedDiam + PreviewWindow.Space)
                    ));
                windowGraphics.DrawImageUnscaled(currentWindowImage, 0, 0);

                currentWindowImage.Dispose();
                currentWindowGraphics.Dispose();
                windowGraphics.Dispose();
            }
            else
            {
                for (int i = 0; i < changedPixels.Count; i++)
                {
                    //if (changedPixels[i].PixelPoint.X > currentX)
                    changedPixels[i].PixelPoint = new Point(changedPixels[i].PixelPoint.X, changedPixels[i].PixelPoint.Y + 1);
                }
            }

            for (int x = 0; x < frameSize.Width; x++)
            {
                for (int y = 0; y < frameSize.Height - currentY; y++)
                {
                    //DrawLED(x, y + currentY, staticBitmapPixels[currentBitmap][x][y]);
                }
                DrawLED(x, 0, staticBitmapPixels[currentBitmap][x][frameSize.Height - currentY - 1]);


                if (MainWindow.oldAnimation)
                {
                    DrawLED(x, 0, staticBitmapPixels[currentBitmap][x][frameSize.Height - currentY - 1]);
                }
                else
                {
                    int y = 0;
                    Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][frameSize.Height - currentY - 1]);

                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    {
                        frameBitmapPixels[x][y] = c;
                        changedPixels.Add(new ColorAtPixel(c, x, y));
                    }
                }
            }
            if (currentY == frameSize.Height - 1)
            {
                lastFrame = true;
            }
            else
                currentY++;
        }
        private void AnimateScrollRightCont()
        {
            if (lastFrame)
                return;

            if (MainWindow.oldAnimation)
            {
                //Graphics windowGraphics = Graphics.FromImage(parentWindow.windowBitmap);

                //Bitmap currentWindowImage = new Bitmap((frameSize.Width - 1) * (PreviewWindow.LedDiam + PreviewWindow.Space), 
                //    frameSize.Height * (PreviewWindow.LedDiam + PreviewWindow.Space));
                //Graphics currentWindowGraphics = Graphics.FromImage(currentWindowImage);
                //currentWindowGraphics.DrawImageUnscaledAndClipped(parentWindow.windowBitmap, new Rectangle(1 * (PreviewWindow.LedDiam + PreviewWindow.Space), 0, 
                //    currentWindowImage.Width - 1 * (PreviewWindow.LedDiam + PreviewWindow.Space), currentWindowImage.Height));
                //windowGraphics.DrawImageUnscaled(currentWindowImage, 0, 0);

                Graphics windowGraphics = Graphics.FromImage(parentWindow.windowBitmap);


                Bitmap currentWindowImage = new Bitmap((frameSize.Width) * (PreviewWindow.LedDiam + PreviewWindow.Space),
                    frameSize.Height * (PreviewWindow.LedDiam + PreviewWindow.Space));
                Graphics currentWindowGraphics = Graphics.FromImage(currentWindowImage);
                currentWindowGraphics.DrawImageUnscaledAndClipped(parentWindow.windowBitmap, new Rectangle(1 * (PreviewWindow.LedDiam + PreviewWindow.Space), 0,
                    currentWindowImage.Width - 1 * (PreviewWindow.LedDiam + PreviewWindow.Space), currentWindowImage.Height));
                windowGraphics.DrawImageUnscaled(currentWindowImage, 0, 0);


                currentWindowImage.Dispose();
                currentWindowGraphics.Dispose();
                windowGraphics.Dispose();
            }
            else
            {
                for (int i = 0; i < changedPixels.Count; i++)
                {
                    changedPixels[i].PixelPoint = new Point(changedPixels[i].PixelPoint.X - 1, changedPixels[i].PixelPoint.Y);
                }
            }

            for (int y = 0; y < frameSize.Height; y++)
            {
                for (int x = 0; x < frameSize.Width - currentX; x++)
                {
                    // DrawLED(x + currentX, y, staticBitmapPixels[currentBitmap][x][y]);
                }



                if (MainWindow.oldAnimation)
                {
                    DrawLED(0, y, staticBitmapPixels[currentBitmap][frameSize.Width - currentX - 1][y]);

                }
                else
                {
                    int x = 0;
                    Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][frameSize.Width - currentX - 1][y]);

                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    {
                        frameBitmapPixels[x][y] = c;
                        changedPixels.Add(new ColorAtPixel(c, x, y));
                    }
                }
            }
            if (currentX == frameSize.Width - 1)
            {
                if (currentBitmap > 0)
                {
                    currentBitmap--;
                    currentX = 0;
                }
                else
                    lastFrame = true;
            }
            else
                currentX++;
        }
        private void AnimateScrollLeftCont()
        {
            if (lastFrame)
                return;

            if (MainWindow.oldAnimation)
            {
                Graphics windowGraphics = Graphics.FromImage(parentWindow.windowBitmap);

                Bitmap currentWindowImage = new Bitmap((frameSize.Width - 1) * (PreviewWindow.LedDiam + PreviewWindow.Space), frameSize.Height * (PreviewWindow.LedDiam + PreviewWindow.Space));
                Graphics currentWindowGraphics = Graphics.FromImage(currentWindowImage);
                currentWindowGraphics.DrawImageUnscaledAndClipped(parentWindow.windowBitmap, new Rectangle(-1 * (PreviewWindow.LedDiam + PreviewWindow.Space), 0, currentWindowImage.Width + 1 * (PreviewWindow.LedDiam + PreviewWindow.Space), currentWindowImage.Height));
                windowGraphics.DrawImageUnscaled(currentWindowImage, 0, 0);

                currentWindowImage.Dispose();
                currentWindowGraphics.Dispose();
                windowGraphics.Dispose();
            }
            else
            {
                for (int i = 0; i < changedPixels.Count; i++)
                {
                    ////if (changedPixels[i].PixelPoint.X > currentX)
                    //if (changedPixels[i].PixelPoint.X > 0 &&
                    //    (frameBitmapPixels[changedPixels[i].PixelPoint.X][changedPixels[i].PixelPoint.Y] == frameBitmapPixels[changedPixels[i].PixelPoint.X-1][changedPixels[i].PixelPoint.Y])
                    //)
                    //{
                    //    changedPixels.RemoveAt(i);
                    //    continue;
                    //}
                    changedPixels[i].PixelPoint = new Point(changedPixels[i].PixelPoint.X - 1, changedPixels[i].PixelPoint.Y);
                }
            }

            for (int y = 0; y < frameSize.Height; y++)
            {
                for (int x = 0; x < frameSize.Width - currentX; x++)
                {
                    // DrawLED(x + currentX, y, staticBitmapPixels[currentBitmap][x][y]);
                }



                if (MainWindow.oldAnimation)
                {
                    DrawLED(frameSize.Width - 1, y, staticBitmapPixels[currentBitmap][frameSize.Width - currentX - 1][y]);

                }
                else
                {
                    int x = frameSize.Width - 1;
                    Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][frameSize.Width - currentX - 1][y]);

                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    {
                        frameBitmapPixels[x][y] = c;
                        changedPixels.Add(new ColorAtPixel(c, x, y));
                    }
                }
            }
            if (currentX == 0)
            {
                if (currentBitmap < InputBitmaps.Count - 1)
                {
                    currentBitmap++;
                    currentX = frameSize.Width - 1;
                }
                else
                    lastFrame = true;
            }
            else
                currentX--;
        }

        private void AnimateScrollRight()
        {
            if (lastFrame)
                return;

            if (MainWindow.oldAnimation)
            {
                //Graphics windowGraphics = Graphics.FromImage(parentWindow.windowBitmap);

                //Bitmap currentWindowImage = new Bitmap((frameSize.Width - 1) * (PreviewWindow.LedDiam + PreviewWindow.Space), 
                //    frameSize.Height * (PreviewWindow.LedDiam + PreviewWindow.Space));
                //Graphics currentWindowGraphics = Graphics.FromImage(currentWindowImage);
                //currentWindowGraphics.DrawImageUnscaledAndClipped(parentWindow.windowBitmap, new Rectangle(1 * (PreviewWindow.LedDiam + PreviewWindow.Space), 0, 
                //    currentWindowImage.Width - 1 * (PreviewWindow.LedDiam + PreviewWindow.Space), currentWindowImage.Height));
                //windowGraphics.DrawImageUnscaled(currentWindowImage, 0, 0);

                Graphics windowGraphics = Graphics.FromImage(parentWindow.windowBitmap);


                Bitmap currentWindowImage = new Bitmap((frameSize.Width ) * (PreviewWindow.LedDiam + PreviewWindow.Space),
                    frameSize.Height * (PreviewWindow.LedDiam + PreviewWindow.Space));
                Graphics currentWindowGraphics = Graphics.FromImage(currentWindowImage);
                currentWindowGraphics.DrawImageUnscaledAndClipped(parentWindow.windowBitmap, new Rectangle(1 * (PreviewWindow.LedDiam + PreviewWindow.Space), 0,
                    currentWindowImage.Width - 1 * (PreviewWindow.LedDiam + PreviewWindow.Space), currentWindowImage.Height));
                windowGraphics.DrawImageUnscaled(currentWindowImage, 0, 0);


                currentWindowImage.Dispose();
                currentWindowGraphics.Dispose();
                windowGraphics.Dispose();
            }
            else
            {
                for (int i = 0; i < changedPixels.Count; i++)
                {
                    changedPixels[i].PixelPoint = new Point(changedPixels[i].PixelPoint.X - 1, changedPixels[i].PixelPoint.Y);
                }
            }

            for (int y = 0; y < frameSize.Height; y++)
            {
                for (int x = 0; x < frameSize.Width - currentX; x++)
                {
                    // DrawLED(x + currentX, y, staticBitmapPixels[currentBitmap][x][y]);
                }



                if (MainWindow.oldAnimation)
                {
                    DrawLED(0, y, staticBitmapPixels[currentBitmap][frameSize.Width - currentX - 1][y]);

                }
                else
                {
                    int x = 0;
                    Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][frameSize.Width - currentX - 1][y]);

                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    {
                        frameBitmapPixels[x][y] = c;
                        changedPixels.Add(new ColorAtPixel(c, x, y));
                    }
                }
            }
            if (currentX == frameSize.Width - 1)
            {
                if (currentBitmap > 0)
                {
                    currentBitmap--;
                    currentX = 0;
                }
                else
                    lastFrame = true;
            }
            else
                currentX++;
        }
        private void AnimateScrollLeft()
        {
            if (lastFrame)
                return;

            if (MainWindow.oldAnimation)
            {
                Graphics windowGraphics = Graphics.FromImage(parentWindow.windowBitmap);

                Bitmap currentWindowImage = new Bitmap((frameSize.Width - 1) * (PreviewWindow.LedDiam + PreviewWindow.Space), frameSize.Height * (PreviewWindow.LedDiam + PreviewWindow.Space));
                Graphics currentWindowGraphics = Graphics.FromImage(currentWindowImage);
                currentWindowGraphics.DrawImageUnscaledAndClipped(parentWindow.windowBitmap, new Rectangle(-1 * (PreviewWindow.LedDiam + PreviewWindow.Space), 0, currentWindowImage.Width + 1 * (PreviewWindow.LedDiam + PreviewWindow.Space), currentWindowImage.Height));
                windowGraphics.DrawImageUnscaled(currentWindowImage, 0, 0);

                currentWindowImage.Dispose();
                currentWindowGraphics.Dispose();
                windowGraphics.Dispose();
            }
            else
            {
                for (int i = 0; i < changedPixels.Count; i++)
                {
                    ////if (changedPixels[i].PixelPoint.X > currentX)
                    //if (changedPixels[i].PixelPoint.X > 0 &&
                    //    (frameBitmapPixels[changedPixels[i].PixelPoint.X][changedPixels[i].PixelPoint.Y] == frameBitmapPixels[changedPixels[i].PixelPoint.X-1][changedPixels[i].PixelPoint.Y])
                    //)
                    //{
                    //    changedPixels.RemoveAt(i);
                    //    continue;
                    //}
                    changedPixels[i].PixelPoint = new Point(changedPixels[i].PixelPoint.X - 1, changedPixels[i].PixelPoint.Y);
                }
            }

            for (int y = 0; y < frameSize.Height; y++)
            {
                for (int x = 0; x < frameSize.Width - currentX; x++)
                {
                    // DrawLED(x + currentX, y, staticBitmapPixels[currentBitmap][x][y]);
                }



                if (MainWindow.oldAnimation)
                {
                    DrawLED(frameSize.Width - 1, y, staticBitmapPixels[currentBitmap][frameSize.Width - currentX - 1][y]);

                }
                else
                {
                    int x = frameSize.Width - 1;
                    Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][frameSize.Width - currentX - 1][y]);

                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    {
                        frameBitmapPixels[x][y] = c;
                        changedPixels.Add(new ColorAtPixel(c, x, y));
                    }
                }
            }
            if (currentX == 0)
            {
                if (currentBitmap < InputBitmaps.Count - 1)
                {
                    currentBitmap++;
                    currentX = frameSize.Width - 1;
                }
                else
                    lastFrame = true;
            }
            else
                currentX--;
        }

        private void AnimateScrollUp()
        {
            if (lastFrame)
                return;

            if (MainWindow.oldAnimation)
            {
                Graphics windowGraphics = Graphics.FromImage(parentWindow.windowBitmap);

                Bitmap currentWindowImage = new Bitmap(((frameSize.Width) * (PreviewWindow.LedDiam + PreviewWindow.Space)),
                    //parentWindow.windowBitmap.Height - 
                    ((frameSize.Height - 1)  * (PreviewWindow.LedDiam + PreviewWindow.Space)));
                Graphics currentWindowGraphics = Graphics.FromImage(currentWindowImage);
                currentWindowGraphics.DrawImageUnscaledAndClipped(parentWindow.windowBitmap, new Rectangle(0, -1 * (PreviewWindow.LedDiam + PreviewWindow.Space),
                    currentWindowImage.Width, currentWindowImage.Height + 1 * (PreviewWindow.LedDiam + PreviewWindow.Space)));
                windowGraphics.DrawImageUnscaled(currentWindowImage, 0, 0);

                currentWindowImage.Dispose();
                currentWindowGraphics.Dispose();
                windowGraphics.Dispose();
            }
            else
            {
                for (int i = 0; i < changedPixels.Count; i++)
                {
                    //if (changedPixels[i].PixelPoint.X > currentX)
                    changedPixels[i].PixelPoint = new Point(changedPixels[i].PixelPoint.X, changedPixels[i].PixelPoint.Y - 1);
                }
            }



            for (int x = 0; x < frameSize.Width; x++)
            {
                for (int y = 0; y < frameSize.Height - currentY; y++)
                {
                    //if (MainWindow.oldAnimation)
                    //{
                    //    DrawLED(x, y + currentY, staticBitmapPixels[currentBitmap][x][y]);
                    //}
                    //else
                    //{
                    //    Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                    //    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    //    {
                    //        frameBitmapPixels[x][y + currentY] = c;
                    //        changedPixels.Add(new ColorAtPixel(c, x, y + currentY));
                    //    }
                    //}
                }

                if (MainWindow.oldAnimation)
                {
                    DrawLED(x, frameSize.Height - 1, staticBitmapPixels[currentBitmap][x][frameSize.Height - currentY - 1]);
                }
                else
                {
                    int y = frameSize.Height - 1;
                    Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][frameSize.Height - currentY - 1]);

                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    {
                        frameBitmapPixels[x][y] = c;
                        changedPixels.Add(new ColorAtPixel(c, x, y));
                    }
                }
            }
            if (currentY == 0)
            {
                if (currentBitmap < InputBitmaps.Count - 1)
                {
                    currentBitmap++;
                    currentY = frameSize.Height - 1;
                }
                else
                    lastFrame = true;
            }
            else
                currentY--;
        }
        private void AnimateScrollDown()
        {
            if (lastFrame)
                return;

            if (MainWindow.oldAnimation)
            {
                Graphics windowGraphics = Graphics.FromImage(parentWindow.windowBitmap);

                Bitmap currentWindowImage = new Bitmap(((frameSize.Width) * (PreviewWindow.LedDiam + PreviewWindow.Space)),
                    (frameSize.Height * (PreviewWindow.LedDiam + PreviewWindow.Space))
                    //parentWindow.windowBitmap.Height// - (1 * (PreviewWindow.LedDiam + PreviewWindow.Space))
                    );
                Graphics currentWindowGraphics = Graphics.FromImage(currentWindowImage);
                currentWindowGraphics.DrawImageUnscaledAndClipped(parentWindow.windowBitmap, new Rectangle(0, 1 * (PreviewWindow.LedDiam + PreviewWindow.Space),
                    currentWindowImage.Width, currentWindowImage.Height// + 1 * (PreviewWindow.LedDiam + PreviewWindow.Space)
                    ));
                windowGraphics.DrawImageUnscaled(currentWindowImage, 0, 0);

                currentWindowImage.Dispose();
                currentWindowGraphics.Dispose();
                windowGraphics.Dispose();
            }
            else
            {
                for (int i = 0; i < changedPixels.Count; i++)
                {
                    //if (changedPixels[i].PixelPoint.X > currentX)
                    changedPixels[i].PixelPoint = new Point(changedPixels[i].PixelPoint.X, changedPixels[i].PixelPoint.Y + 1);
                }
            }

            for (int x = 0; x < frameSize.Width; x++)
            {
                for (int y = 0; y < frameSize.Height - currentY; y++)
                {
                    //DrawLED(x, y + currentY, staticBitmapPixels[currentBitmap][x][y]);
                }
                DrawLED(x, 0, staticBitmapPixels[currentBitmap][x][frameSize.Height - currentY - 1]);


                if (MainWindow.oldAnimation)
                {
                    DrawLED(x, 0, staticBitmapPixels[currentBitmap][x][frameSize.Height - currentY - 1]);
                }
                else
                {
                    int y = 0;
                    Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][frameSize.Height - currentY - 1]);

                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    {
                        frameBitmapPixels[x][y] = c;
                        changedPixels.Add(new ColorAtPixel(c, x, y));
                    }
                }
            }
            if (currentY == frameSize.Height - 1)
            {
                if (currentBitmap < InputBitmaps.Count - 1)
                {
                    currentBitmap++;
                    currentY = 0;
                }
                else
                    lastFrame = true;
            }
            else
                currentY++;
        }
        private void AnimateScrollUpCont()
        {
            if (lastFrame)
                return;

            if (MainWindow.oldAnimation)
            {
                Graphics windowGraphics = Graphics.FromImage(parentWindow.windowBitmap);

                Bitmap currentWindowImage = new Bitmap(((frameSize.Width) * (PreviewWindow.LedDiam + PreviewWindow.Space)),
                    //parentWindow.windowBitmap.Height - 
                    ((frameSize.Height - 1) * (PreviewWindow.LedDiam + PreviewWindow.Space)));
                Graphics currentWindowGraphics = Graphics.FromImage(currentWindowImage);
                currentWindowGraphics.DrawImageUnscaledAndClipped(parentWindow.windowBitmap, new Rectangle(0, -1 * (PreviewWindow.LedDiam + PreviewWindow.Space),
                    currentWindowImage.Width, currentWindowImage.Height + 1 * (PreviewWindow.LedDiam + PreviewWindow.Space)));
                windowGraphics.DrawImageUnscaled(currentWindowImage, 0, 0);

                currentWindowImage.Dispose();
                currentWindowGraphics.Dispose();
                windowGraphics.Dispose();
            }
            else
            {
                for (int i = 0; i < changedPixels.Count; i++)
                {
                    //if (changedPixels[i].PixelPoint.X > currentX)
                    changedPixels[i].PixelPoint = new Point(changedPixels[i].PixelPoint.X, changedPixels[i].PixelPoint.Y - 1);
                }
            }



            for (int x = 0; x < frameSize.Width; x++)
            {
                for (int y = 0; y < frameSize.Height - currentY; y++)
                {
                    //if (MainWindow.oldAnimation)
                    //{
                    //    DrawLED(x, y + currentY, staticBitmapPixels[currentBitmap][x][y]);
                    //}
                    //else
                    //{
                    //    Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                    //    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    //    {
                    //        frameBitmapPixels[x][y + currentY] = c;
                    //        changedPixels.Add(new ColorAtPixel(c, x, y + currentY));
                    //    }
                    //}
                }

                if (MainWindow.oldAnimation)
                {
                    DrawLED(x, frameSize.Height - 1, staticBitmapPixels[currentBitmap][x][frameSize.Height - currentY - 1]);
                }
                else
                {
                    int y = frameSize.Height - 1;
                    Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][frameSize.Height - currentY - 1]);

                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    {
                        frameBitmapPixels[x][y] = c;
                        changedPixels.Add(new ColorAtPixel(c, x, y));
                    }
                }
            }
            if (currentY == 0)
            {
                if (currentBitmap < InputBitmaps.Count - 1)
                {
                    currentBitmap++;
                    currentY = frameSize.Height - 1;
                }
                else
                    lastFrame = true;
            }
            else
                currentY--;
        }
        private void AnimateScrollDownCont()
        {
            if (lastFrame)
                return;

            if (MainWindow.oldAnimation)
            {
                Graphics windowGraphics = Graphics.FromImage(parentWindow.windowBitmap);

                Bitmap currentWindowImage = new Bitmap(((frameSize.Width) * (PreviewWindow.LedDiam + PreviewWindow.Space)),
                    ((frameSize.Height) * (PreviewWindow.LedDiam + PreviewWindow.Space))
                    //parentWindow.windowBitmap.Height// - (1 * (PreviewWindow.LedDiam + PreviewWindow.Space))
                    );
                Graphics currentWindowGraphics = Graphics.FromImage(currentWindowImage);
                currentWindowGraphics.DrawImageUnscaledAndClipped(parentWindow.windowBitmap, new Rectangle(0, 1 * (PreviewWindow.LedDiam + PreviewWindow.Space),
                    currentWindowImage.Width, currentWindowImage.Height //+ 1 * (PreviewWindow.LedDiam + PreviewWindow.Space)
                    )
                    );
                windowGraphics.DrawImageUnscaled(currentWindowImage, 0, 0);

                currentWindowImage.Dispose();
                currentWindowGraphics.Dispose();
                windowGraphics.Dispose();
            }
            else
            {
                for (int i = 0; i < changedPixels.Count; i++)
                {
                    //if (changedPixels[i].PixelPoint.X > currentX)
                    changedPixels[i].PixelPoint = new Point(changedPixels[i].PixelPoint.X, changedPixels[i].PixelPoint.Y + 1);
                }
            }

            for (int x = 0; x < frameSize.Width; x++)
            {
                for (int y = 0; y < frameSize.Height - currentY; y++)
                {
                    //DrawLED(x, y + currentY, staticBitmapPixels[currentBitmap][x][y]);
                }
                DrawLED(x, 0, staticBitmapPixels[currentBitmap][x][frameSize.Height - currentY - 1]);


                if (MainWindow.oldAnimation)
                {
                    DrawLED(x, 0, staticBitmapPixels[currentBitmap][x][frameSize.Height - currentY - 1]);
                }
                else
                {
                    int y = 0;
                    Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][frameSize.Height - currentY - 1]);

                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    {
                        frameBitmapPixels[x][y] = c;
                        changedPixels.Add(new ColorAtPixel(c, x, y));
                    }
                }
            }
            if (currentY == frameSize.Height - 1)
            {
                if (currentBitmap < InputBitmaps.Count - 1)
                {
                    currentBitmap++;
                    currentY = 0;
                }
                else
                    lastFrame = true;
            }
            else
                currentY++;
        }
        #endregion
        #region Clock
        private void AnimateOpenClock()
        {
            if (lastFrame)
                return;

            DateTime start = DateTime.Now;


            Bitmap clockBitmap = new Bitmap(Convert.ToInt32(Math.Round(clockRadius + 1, 0)) * 2, Convert.ToInt32(Math.Round(clockRadius + 1, 0)) * 2);
            newAngle = ++angleStep * 360 / 16.0;

            Graphics clockGraphics = Graphics.FromImage(clockBitmap);

            clockGraphics.FillPie(new SolidBrush(maskColor),
                frameSize.Width / 2 - roundedClockRadius,
                frameSize.Height / 2 - roundedClockRadius,
                roundedClockRadius * 2, roundedClockRadius * 2,
                270, clockDirection * (float)newAngle);

            clockGraphics.Dispose();

            for (int x = 0; x < frameSize.Width; x++)
            {
                for (int y = 0; y < frameSize.Height; y++)
                {
                    if (x < frameSize.Width && y < frameSize.Height)
                    {
                        Color clockColor = clockBitmap.GetPixel(x, y);
                        if (clockColor == maskColor)
                        {

                            Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                            if (MainWindow.oldAnimation)
                                DrawLED(x, y, c);
                            else
                            {
                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                            
                        }
                    }
                }
            }

            clockBitmap.Dispose();

            if (newAngle == 360)
                lastFrame = true;
            else
                oldAngle = newAngle;


            DateTime end = DateTime.Now;
            //MessageBox.Show((((TimeSpan)(end - start)).TotalMilliseconds).ToString(), "AnimateInstant");
            //Console.WriteLine("Clock - {0}ms", (((TimeSpan)(end - start)).TotalMilliseconds).ToString());
        }
        private void AnimateWindmill()
        {
            if (lastFrame)
                return;

            double clockRadius = Math.Pow((Math.Pow(frameSize.Width / 2, 2) + Math.Pow(frameSize.Height / 2, 2)), 0.5);
            int roundedClockRadius = Convert.ToInt32(Math.Round(clockRadius + 1, 0));
            Bitmap clockBitmap = new Bitmap(Convert.ToInt32(Math.Round(clockRadius + 1, 0)) * 2, Convert.ToInt32(Math.Round(clockRadius + 1, 0)) * 2);
            newAngle = ++angleStep * 90 / 4.0;

            Graphics clockGraphics = Graphics.FromImage(clockBitmap);

            clockGraphics.FillPie(new SolidBrush(maskColor),
                frameSize.Width / 2 - roundedClockRadius,
                frameSize.Height / 2 - roundedClockRadius,
                roundedClockRadius * 2, roundedClockRadius * 2,
                270, clockDirection * (float)newAngle);

            clockGraphics.FillPie(new SolidBrush(maskColor),
                frameSize.Width / 2 - roundedClockRadius,
                frameSize.Height / 2 - roundedClockRadius,
                roundedClockRadius * 2, roundedClockRadius * 2,
                0, clockDirection * (float)newAngle);

            clockGraphics.FillPie(new SolidBrush(maskColor),
                frameSize.Width / 2 - roundedClockRadius,
                frameSize.Height / 2 - roundedClockRadius,
                roundedClockRadius * 2, roundedClockRadius * 2,
                90, clockDirection * (float)newAngle);

            clockGraphics.FillPie(new SolidBrush(maskColor),
                frameSize.Width / 2 - roundedClockRadius,
                frameSize.Height / 2 - roundedClockRadius,
                roundedClockRadius * 2, roundedClockRadius * 2,
                180, clockDirection * (float)newAngle);


            clockGraphics.Dispose();

            for (int x = 0; x < frameSize.Width; x++)
            {
                for (int y = 0; y < frameSize.Height; y++)
                {
                    if (x < frameSize.Width && y < frameSize.Height)
                    {
                        Color currentClockPixelColor = clockBitmap.GetPixel(x, y);
                        if (currentClockPixelColor == maskColor)
                        {
                            Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                            if (MainWindow.oldAnimation)
                                DrawLED(x, y, c);
                            else
                            {
                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                    }
                }
            }

            clockBitmap.Dispose();

            if (newAngle == 90)
                lastFrame = true;
            else
                oldAngle = newAngle;
        }
        private void AnimateOpen2Fan()
        {
            if (lastFrame)
                return;

            Bitmap clockBitmap = new Bitmap(Convert.ToInt32(Math.Round(clockRadius + 1, 0)) * 2, Convert.ToInt32(Math.Round(clockRadius + 1, 0)) * 2);
            newAngle = ++angleStep * 360 / 16.0;

            Graphics clockGraphics = Graphics.FromImage(clockBitmap);

            clockGraphics.FillPie(new SolidBrush(maskColor),
                frameSize.Width / 2 - roundedClockRadius,
                frameSize.Height / 2 - roundedClockRadius,
                roundedClockRadius * 2, roundedClockRadius * 2,
                270 - clockDirection * (float)newAngle, 2* clockDirection * (float)newAngle);

            clockGraphics.Dispose();

            for (int x = 0; x < frameSize.Width; x++)
            {
                for (int y = 0; y < frameSize.Height; y++)
                {
                    if (x < frameSize.Width && y < frameSize.Height)
                    {
                        Color clockColor = clockBitmap.GetPixel(x, y);
                        if (clockColor == maskColor)
                        {

                            Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                            if (MainWindow.oldAnimation)
                                DrawLED(x, y, c);
                            else
                            {
                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }

                        }
                    }
                }
            }

            clockBitmap.Dispose();

            if (newAngle == 360)
                lastFrame = true;
            else
                oldAngle = newAngle;
        }
        
        #endregion
        #region From center
        private void AnimateRectangleOut()
        {
            if (lastFrame)
                return;

            if (frameSize.Width > frameSize.Height)
            {
                //ratio = frameSize.Width / frameSize.Height;
                if (plusX == borderX)
                    plusY++;
                plusX++;
                if (plusX >= borderX)
                    borderX = ratio * plusY;
            }
            else if ( frameSize.Height >= frameSize.Width)
            {
                //ratio =  frameSize.Height / frameSize.Width;
                if (plusY == borderY)
                    plusX++;
                plusY++;
                if (plusY >= borderY)
                    borderY = ratio * plusX;
            }
            newRect = Rectangle.FromLTRB(center.X - plusX, center.Y - plusY, center.X + plusX, center.Y + plusY);

            for (int x = center.X - plusX; x <= center.X + plusX; x++)
            {
                for (int y = center.Y - plusY; y <= center.Y + plusY; y++)
                {
                    if ((x >= 0 && x < frameSize.Width)
                        && (y >= 0 && y < frameSize.Height))
                    {
                        if (newRect.Contains(x, y) && !oldRect.Contains(x, y))
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, y, staticBitmapPixels[currentBitmap][x][y]);
                            else
                            {
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                    }
                }
            }
            if (newRect.Contains(0, 0) && newRect.Contains(0, frameSize.Height - 1)
                && newRect.Contains(frameSize.Width - 1, 0)
                && newRect.Contains(frameSize.Width - 1, frameSize.Height - 1))
            {
                lastFrame = true;
            }
            else
            {
                oldRect = newRect;
            }
        }
        private void AnimateRectangleIn()
        {
            if (lastFrame)
                return;

            if (frameSize.Width > frameSize.Height)
            {
                //ratio = frameSize.Width / frameSize.Height;
                if (plusX == borderX)
                    plusY++;
                plusX++;
                if (plusX >= borderX)
                    borderX = ratio * plusY;
            }
            else if ( frameSize.Height >= frameSize.Width)
            {
                //ratio =  frameSize.Height / frameSize.Width;
                if (plusY == borderY)
                    plusX++;
                plusY++;
                if (plusY >= borderY)
                    borderY = ratio * plusX;
            }

            int tranformX = (frameSize.Width /2) - plusX;
            int tranformY = (frameSize.Height/2) - plusY;


            newRect = Rectangle.FromLTRB(center.X - tranformX, center.Y - tranformY, center.X + tranformX, center.Y + tranformY);

            //for (int x = center.X - tranformX; x <= center.X + tranformX; x++)
            //{
            //    for (int y = center.Y - tranformY; y <= center.Y + tranformY; y++)
            //    {
            for (int x = 0; x <= frameSize.Width; x++)
            {
                for (int y = 0; y <= frameSize.Height; y++)
                {
                    if ((x >= 0 && x < frameSize.Width)
                        && (y >= 0 && y < frameSize.Height))
                    {
                        if (!newRect.Contains(x, y))// && oldRect.Contains(x, y))
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, y, staticBitmapPixels[currentBitmap][x][y]);
                            else
                            {
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                    }
                }
            }
            if (tranformX <= 0 && tranformY <= 0)
            {
                lastFrame = true;
            }
            else
            {
                oldRect = newRect;
            }
        }
        private void AnimateCornerOut()
        {
            if (lastFrame)
                return;

            if (frameSize.Width > frameSize.Height)
            {
                //ratiof = frameSize.Width / (float)frameSize.Height;
                if (plusX >= borderXf)
                    plusY++;
                plusX++;
                if (plusX >= borderXf)
                {
                    borderXf = ratiof * plusY;
                    
                    //if (modulo > 0)
                    //{
                    //    modulo--;
                    //    borderXf++;
                    //}
                }
            }
            else if ( frameSize.Height >= frameSize.Width)
            {
                //ratiof = frameSize.Height / (float)frameSize.Width;
                if (plusY >= borderYf)
                    plusX++;
                plusY++;
                if (plusY >= borderY)
                {
                    borderY = ratio * plusX;

                    //if (modulo > 0)
                    //{
                    //    modulo--;
                    //    borderY++;
                    //}
                }
            }

            double[] xtab = new double[4] { center.X - plusX, center.X, center.X + plusX, center.X };
            double[] ytab = new double[4] { center.Y, center.Y - plusY, center.Y, center.Y + plusY };

            for (int x = center.X - plusX; x <= center.X + plusX; x++)
            {
                for (int y = center.Y - plusY; y <= center.Y + plusY; y++)
                {
                    if ((x >= 0 && x < frameSize.Width)
                        && (y >= 0 && y < frameSize.Height))
                    {

                        //if (newRect.Contains(x, y) && !oldRect.Contains(x, y))
                        if (IsPointInPolygon(4, xtab, ytab, x, y))
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, y, staticBitmapPixels[currentBitmap][x][y]);
                            else
                            {
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                    }
                }
            }

            if ((IsPointInPolygon(4, xtab, ytab, 0, 0))
                && (IsPointInPolygon(4, xtab, ytab, 0, frameSize.Height - 1))
                && (IsPointInPolygon(4, xtab, ytab, frameSize.Width - 1, 0))
                && (IsPointInPolygon(4, xtab, ytab, frameSize.Width - 1, frameSize.Height - 1)))
            {
                lastFrame = true;
            }
        }
        private void AnimateCornerIn()
        {
            if (lastFrame)
                return;

            if (frameSize.Width > frameSize.Height)
            {
                //ratiof = frameSize.Width / (float)frameSize.Height;
                if (plusX >= borderXf)
                    plusY++;
                plusX++;
                if (plusX >= borderXf)
                {
                    borderXf = ratiof * plusY;

                    //if (modulo > 0)
                    //{
                    //    modulo--;
                    //    borderXf++;
                    //}
                }
            }
            else if ( frameSize.Height >= frameSize.Width)
            {
                //ratiof = frameSize.Height / (float)frameSize.Width;
                if (plusY >= borderYf)
                    plusX++;
                plusY++;
                if (plusY >= borderY)
                {
                    borderY = ratio * plusX;

                    //if (modulo > 0)
                    //{
                    //    modulo--;
                    //    borderY++;
                    //}
                }
            }

            
            int tranformX = (frameSize.Width) - plusX;
            int tranformY = (frameSize.Height) - plusY;

            double[] xtab = new double[4] { center.X - tranformX, center.X, center.X + tranformX, center.X };
            double[] ytab = new double[4] { center.Y, center.Y - tranformY, center.Y, center.Y + tranformY };

            for (int x = 0; x <= frameSize.Width; x++)
            {
                for (int y = 0; y <= frameSize.Height; y++)
                {
                    if ((x >= 0 && x < frameSize.Width)
                        && (y >= 0 && y < frameSize.Height))
                    {

                        //if (newRect.Contains(x, y) && !oldRect.Contains(x, y))
                        if (!IsPointInPolygon(4, xtab, ytab, x, y))
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, y, staticBitmapPixels[currentBitmap][x][y]);
                            else
                            {
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                    }
                }
            }

            //if ((IsPointInPolygon(4, xtab, ytab, 0, 0))
            //    && (IsPointInPolygon(4, xtab, ytab, 0, frameSize.Height - 1))
            //    && (IsPointInPolygon(4, xtab, ytab, frameSize.Width - 1, 0))
            //    && (IsPointInPolygon(4, xtab, ytab, frameSize.Width - 1, frameSize.Height - 1)))
            if(tranformX <= 0 && tranformY <= 0)
            {
                lastFrame = true;
            }
        }
        private void AnimateRoundOut()
        {
            if (lastFrame)
                return;

            if(!MainWindow.oldAnimation)
                ClearLEDs(null);
            
            if (frameSize.Width > frameSize.Height)
            {
                //ratio = frameSize.Width / frameSize.Height;
                if (plusX == borderX)
                    plusY++;
                plusX++;
                if (plusX >= borderX)
                    borderX = ratio * plusY;
            }
            else if ( frameSize.Height >= frameSize.Width)
            {
                //ratio =  frameSize.Height / frameSize.Width;
                if (plusY == borderY)
                    plusX++;
                plusY++;
                if (plusY >= borderY)
                    borderY = ratio * plusX;
            }

            //lock (InputBitmaps[currentBitmap])
            {
               // System.Drawing.Imaging.BitmapData bd =
               //     InputBitmaps[currentBitmap].LockBits(new Rectangle(new Point(0, 0), InputBitmaps[currentBitmap].Size), System.Drawing.Imaging.ImageLockMode.ReadOnly, InputBitmaps[currentBitmap].PixelFormat);
                for (int x = center.X - plusX; x <= center.X + plusX; x++)
                {
                    for (int y = center.Y - plusY; y <= center.Y + plusY; y++)
                    {
                        if ((Math.Pow((x - center.X), 2) / Math.Pow(plusX, 2)) + (Math.Pow((y - center.Y), 2) / Math.Pow(plusY, 2)) <= 1)
                        {
                            if ((x >= 0 && x < frameSize.Width)
                                && (y >= 0 && y < frameSize.Height))
                            {
                                
                                if (MainWindow.oldAnimation)
                                    DrawLED(x, y, staticBitmapPixels[currentBitmap][x][y]);
                                else
                                {
                                    Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                                    //if (frameBitmapPixels[x][y].ToArgb() != c.ToArgb())
                                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                    //if ((frameBitmapPixels[x][y].ToArgb() != c.ToArgb())
                                    //    && (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb()))
                                    {
                                        frameBitmapPixels[x][y] = c;
                                        changedPixels.Add(new ColorAtPixel(c, x, y));
                                    }
                                }
                            }
                        }
                    }
                }
                //InputBitmaps[currentBitmap].UnlockBits(bd);
            }

            if (((Math.Pow((0 - center.X), 2) / Math.Pow(plusX, 2)) + (Math.Pow((0 - center.Y), 2) / Math.Pow(plusY, 2)) <= 1)
                && ((Math.Pow((0 - center.X), 2) / Math.Pow(plusX, 2)) + (Math.Pow((frameSize.Height - 1 - center.Y), 2) / Math.Pow(plusY, 2)) <= 1)
                && ((Math.Pow((frameSize.Width - 1 - center.X), 2) / Math.Pow(plusX, 2)) + (Math.Pow((0 - center.Y), 2) / Math.Pow(plusY, 2)) <= 1)
                && ((Math.Pow((frameSize.Width - 1 - center.X), 2) / Math.Pow(plusX, 2)) + (Math.Pow((frameSize.Height - 1 - center.Y), 2) / Math.Pow(plusY, 2)) <= 1))
            {
                lastFrame = true;
            }
        }
        
        #endregion
        #region Slides
        int gcd(int a, int b)
        {
            return (b != 0) ? gcd(b, a % b) : a;
        }

        private void MoveStripeLeft(int w, int x, int h, int y)
        {
            if (h > frameSize.Height - y)
                h = frameSize.Height - y;

            using (Bitmap currentWindowImage = new Bitmap((w - 1) * (PreviewWindow.LedDiam + PreviewWindow.Space),
                    h * (PreviewWindow.LedDiam + PreviewWindow.Space)))
            {
                using (Graphics windowGraphics = Graphics.FromImage(parentWindow.windowBitmap), currentWindowGraphics = Graphics.FromImage(currentWindowImage))
                {
                    currentWindowGraphics.DrawImageUnscaledAndClipped(parentWindow.windowBitmap, 
                        new Rectangle((x-1) * (PreviewWindow.LedDiam + PreviewWindow.Space), 
                            (-y) * (PreviewWindow.LedDiam + PreviewWindow.Space), 
                            currentWindowImage.Width + 1 * (PreviewWindow.LedDiam + PreviewWindow.Space),
                            ((frameSize.Height) * (PreviewWindow.LedDiam + PreviewWindow.Space))
                            //parentWindow.windowBitmap.Height
                            ));

                    windowGraphics.DrawImageUnscaled(currentWindowImage, x * (PreviewWindow.LedDiam + PreviewWindow.Space), y * (PreviewWindow.LedDiam + PreviewWindow.Space));

                }
            }

            for (int localy = y; localy < y+h; localy++)
            {
                if (MainWindow.oldAnimation)
                {
                    DrawLED(w - 1, localy, staticBitmapPixels[currentBitmap][w - currentX - 1][localy]);
                }
                else
                {
                    int localx = w - 1;
                    Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][w - currentX - 1][localy]);

                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    {
                        frameBitmapPixels[localx][localy] = c;
                        changedPixels.Add(new ColorAtPixel(c, localx, localy));
                    }
                }
            }

        }
        private void MoveStripeRight(int w, int x, int h, int y)
        {
            if (h > frameSize.Height - y)
                h = frameSize.Height - y;

            using (Bitmap currentWindowImage = new Bitmap((w - 1) * (PreviewWindow.LedDiam + PreviewWindow.Space),
                    h * (PreviewWindow.LedDiam + PreviewWindow.Space)))
            {
                using (Graphics windowGraphics = Graphics.FromImage(parentWindow.windowBitmap), currentWindowGraphics = Graphics.FromImage(currentWindowImage))
                {
                    currentWindowGraphics.DrawImageUnscaledAndClipped(parentWindow.windowBitmap,
                        new Rectangle((x) * (PreviewWindow.LedDiam + PreviewWindow.Space),
                            (-y) * (PreviewWindow.LedDiam + PreviewWindow.Space),
                            currentWindowImage.Width,// + 1 * (PreviewWindow.LedDiam + PreviewWindow.Space),
                            ((frameSize.Height) * (PreviewWindow.LedDiam + PreviewWindow.Space))
                            //parentWindow.windowBitmap.Height
                            ));

                    windowGraphics.DrawImageUnscaled(currentWindowImage, (x+1) * (PreviewWindow.LedDiam + PreviewWindow.Space), y * (PreviewWindow.LedDiam + PreviewWindow.Space));

                }
            }

            for (int localy = y; localy < y + h; localy++)
            {
                if (MainWindow.oldAnimation)
                {
                    DrawLED(0, localy, staticBitmapPixels[currentBitmap][currentX][localy]);
                }
                else
                {
                    int localx = 0;
                    Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][ currentX][localy]);

                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    {
                        frameBitmapPixels[localx][localy] = c;
                        changedPixels.Add(new ColorAtPixel(c, localx, localy));
                    }
                }
            }

        }
        private void MoveStripeUp(int w, int x, int h, int y)
        {
            if (w > frameSize.Width- x)
                w = frameSize.Width - x;

            using (Bitmap currentWindowImage = new Bitmap((w) * (PreviewWindow.LedDiam + PreviewWindow.Space),
                    (h-1) * (PreviewWindow.LedDiam + PreviewWindow.Space)))
            {
                using (Graphics windowGraphics = Graphics.FromImage(parentWindow.windowBitmap), currentWindowGraphics = Graphics.FromImage(currentWindowImage))
                {
                    currentWindowGraphics.DrawImageUnscaledAndClipped(parentWindow.windowBitmap,
                        new Rectangle((-x) * (PreviewWindow.LedDiam + PreviewWindow.Space),
                            (y-1) * (PreviewWindow.LedDiam + PreviewWindow.Space),
                            ((frameSize.Width) * (PreviewWindow.LedDiam + PreviewWindow.Space)),
                            currentWindowImage.Height + 1 * (PreviewWindow.LedDiam + PreviewWindow.Space)));

                    windowGraphics.DrawImageUnscaled(currentWindowImage, x * (PreviewWindow.LedDiam + PreviewWindow.Space), y * (PreviewWindow.LedDiam + PreviewWindow.Space));

                }
            }

            for (int localx = x; localx < x + w; localx++)
            {
                if (MainWindow.oldAnimation)
                {
                    DrawLED(localx, h-1, staticBitmapPixels[currentBitmap][localx][h - currentY - 1]);
                }
                else
                {
                    int localy = h - 1;
                    Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][localx][h - currentY - 1]);

                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    {
                        frameBitmapPixels[localx][localy] = c;
                        changedPixels.Add(new ColorAtPixel(c, localx, localy));
                    }
                }
            }

        }
        private void MoveStripeDown(int w, int x, int h, int y)
        {
            if (w > frameSize.Width - x)
                w = frameSize.Width - x;

            using (Bitmap currentWindowImage = new Bitmap((w) * (PreviewWindow.LedDiam + PreviewWindow.Space),
                    (h - 1) * (PreviewWindow.LedDiam + PreviewWindow.Space)))
            {
                using (Graphics windowGraphics = Graphics.FromImage(parentWindow.windowBitmap), currentWindowGraphics = Graphics.FromImage(currentWindowImage))
                {
                    currentWindowGraphics.DrawImageUnscaledAndClipped(parentWindow.windowBitmap,
                        new Rectangle((-x) * (PreviewWindow.LedDiam + PreviewWindow.Space),
                            (y) * (PreviewWindow.LedDiam + PreviewWindow.Space),
                            ((frameSize.Width) * (PreviewWindow.LedDiam + PreviewWindow.Space)),
                            currentWindowImage.Height// + 1 * (PreviewWindow.LedDiam + PreviewWindow.Space)
                            ));

                    windowGraphics.DrawImageUnscaled(currentWindowImage, x * (PreviewWindow.LedDiam + PreviewWindow.Space), (y+1) * (PreviewWindow.LedDiam + PreviewWindow.Space));

                }
            }

            for (int localx = x; localx < x + w; localx++)
            {
                if (MainWindow.oldAnimation)
                {
                    DrawLED(localx, 0, staticBitmapPixels[currentBitmap][localx][currentY]);
                }
                else
                {
                    int localy = 0;
                    Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][localx][currentY]);

                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    {
                        frameBitmapPixels[localx][localy] = c;
                        changedPixels.Add(new ColorAtPixel(c, localx, localy));
                    }
                }
            }
        }

        private void AnimateSlideZebraHorizontal()
        {
            if (lastFrame)
                return;

            if (MainWindow.oldAnimation)
            {
                //if (frameSize.Width - currentX - 1 < currentX)
                //{
                //    MoveStripeLeft(currentX, 0, frameSize.Height, 0);
                //    MoveStripeRight(frameSize.Width - currentX - 1, 0, frameSize.Height, 0);
                //}
                //else
                {
                    for (int y = 0; y < frameSize.Height; y += 8)
                        MoveStripeLeft(frameSize.Width, 0, 4, y);

                    for (int y = 4; y < frameSize.Height; y += 8)
                        MoveStripeRight(frameSize.Width, 0, 4, y);
                }
            }
            else
            {
                for (int i = 0; i < changedPixels.Count; i++)
                {
                    //if (changedPixels[i].PixelPoint.X > currentX)
                    changedPixels[i].PixelPoint = new Point(changedPixels[i].PixelPoint.X - 1, changedPixels[i].PixelPoint.Y);
                }
            }

            if (currentX == 0)
            {
                lastFrame = true;
            }
            else
                currentX--;
        }
        private void AnimateSlideZebraVertical()
        {
            if (lastFrame)
                return;

            if (MainWindow.oldAnimation)
            {
                //if (frameSize.Width - currentX - 1 < currentX)
                //{
                //    MoveStripeLeft(currentX, 0, frameSize.Height, 0);
                //    MoveStripeRight(frameSize.Width - currentX - 1, 0, frameSize.Height, 0);
                //}
                //else
                {
                    for (int x = 0; x < frameSize.Width; x += 8)
                        MoveStripeUp(4, x, frameSize.Height, 0);

                    for (int x = 4; x < frameSize.Width; x += 8)
                        MoveStripeDown(4, x, frameSize.Height, 0);
                }
            }
            else
            {
                for (int i = 0; i < changedPixels.Count; i++)
                {
                    //if (changedPixels[i].PixelPoint.Y > currentY)
                    changedPixels[i].PixelPoint = new Point(changedPixels[i].PixelPoint.X, changedPixels[i].PixelPoint.Y - 1);
                }
            }

            if (currentY == 0)
            {
                lastFrame = true;
            }
            else
                currentY--;
        }

        private void AnimateSlideTopLeft()
        {
            if (lastFrame)
                return;

            if (!MainWindow.oldAnimation)
                ClearLEDs(null);

            if (frameSize.Width > frameSize.Height)
            {
                //ratio = frameSize.Width / frameSize.Height;
                if (plusX >= borderX)
                    plusY++;
                plusX++;
                if (modulo > 0)
                {
                    modulo--;
                    plusX++;
                }


                if (plusX >= borderX)
                    borderX = ratio * plusY;
            }
            else if ( frameSize.Height >= frameSize.Width)
            {
                //ratio =  frameSize.Height / frameSize.Width;
                if (plusY >= borderY)
                    plusX++;
                plusY++;


                if (modulo > 0)
                {
                    modulo--;
                    plusY++;
                }

                if (plusY >= borderY)
                    borderY = ratio * plusX;
            }

            if (InputBitmaps != null && InputBitmaps.Count > 0)
            {
                int tmpX = (frameSize.Width - frameSize.Width);

                int tmpY = (frameSize.Height - frameSize.Height);

                if (plusX >= tmpX && plusY >= tmpY)
                {
                    for (int x = 0; x < plusX; x++)
                    {
                        for (int y = 0; y < plusY; y++)
                        {
                            if ((frameSize.Width - tmpX - (plusX - x) >= frameSize.Width)
                                || (frameSize.Width - tmpX - (plusX - x) < 0)
                                || (frameSize.Height - tmpY - (plusY - y) >= frameSize.Height)
                                || (frameSize.Height - tmpY - (plusY - y) < 0))
                            {
                                ShowStatic();
                                lastFrame = true;
                                return;
                            } 
                            //Color c = InputBitmaps[currentBitmap].GetPixel(frameSize.Width - tmpX - (plusX - x), frameSize.Height - tmpY - (plusY - y));

                            //DrawLED(x, y, c);

                            //Color c = parentWindow.Owner.ApplyColorType(InputBitmaps[currentBitmap].GetPixel(frameSize.Width - tmpX - (plusX - x), frameSize.Height - tmpY - (plusY - y)));
                            Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][frameSize.Width - tmpX - (plusX - x)][frameSize.Height - tmpY - (plusY - y)]);


                            if (MainWindow.oldAnimation)
                                DrawLED(x, y, c);
                            else
                            {
                                //if (frameBitmapPixels[x][y].ToArgb() != c.ToArgb())
                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                    }
                }
            }

            if (plusX > frameSize.Width - 1 && plusY >= frameSize.Height - 1)
                lastFrame = true;
        }
        private void AnimateSlideTopRight()
        {
            if (lastFrame)
                return;

            if (!MainWindow.oldAnimation)
                ClearLEDs(null);


            if (frameSize.Width > frameSize.Height)
            {
                //ratio = frameSize.Width / frameSize.Height;
                if (plusX >= borderX)
                    plusY++;
                plusX++;


                if (modulo > 0)
                {
                    modulo--;
                    plusX++;
                }

                if (plusX >= borderX)
                    borderX = ratio * plusY;
            }
            else if ( frameSize.Height >= frameSize.Width)
            {
                //ratio =  frameSize.Height / frameSize.Width;
                if (plusY >= borderY)
                    plusX++;
                plusY++;


                if (modulo > 0)
                {
                    modulo--;
                    plusY++;
                }

                if (plusY >= borderY)
                    borderY = ratio * plusX;
            }

            if (InputBitmaps != null && InputBitmaps.Count > 0)
            {
                int tmpX = (frameSize.Width - frameSize.Width);

                int tmpY = (frameSize.Height - frameSize.Height);

                if (plusX >= tmpX && plusY >= tmpY)
                {
                    for (int x = frameSize.Width - 1; x > frameSize.Width - 1 - plusX; x--)
                    {
                        for (int y = 0; y < plusY; y++)
                        {

                            if ((plusX - (frameSize.Width - x) - tmpX >= frameSize.Width)
                                || ((plusX - (frameSize.Width - x) - tmpX) < 0)
                                || (frameSize.Height - tmpY - (plusY - y) >= frameSize.Height)
                                || (frameSize.Height - tmpY - (plusY - y) < 0))
                            {
                                ShowStatic();
                                lastFrame = true;
                                return;
                            }

                            Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][plusX - (frameSize.Width - x) - tmpX][frameSize.Height - tmpY - (plusY - y)]);

                            if (MainWindow.oldAnimation)
                                DrawLED(x, y, c);
                            else
                            {
                                //if (frameBitmapPixels[x][y].ToArgb() != c.ToArgb())
                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                    }
                }
            }

            if (plusX > frameSize.Width - 1 && plusY >= frameSize.Height - 1)
                lastFrame = true;
        }
        private void AnimateSlideBottomLeft()
        {
            if (lastFrame)
                return;

            if (!MainWindow.oldAnimation)
                ClearLEDs(null);


            if (frameSize.Width > frameSize.Height)
            {
                //ratio = frameSize.Width / frameSize.Height;
                if (plusX >= borderX)
                    plusY++;
                plusX++;


                if (modulo > 0)
                {
                    modulo--;
                    plusX++;
                }


                if (plusX >= borderX)
                    borderX = ratio * plusY;
            }
            else if ( frameSize.Height >= frameSize.Width)
            {
                //ratio =  frameSize.Height / frameSize.Width;
                if (plusY >= borderY)
                    plusX++;
                plusY++;



                if (modulo > 0)
                {
                    modulo--;
                    plusY++;
                }

                if (plusY >= borderY)
                    borderY = ratio * plusX;
            }

            if (InputBitmaps != null && InputBitmaps.Count > 0)
            {
                int tmpX = (frameSize.Width - frameSize.Width);

                int tmpY = (frameSize.Height - frameSize.Height);

                if (plusX >= tmpX && plusY >= tmpY)
                {
                    for (int x = 0; x < plusX; x++)
                    {
                        for (int y = frameSize.Height - 1; y > frameSize.Height - 1 - plusY; y--)
                        {
                            if ((frameSize.Width - tmpX - (plusX - x) >= frameSize.Width)
                                || (frameSize.Width - tmpX - (plusX - x) < 0)
                                || (plusY - (frameSize.Height - y) - tmpY >= frameSize.Height)
                                || (plusY - (frameSize.Height - y) - tmpY < 0))
                            {
                                ShowStatic();
                                lastFrame = true;
                                return;
                            }
                            
                            Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][frameSize.Width - tmpX - (plusX - x)][plusY - (frameSize.Height - y) - tmpY]);

                            if (MainWindow.oldAnimation)
                                DrawLED(x, y, c);
                            else
                            {
                                //if (frameBitmapPixels[x][y].ToArgb() != c.ToArgb())
                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                    }
                }
            }

            if (plusX > frameSize.Width - 1 && plusY >= frameSize.Height - 1)
                lastFrame = true;
        }
        private void AnimateSlideBottomRight()
        {
            if (lastFrame)
                return;

            if (!MainWindow.oldAnimation)
                ClearLEDs(null);


            if (frameSize.Width > frameSize.Height)
            {
                //ratio = frameSize.Width / frameSize.Height;
                if (plusX >= borderX)
                    plusY++;
                plusX++;


                if (modulo > 0)
                {
                    modulo--;
                    plusX++;
                }

                if (plusX >= borderX)
                    borderX = ratio * plusY;
            }
            else if ( frameSize.Height >= frameSize.Width)
            {
                //ratio =  frameSize.Height / frameSize.Width;
                if (plusY >= borderY)
                    plusX++;
                plusY++;


                if (modulo > 0)
                {
                    modulo--;
                    plusY++;
                }

                if (plusY >= borderY)
                    borderY = ratio * plusX;
            }

            if (InputBitmaps != null && InputBitmaps.Count > 0)
            {
                int tmpX = (frameSize.Width - frameSize.Width);

                int tmpY = (frameSize.Height - frameSize.Height);

                if (plusX >= tmpX && plusY >= tmpY)
                {
                    for (int x = frameSize.Width - 1; x > frameSize.Width - 1 - plusX; x--)
                    {
                        for (int y = frameSize.Height - 1; y > frameSize.Height - 1 - plusY; y--)
                        {
                            if ((plusX - (frameSize.Width - x) - tmpX >= frameSize.Width)
                                || ((plusX - (frameSize.Width - x) - tmpX) < 0)
                                || (plusY - (frameSize.Height - y) - tmpY >= frameSize.Height)
                                || (plusY - (frameSize.Height - y) - tmpY < 0))
                            {
                                ShowStatic();
                                lastFrame = true;
                                return;
                            }
                            
                            Color c = parentWindow.Owner.ApplyColorType( staticBitmapPixels[currentBitmap][plusX - (frameSize.Width - x) - tmpX][plusY - (frameSize.Height - y) - tmpY]);

                            if (MainWindow.oldAnimation)
                                DrawLED(x, y, c);
                            else
                            {
                                //if (frameBitmapPixels[x][y].ToArgb() != c.ToArgb())
                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                    }
                }
            }

            if (plusX > frameSize.Width - 1 && plusY >= frameSize.Height - 1)
                lastFrame = true;
        }
        #endregion
        #region Zebra
        private void AnimateZebraHorizontal()
        {
            if (lastFrame)
                return;

            for (int y = 0; y < frameSize.Height * (PreviewWindow.LedDiam + PreviewWindow.Space); y++)
            {
                if (y < frameSize.Height && plusX < frameSize.Width)
                {
                    if (MainWindow.oldAnimation)
                        DrawLED(plusX, y, staticBitmapPixels[currentBitmap][plusX][y]);
                    else
                    {
                        int x = plusX;
                        Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                        if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                        {
                            frameBitmapPixels[x][y] = c;
                            changedPixels.Add(new ColorAtPixel(c, x, y));
                        }
                    }
                }

                if (y % 4 == 0 && (y / 4) % 2 == 1)
                    y += 3;
            }

            for (int y = 4; y < frameSize.Height * (PreviewWindow.LedDiam + PreviewWindow.Space); y++)
            {
                if (y < frameSize.Height && frameSize.Width - 1 - plusX >= 0)
                {
                    if (MainWindow.oldAnimation)
                        DrawLED(frameSize.Width - 1 - plusX, y, staticBitmapPixels[currentBitmap][frameSize.Width - 1 - plusX][y]);
                    else
                    {
                        int x = frameSize.Width - 1 - plusX;
                        Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                        if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                        {
                            frameBitmapPixels[x][y] = c;
                            changedPixels.Add(new ColorAtPixel(c, x, y));
                        }
                    }
                }
                    
                if (y % 4 == 0 && (y / 4) % 2 == 0)
                    y += 3;
            }

            if (plusX == frameSize.Width - 1)
            {
                lastFrame = true;
            }
            else
                plusX++;
        }
        private void AnimateZebraVertical()
        {
            if (lastFrame)
                return;

            for (int x = 0; x < frameSize.Width * (PreviewWindow.LedDiam + PreviewWindow.Space); x++)
            {
                if (x < frameSize.Width && plusY < frameSize.Height)
                {
                    if (MainWindow.oldAnimation)
                        DrawLED(x, plusY, staticBitmapPixels[currentBitmap][x][plusY]);
                    else
                    {
                        int y = plusY;
                        Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                        if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                        {
                            frameBitmapPixels[x][y] = c;
                            changedPixels.Add(new ColorAtPixel(c, x, y));
                        }
                    }
                }
                   
                if (x % 4 == 0 && (x / 4) % 2 == 1)
                    x += 3;
            }

            for (int x = 4; x < frameSize.Width * (PreviewWindow.LedDiam + PreviewWindow.Space); x++)
            {
                if (x < frameSize.Width && frameSize.Height - 1 - plusY >= 0)
                {
                    if (MainWindow.oldAnimation)
                        DrawLED(x, frameSize.Height - 1 - plusY, staticBitmapPixels[currentBitmap][x][frameSize.Height - 1 - plusY]);
                    else
                    {
                        int y = frameSize.Height - 1 - plusY;
                        Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                        if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                        {
                            frameBitmapPixels[x][y] = c;
                            changedPixels.Add(new ColorAtPixel(c, x, y));
                        }
                    }
                }
                    
                if (x % 4 == 0 && (x / 4) % 2 == 0)
                    x += 3;
            }

            if (plusY == frameSize.Height - 1)
            {
                lastFrame = true;
            }
            else
                plusY++;
        }
        #endregion
        #region Expand
        private void AnimateExpandTop()
        {
            if (lastFrame)
                return;

            if (currentY > frameSize.Height)
            {
                currentY--;
            }
  
            using (Bitmap expandBmp = new Bitmap(frameSize.Width, currentY))
            {
                using (Graphics expandGraphics = Graphics.FromImage(expandBmp))
                {
                    expandGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                    expandGraphics.DrawImage(InputBitmaps[currentBitmap], 0, 0, expandBmp.Width, expandBmp.Height);

                    if (changedPixels != null)
                        changedPixels.Clear();

                    for (int y = 0; y < expandBmp.Height; y++)
                    {
                        for (int x = 0; x < expandBmp.Width; x++)
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, y, expandBmp.GetPixel(x, y));
                            else
                            {
                                Color c = parentWindow.Owner.ApplyColorType(expandBmp.GetPixel(x, y));

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                //if (frameBitmapPixels[x][y].ToArgb() != c.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }

                        }
                    }
                }
            }

            if (currentY == frameSize.Height)
            {
                lastFrame = true;
                drawToMemory = false;
            }
            else
                currentY+=2;
        }
        private void AnimateExpandBottom()
        {
            if (lastFrame)
                return;

            if (currentY > frameSize.Height)
            {
                currentY--;
            }

            using (Bitmap expandBmp = new Bitmap(frameSize.Width, currentY))
            {
                using (Graphics expandGraphics = Graphics.FromImage(expandBmp))
                {
                    expandGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                    expandGraphics.DrawImage(InputBitmaps[currentBitmap], 0, 0, expandBmp.Width, expandBmp.Height);

                    if (changedPixels != null)
                        changedPixels.Clear();

                    for (int y = 0; y < expandBmp.Height; y++)
                    {
                        for (int x = 0; x < expandBmp.Width; x++)
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, frameSize.Height - expandBmp.Height + y, expandBmp.GetPixel(x, y));
                            else
                            {
                                Color c = parentWindow.Owner.ApplyColorType(expandBmp.GetPixel(x, y));

                                int tmpy = frameSize.Height - expandBmp.Height + y;
                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                //if (frameBitmapPixels[x][tmpy].ToArgb() != c.ToArgb())
                                {
                                    frameBitmapPixels[x][tmpy] = c;//expandBmp.GetPixel(x, y);
                                    changedPixels.Add(new ColorAtPixel(c, x, tmpy));
                                }
                            }
                        }
                    }
                }
            }

            if (currentY == frameSize.Height)
            {
                lastFrame = true;
            }
            else
                currentY+=2;
        }
        private void AnimateExpandVertical()
        {
            if (lastFrame)
                return;

            if (currentY > frameSize.Height)
            {
                currentY--;
            }

            using (Bitmap expandBmp = new Bitmap(frameSize.Width, currentY))
            {
                using (Graphics expandGraphics = Graphics.FromImage(expandBmp))
                {
                    expandGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                    expandGraphics.DrawImage(InputBitmaps[currentBitmap], 0, 0, expandBmp.Width, expandBmp.Height);

                    if (changedPixels != null)
                        changedPixels.Clear();

                    for (int y = 0; y < expandBmp.Height; y++)
                    {
                        for (int x = 0; x < expandBmp.Width; x++)
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, (frameSize.Height / 2) - (currentY / 2) + y, expandBmp.GetPixel(x, y));
                            else
                            {
                                Color c = parentWindow.Owner.ApplyColorType(expandBmp.GetPixel(x, y));

                                int tmpy = frameSize.Height - expandBmp.Height + y;
                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                //if (frameBitmapPixels[x][tmpy].ToArgb() != c.ToArgb())
                                {
                                    frameBitmapPixels[x][tmpy] = c;//expandBmp.GetPixel(x, y);
                                    changedPixels.Add(new ColorAtPixel(c, x, tmpy));
                                }
                            }
                        }
                    }
                }
            }

            if (currentY == frameSize.Height)
            {
                lastFrame = true;
            }
            else
                currentY+=2;
        }
        #endregion
        #region Other
        private void AnimateScrapeUp()
        {
            if (lastFrame)
                return;

            Color pixelColor;

            if (borderY == frameSize.Height - 1)
            {
                for (int x = 0; x < frameSize.Width; x++)
                {
                    for (int y = borderY, currentY = 0; y >= frameSize.Height / 2; y--, currentY++)
                    {
                        Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                        if (MainWindow.oldAnimation)
                            DrawLED(x, y - currentY, c);
                        else
                        {
                            //if (frameBitmapPixels[x][y].ToArgb() != c.ToArgb())
                            if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                            {
                                frameBitmapPixels[x][y - currentY] = c;
                                changedPixels.Add(new ColorAtPixel(c, x, y - currentY));
                            }
                        }
                    }
                }
            }


            if (borderY > -1)
            {
                if (borderY < frameSize.Height - 2)
                {
                    int change = 2;
                    int hmod = (frameSize.Height - 1) % 2;


                    //if ((borderY % 2 == hmod && (borderY < frameSize.Height / 2)) || (borderY % 2 != hmod && (borderY >= frameSize.Height / 2)))
                    if ((borderY % 2 != hmod))
                    {
                        Bitmap scrapePart = new Bitmap((frameSize.Width) * (PreviewWindow.LedDiam + PreviewWindow.Space), 
                            (borderY + change) * (PreviewWindow.LedDiam + PreviewWindow.Space));
                        Graphics gScrapePart = Graphics.FromImage(scrapePart);
                        Graphics windowGraphics = Graphics.FromImage(parentWindow.windowBitmap);
                        
                        gScrapePart.DrawImageUnscaledAndClipped(parentWindow.windowBitmap, new Rectangle(0, 0 * (PreviewWindow.LedDiam + PreviewWindow.Space), 
                            scrapePart.Width, scrapePart.Height - change * (PreviewWindow.LedDiam + PreviewWindow.Space)));

                        windowGraphics.DrawImageUnscaledAndClipped(scrapePart, new Rectangle(0, change * (PreviewWindow.LedDiam + PreviewWindow.Space), 
                            scrapePart.Width, scrapePart.Height));


                        scrapePart.Dispose();
                        gScrapePart.Dispose();
                        windowGraphics.Dispose();

                        if (currentY > -1)
                        {
                            for (int x = 0; x < frameSize.Width; x++)
                            {
                                pixelColor = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);
                                DrawLED(x, hmod, pixelColor);
                            }
                            currentY--;
                        }
                    }
                    else
                    {
                        //if (borderY >= frameSize.Height / 2)
                        {
                            for (int x = 0; x < frameSize.Width; x++)
                                DrawLED(x, borderY + 1, staticBitmapPixels[currentBitmap][x][borderY + 1]);
                        }
                    }
                }

                if (borderY == 0)
                {
                    for (int x = 0; x < frameSize.Width; x++)
                    {
                        pixelColor = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][0]);
                        DrawLED(x, 0, pixelColor);
                    }

                    lastFrame = true;
                }
                else
                    borderY--;
            }
        }
        private void AnimateSnow()
        {
            if (lastFrame)
                return;

            Color pixelColor;

            if (borderY > -1)
            {
                if (borderY < frameSize.Height - 1)
                {
                    for (int x = 0; x < frameSize.Width; x++)
                    {
                        pixelColor = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][borderY + 1]);
                        Color drawingAreaPixelColor =
                            parentWindow.Owner.ApplyColorType(parentWindow.windowBitmap.GetPixel(x * (PreviewWindow.LedDiam + PreviewWindow.Space) + PreviewWindow.LedDiam / 2, borderY * (PreviewWindow.LedDiam + PreviewWindow.Space) + PreviewWindow.LedDiam / 2));
                        if (drawingAreaPixelColor == pixelColor)
                        {
                            if (pixelColor != PreviewWindow.OffLEDColor)
                            {

                                if (MainWindow.oldAnimation)
                                    DrawLED(x, borderY + 1, pixelColor);
                                else
                                {
                                    //int y = borderY + 1;
                                    //Color c = pixelColor;//parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                                    //if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                    //{
                                    //    frameBitmapPixels[x][y] = c;
                                    //    changedPixels.Add(new ColorAtPixel(c, x, y));
                                    //}
                                }
                            }
                        }
                    }
                }

                if (MainWindow.oldAnimation)
                {
                    Bitmap snowPart = new Bitmap((frameSize.Width) * (PreviewWindow.LedDiam + PreviewWindow.Space), (borderY + 1) * (PreviewWindow.LedDiam + PreviewWindow.Space));
                    Graphics gSnowPart = Graphics.FromImage(snowPart);

                    gSnowPart.DrawImageUnscaledAndClipped(parentWindow.windowBitmap, new Rectangle(0, 0 * (PreviewWindow.LedDiam + PreviewWindow.Space), snowPart.Width, snowPart.Height - 1 * (PreviewWindow.LedDiam + PreviewWindow.Space)));

                    Graphics windowGraphics = Graphics.FromImage(parentWindow.windowBitmap);
                    windowGraphics.DrawImageUnscaledAndClipped(snowPart, new Rectangle(0, 1 * (PreviewWindow.LedDiam + PreviewWindow.Space), snowPart.Width, snowPart.Height));

                    //for (int x = 0; x < frameSize.Width; x ++)
                    //    DrawLED(x, 0, PreviewWindow.OffLEDColor);

                    snowPart.Dispose();
                    gSnowPart.Dispose();
                    windowGraphics.Dispose();
                }
                else
                {
                    for (int i = 0; i < changedPixels.Count; i++)
                    {
                        if(changedPixels[i].PixelPoint.Y < borderY+1)
                            changedPixels[i].PixelPoint = new Point(changedPixels[i].PixelPoint.X, changedPixels[i].PixelPoint.Y + 1);
                    }
                    //int y = borderY + 1;
                    //Color c = pixelColor;//parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                    //if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    //{
                    //    frameBitmapPixels[x][y] = c;
                    //    changedPixels.Add(new ColorAtPixel(c, x, y));
                    //}
                }
            }

            //kolejność:
            // 0,4,2,6,1,7,3,5
            #region snow switch
            switch ((currentX) % 8)
            {
                case 0:
                    for (int x = 0; x < frameSize.Width; x += 8)
                    {
                        if (staticBitmapPixels[currentBitmap][x][currentY] != PreviewWindow.OffLEDColor)
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, 0, staticBitmapPixels[currentBitmap][x][currentY]);
                            else
                            {
                                int y = 0;
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                        if (currentY > 0)
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x + 5, 0, PreviewWindow.OffLEDColor);
                            else
                            {
                                int y = 0;
                                Color c = PreviewWindow.OffLEDColor;//parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x + 5][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x + 5, y));
                                }
                            }
                        }
                        else
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x + 5, 0, staticBitmapPixels[currentBitmap][x + 5][currentY]);
                            else
                            {
                                int y = 0;
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x + 5][currentY]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x + 5][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x + 5, y));
                                }
                            }
                        }

                        if (borderY < frameSize.Height - 1)
                        {
                            pixelColor = staticBitmapPixels[currentBitmap][x + 5][borderY + 1];
                            if (pixelColor != PreviewWindow.OffLEDColor)
                            {
                                //      DrawLED(x - 5, borderY + 1, pixelColor);
                            }
                        }
                    }
                    break;
                case 1:
                    for (int x = 4; x < frameSize.Width; x += 8)
                    {
                        if (staticBitmapPixels[currentBitmap][x][currentY] != PreviewWindow.OffLEDColor)
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, 0, staticBitmapPixels[currentBitmap][x][currentY]);
                            else
                            {
                                int y = 0;
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                        if (currentY > 0)
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x - 4, 0, PreviewWindow.OffLEDColor);
                            else
                            {
                                int y = 0;
                                Color c = PreviewWindow.OffLEDColor;//parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x - 4][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x - 4, y));
                                }
                            }
                        }
                        if (borderY < frameSize.Height - 1)
                        {
                            pixelColor = staticBitmapPixels[currentBitmap][x - 4][borderY + 1];
                            if (pixelColor != PreviewWindow.OffLEDColor)
                            {
                                //      DrawLED(x + 2, borderY + 1, pixelColor);
                            }
                        }
                    }
                    break;
                case 2:
                    for (int x = 2; x < frameSize.Width; x += 8)
                    {
                        if (staticBitmapPixels[currentBitmap][x][currentY] != PreviewWindow.OffLEDColor)
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, 0, staticBitmapPixels[currentBitmap][x][currentY]);
                            else
                            {
                                int y = 0;
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                        if (currentY > 0)
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x + 2, 0, PreviewWindow.OffLEDColor);
                            else
                            {
                                int y = 0;
                                Color c = PreviewWindow.OffLEDColor;//parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x + 2][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x + 2, y));
                                }
                            }
                        }


                        if (borderY < frameSize.Height - 1)
                        {
                            pixelColor = staticBitmapPixels[currentBitmap][x + 2][borderY + 1];
                            if (pixelColor != PreviewWindow.OffLEDColor)
                            {
                                //      DrawLED(x + 2, borderY + 1, pixelColor);
                            }
                        }
                    }
                    break;
                case 3:
                    for (int x = 6; x < frameSize.Width; x += 8)
                    {
                        if (staticBitmapPixels[currentBitmap][x][currentY] != PreviewWindow.OffLEDColor)
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, 0, staticBitmapPixels[currentBitmap][x][currentY]);
                            else
                            {
                                int y = 0;
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                        if (currentY > 0)
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x - 4, 0, PreviewWindow.OffLEDColor);
                            else
                            {
                                int y = 0;
                                Color c = PreviewWindow.OffLEDColor;//parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x - 4][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x - 4, y));
                                }
                            }
                        }

                        if (borderY < frameSize.Height - 1)
                        {
                            pixelColor = staticBitmapPixels[currentBitmap][x - 4][borderY + 1];
                            if (pixelColor != PreviewWindow.OffLEDColor)
                            {
                                //   DrawLED(x + 2, borderY + 1, pixelColor);
                            }
                        }
                    }
                    break;
                case 4:
                    for (int x = 1; x < frameSize.Width; x += 8)
                    {
                        if (staticBitmapPixels[currentBitmap][x][currentY] != PreviewWindow.OffLEDColor)
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, 0, staticBitmapPixels[currentBitmap][x][currentY]);
                            else
                            {
                                int y = 0;
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                        if (currentY > 0)
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x + 5, 0, PreviewWindow.OffLEDColor);
                            else
                            {
                                int y = 0;
                                Color c = PreviewWindow.OffLEDColor;//parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x + 5][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x + 5, y));
                                }
                            }
                        }

                        if (borderY < frameSize.Height - 1)
                        {
                            pixelColor = staticBitmapPixels[currentBitmap][x + 5][borderY + 1];
                            if (pixelColor != PreviewWindow.OffLEDColor)
                            {
                                //      DrawLED(x - 5, borderY + 1, pixelColor);
                            }
                        }
                    }
                    break;
                case 5:
                    for (int x = 7; x < frameSize.Width; x += 8)
                    {
                        if (staticBitmapPixels[currentBitmap][x][currentY] != PreviewWindow.OffLEDColor)
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, 0, staticBitmapPixels[currentBitmap][x][currentY]);
                            else
                            {
                                int y = 0;
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                        if (currentY > 0)
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x - 6, 0, PreviewWindow.OffLEDColor);
                            else
                            {
                                int y = 0;
                                Color c = PreviewWindow.OffLEDColor;//parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x - 6][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x - 6, y));
                                }
                            }
                        }

                        if (borderY < frameSize.Height - 1)
                        {
                            pixelColor = staticBitmapPixels[currentBitmap][x - 6][borderY + 1];
                            if (pixelColor != PreviewWindow.OffLEDColor)
                            {
                                //     DrawLED(x + 6, borderY + 1, pixelColor);
                            }
                        }
                    }
                    break;
                case 6:
                    for (int x = 3; x < frameSize.Width; x += 8)
                    {
                        if (staticBitmapPixels[currentBitmap][x][currentY] != PreviewWindow.OffLEDColor)
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, 0, staticBitmapPixels[currentBitmap][x][currentY]);
                            else
                            {
                                int y = 0;
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                        if (currentY > 0)
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x + 4, 0, PreviewWindow.OffLEDColor);
                            else
                            {
                                int y = 0;
                                Color c = PreviewWindow.OffLEDColor;//parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x + 4][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x + 4, y));
                                }
                            }
                        }

                        if (borderY < frameSize.Height - 1)
                        {
                            pixelColor = staticBitmapPixels[currentBitmap][x + 4][borderY + 1];
                            if (pixelColor != PreviewWindow.OffLEDColor)
                            {
                                //      DrawLED(x - 4, borderY + 1, pixelColor);
                            }
                        }
                    }
                    break;
                case 7:
                    for (int x = 5; x < frameSize.Width; x += 8)
                    {
                        if (staticBitmapPixels[currentBitmap][x][currentY] != PreviewWindow.OffLEDColor)
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, 0, staticBitmapPixels[currentBitmap][x][currentY]);
                            else
                            {
                                int y = 0;
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                        if (currentY > 0)
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x - 2, 0, PreviewWindow.OffLEDColor);
                            else
                            {
                                int y = 0;
                                Color c = PreviewWindow.OffLEDColor;//parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x - 2][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x - 2, y));
                                }
                            }
                        }

                        if (borderY < frameSize.Height - 1)
                        {
                            pixelColor = staticBitmapPixels[currentBitmap][x - 2][borderY + 1];
                            if (pixelColor != PreviewWindow.OffLEDColor)
                            {
                                //       DrawLED(x + 2, borderY + 1, pixelColor);
                            }
                        }
                    }
                    break;

            }
            #endregion
            #region old snow
            /*
                        switch ((currentX) % 8)
                        {
                            case 0:
                                for (int x = 7; x < frameSize.Width; x += 8)
                                {
                                    if (staticBitmapPixels[currentBitmap][x][currentY] != PreviewWindow.OffLEDColor)
                                        DrawLED(x, 0, staticBitmapPixels[currentBitmap][x][currentY]);
                                    //if (currentY > 0)
                                        DrawLED(x - 5, 0, PreviewWindow.OffLEDColor);


                                //    testBitmapGraphics.DrawEllipse(new Pen(staticBitmapPixels[currentBitmap][x][currentY]), x * (PreviewWindow.LedDiam + PreviewWindow.Space), currentY * (PreviewWindow.LedDiam + PreviewWindow.Space), PreviewWindow.LedDiam, PreviewWindow.LedDiam);
                                //    testBitmapGraphics.FillEllipse(new SolidBrush(staticBitmapPixels[currentBitmap][x][currentY]), x * (PreviewWindow.LedDiam + PreviewWindow.Space), currentY * (PreviewWindow.LedDiam + PreviewWindow.Space), PreviewWindow.LedDiam, PreviewWindow.LedDiam);


                                    if (borderY < frameSize.Height - 1)
                                    {
                                        pixelColor = InputBitmaps[currentBitmap].GetPixel(x - 5, borderY + 1);
                                        if (pixelColor != PreviewWindow.OffLEDColor)
                                        {
                                      //      DrawLED(x - 5, borderY + 1, pixelColor);
                                            
                                        }
                                    }
                                }
                                break;
                            case 1:
                                for (int x = 5; x < frameSize.Width; x += 8)
                                {
                                    if (staticBitmapPixels[currentBitmap][x][currentY] != PreviewWindow.OffLEDColor)
                                        DrawLED(x, 0, staticBitmapPixels[currentBitmap][x][currentY]);
                                    if (currentY > 0)
                                        DrawLED(x + 2, 0, PreviewWindow.OffLEDColor);



                                  //  testBitmapGraphics.DrawEllipse(new Pen(staticBitmapPixels[currentBitmap][x][currentY]), x * (PreviewWindow.LedDiam + PreviewWindow.Space), currentY * (PreviewWindow.LedDiam + PreviewWindow.Space), PreviewWindow.LedDiam, PreviewWindow.LedDiam);
                                //    testBitmapGraphics.FillEllipse(new SolidBrush(staticBitmapPixels[currentBitmap][x][currentY]), x * (PreviewWindow.LedDiam + PreviewWindow.Space), currentY * (PreviewWindow.LedDiam + PreviewWindow.Space), PreviewWindow.LedDiam, PreviewWindow.LedDiam);

                                    if (borderY < frameSize.Height - 1)
                                    {
                                        pixelColor = staticBitmapPixels[currentBitmap][x + 2][borderY + 1];
                                        if (pixelColor != PreviewWindow.OffLEDColor)
                                        {
                                      //      DrawLED(x + 2, borderY + 1, pixelColor);
                                            
                                        }
                                    }
                                }
                                break;
                            case 2:
                                for (int x = 3; x < frameSize.Width; x += 8)
                                {
                                    if (staticBitmapPixels[currentBitmap][x][currentY] != PreviewWindow.OffLEDColor)
                                        DrawLED(x, 0, staticBitmapPixels[currentBitmap][x][currentY]);
                                    if (currentY > 0)
                                        DrawLED(x + 2, 0, PreviewWindow.OffLEDColor);



                                //    testBitmapGraphics.DrawEllipse(new Pen(staticBitmapPixels[currentBitmap][x][currentY]), x * (PreviewWindow.LedDiam + PreviewWindow.Space), currentY * (PreviewWindow.LedDiam + PreviewWindow.Space), PreviewWindow.LedDiam, PreviewWindow.LedDiam);
                               //     testBitmapGraphics.FillEllipse(new SolidBrush(staticBitmapPixels[currentBitmap][x][currentY]), x * (PreviewWindow.LedDiam + PreviewWindow.Space), currentY * (PreviewWindow.LedDiam + PreviewWindow.Space), PreviewWindow.LedDiam, PreviewWindow.LedDiam);

                                    if (borderY < frameSize.Height - 1)
                                    {
                                        pixelColor = staticBitmapPixels[currentBitmap][x + 2][borderY + 1];
                                        if (pixelColor != PreviewWindow.OffLEDColor)
                                        {
                                      //      DrawLED(x + 2, borderY + 1, pixelColor);
                                            
                                        }
                                    }
                                }
                                break;
                            case 3:
                                for (int x = 1; x < frameSize.Width; x += 8)
                                {
                                    if (staticBitmapPixels[currentBitmap][x][currentY] != PreviewWindow.OffLEDColor)
                                        DrawLED(x, 0, staticBitmapPixels[currentBitmap][x][currentY]);
                                    if (currentY > 0)
                                        DrawLED(x + 2, 0, PreviewWindow.OffLEDColor);



                                 //   testBitmapGraphics.DrawEllipse(new Pen(staticBitmapPixels[currentBitmap][x][currentY]), x * (PreviewWindow.LedDiam + PreviewWindow.Space), currentY * (PreviewWindow.LedDiam + PreviewWindow.Space), PreviewWindow.LedDiam, PreviewWindow.LedDiam);
                                //    testBitmapGraphics.FillEllipse(new SolidBrush(staticBitmapPixels[currentBitmap][x][currentY]), x * (PreviewWindow.LedDiam + PreviewWindow.Space), currentY * (PreviewWindow.LedDiam + PreviewWindow.Space), PreviewWindow.LedDiam, PreviewWindow.LedDiam);

                                    if (borderY < frameSize.Height - 1)
                                    {
                                        pixelColor = staticBitmapPixels[currentBitmap][x + 2][borderY + 1];
                                        if (pixelColor != PreviewWindow.OffLEDColor)
                                        {
                                         //   DrawLED(x + 2, borderY + 1, pixelColor);
                                            
                                        }
                                    }
                                }
                                break;
                            case 4:
                                for (int x = 6; x < frameSize.Width; x += 8)
                                {
                                    if (staticBitmapPixels[currentBitmap][x][currentY] != PreviewWindow.OffLEDColor)
                                        DrawLED(x, 0, staticBitmapPixels[currentBitmap][x][currentY]);
                                    if (currentY > 0)
                                        DrawLED(x - 5, 0, PreviewWindow.OffLEDColor);



                                  //  testBitmapGraphics.DrawEllipse(new Pen(staticBitmapPixels[currentBitmap][x][currentY]), x * (PreviewWindow.LedDiam + PreviewWindow.Space), currentY * (PreviewWindow.LedDiam + PreviewWindow.Space), PreviewWindow.LedDiam, PreviewWindow.LedDiam);
                                 //   testBitmapGraphics.FillEllipse(new SolidBrush(staticBitmapPixels[currentBitmap][x][currentY]), x * (PreviewWindow.LedDiam + PreviewWindow.Space), currentY * (PreviewWindow.LedDiam + PreviewWindow.Space), PreviewWindow.LedDiam, PreviewWindow.LedDiam);

                                    if (borderY < frameSize.Height - 1)
                                    {
                                        pixelColor = InputBitmaps[currentBitmap].GetPixel(x - 5, borderY + 1);
                                        if (pixelColor != PreviewWindow.OffLEDColor)
                                        {
                                      //      DrawLED(x - 5, borderY + 1, pixelColor);
                                            
                                        }
                                    }
                                }
                                break;
                            case 5:
                                for (int x = 0; x < frameSize.Width; x += 8)
                                {
                                    if (staticBitmapPixels[currentBitmap][x][currentY] != PreviewWindow.OffLEDColor)
                                        DrawLED(x, 0, staticBitmapPixels[currentBitmap][x][currentY]);
                                    if (currentY > 0)
                                        DrawLED(x + 6, 0, PreviewWindow.OffLEDColor);



                                   // testBitmapGraphics.DrawEllipse(new Pen(staticBitmapPixels[currentBitmap][x][currentY]), x * (PreviewWindow.LedDiam + PreviewWindow.Space), currentY * (PreviewWindow.LedDiam + PreviewWindow.Space), PreviewWindow.LedDiam, PreviewWindow.LedDiam);
                                  //  testBitmapGraphics.FillEllipse(new SolidBrush(staticBitmapPixels[currentBitmap][x][currentY]), x * (PreviewWindow.LedDiam + PreviewWindow.Space), currentY * (PreviewWindow.LedDiam + PreviewWindow.Space), PreviewWindow.LedDiam, PreviewWindow.LedDiam);

                                    if (borderY < frameSize.Height - 1)
                                    {
                                        pixelColor = InputBitmaps[currentBitmap].GetPixel(x + 6, borderY + 1);
                                        if (pixelColor != PreviewWindow.OffLEDColor)
                                        {
                                       //     DrawLED(x + 6, borderY + 1, pixelColor);
                                            
                                        }
                                    }
                                }
                                break;
                            case 6:
                                for (int x = 4; x < frameSize.Width; x += 8)
                                {
                                    if (staticBitmapPixels[currentBitmap][x][currentY] != PreviewWindow.OffLEDColor)
                                        DrawLED(x, 0, staticBitmapPixels[currentBitmap][x][currentY]);
                                    if (currentY > 0)
                                        DrawLED(x - 4, 0, PreviewWindow.OffLEDColor);



                                   // testBitmapGraphics.DrawEllipse(new Pen(staticBitmapPixels[currentBitmap][x][currentY]), x * (PreviewWindow.LedDiam + PreviewWindow.Space), currentY * (PreviewWindow.LedDiam + PreviewWindow.Space), PreviewWindow.LedDiam, PreviewWindow.LedDiam);
                                  //  testBitmapGraphics.FillEllipse(new SolidBrush(staticBitmapPixels[currentBitmap][x][currentY]), x * (PreviewWindow.LedDiam + PreviewWindow.Space), currentY * (PreviewWindow.LedDiam + PreviewWindow.Space), PreviewWindow.LedDiam, PreviewWindow.LedDiam);

                                    if (borderY < frameSize.Height - 1)
                                    {
                                        pixelColor = staticBitmapPixels[currentBitmap][x - 4][borderY + 1];
                                        if (pixelColor != PreviewWindow.OffLEDColor)
                                        {
                                      //      DrawLED(x - 4, borderY + 1, pixelColor);
                                            
                                        }
                                    }
                                }
                                break;
                            case 7:
                                for (int x = 2; x < frameSize.Width; x += 8)
                                {
                                    if (staticBitmapPixels[currentBitmap][x][currentY] != PreviewWindow.OffLEDColor)
                                        DrawLED(x, 0, staticBitmapPixels[currentBitmap][x][currentY]);
                                    if (currentY > 0)
                                        DrawLED(x + 2, 0, PreviewWindow.OffLEDColor);



                                    //testBitmapGraphics.DrawEllipse(new Pen(staticBitmapPixels[currentBitmap][x][currentY]), x * (PreviewWindow.LedDiam + PreviewWindow.Space), currentY * (PreviewWindow.LedDiam + PreviewWindow.Space), PreviewWindow.LedDiam, PreviewWindow.LedDiam);
                                   // testBitmapGraphics.FillEllipse(new SolidBrush(staticBitmapPixels[currentBitmap][x][currentY]), x * (PreviewWindow.LedDiam + PreviewWindow.Space), currentY * (PreviewWindow.LedDiam + PreviewWindow.Space), PreviewWindow.LedDiam, PreviewWindow.LedDiam);

                                    if (borderY < frameSize.Height - 1)
                                    {
                                        pixelColor = staticBitmapPixels[currentBitmap][x + 2][borderY + 1];
                                        if (pixelColor != PreviewWindow.OffLEDColor)
                                        {
                                     //       DrawLED(x + 2, borderY + 1, pixelColor);
                                            
                                        }
                                    }
                                }
                                break;

                        }
                         */
            #endregion
            if ((currentX) % 8 == 7)// && currentX > 7)
                currentY--;

            if (currentY < 0)
            {
                lastFrame = true;
                return;
            }

            if (!firstSnowInNewLine && currentX >= frameSize.Height - 1 && (currentX + (frameSize.Height - 1 - borderY) +  (8 - frameSize.Height % 8)) % 8 == 7)// && (frameSize.Height - 1 - borderY) % 8 == 7)
            {
                borderY--;
                firstSnowInNewLine = true;
            }
            else if (firstSnowInNewLine)
                firstSnowInNewLine = false;

            currentX = (currentX + 1);
        }
        private void AnimateSlewingOut()
        {
            if (lastFrame)
                return;

            if (frameSize.Width > frameSize.Height)
            {
                ratio = frameSize.Width / frameSize.Height;
                plusY++;
                plusX = ratio * plusY;
            }
            else if (frameSize.Height > frameSize.Width)
            {
                ratio =  frameSize.Height / frameSize.Width;
                plusX++;
                plusY = ratio * plusX;
            }
            newRect = Rectangle.FromLTRB(center.X - plusX, center.Y - plusY, center.X + plusX, center.Y + plusY);

            for (int x = center.X - plusX; x <= center.X + plusX; x++)
            {
                for (int y = center.Y - plusY; y <= center.Y + plusY; y++)
                {
                    if ((x >= 0 && x < frameSize.Width)
                        && (y >= 0 && y < frameSize.Height))
                    {
                        if (newRect.Contains(x, y) && !oldRect.Contains(x, y))
                            DrawLED(x, y, staticBitmapPixels[currentBitmap][x][y]);
                    }
                }
            }
            if (newRect.Contains(0, 0) && newRect.Contains(0, frameSize.Height - 1)
                && newRect.Contains(frameSize.Width - 1, 0)
                && newRect.Contains(frameSize.Width - 1, frameSize.Height - 1))
            {
                lastFrame = true;
            }
            else
            {
                oldRect = newRect;
            }
        }
        private void AnimateBubbles()
        {
            if (lastFrame)
                return;

            Color pixelColor;

            if (borderY < frameSize.Height)
            {
                if (borderY > 0)
                {
                    for (int x = 0; x < frameSize.Width; x++)
                    {
                        pixelColor = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][borderY - 1]);
                            //parentWindow.Owner.ApplyColorType(InputBitmaps[currentBitmap].GetPixel(x, borderY - 1));
                        Color drawingAreaPixelColor = //frameBitmapPixels[x][borderY];
                            parentWindow.Owner.ApplyColorType(parentWindow.windowBitmap.GetPixel(x * (PreviewWindow.LedDiam + PreviewWindow.Space) + PreviewWindow.LedDiam / 2, borderY * (PreviewWindow.LedDiam + PreviewWindow.Space) + PreviewWindow.LedDiam / 2));
                        if (drawingAreaPixelColor.ToArgb() == pixelColor.ToArgb())
                        {
                            if (pixelColor.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                            {

                                if (MainWindow.oldAnimation)
                                    DrawLED(x, borderY - 1, pixelColor);
                                else
                                {
                                    //int y = borderY + 1;
                                    //Color c = pixelColor;//parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                                    //if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                    //{
                                    //    frameBitmapPixels[x][y] = c;
                                    //    changedPixels.Add(new ColorAtPixel(c, x, y));
                                    //}
                                }
                            }
                        }
                    }
                }

                if (borderY < frameSize.Height-1)
                {
                    if (MainWindow.oldAnimation)
                    {
                        //Graphics windowGraphics = Graphics.FromImage(parentWindow.windowBitmap);

                        //Bitmap currentWindowImage = new Bitmap((parentWindow.windowBitmap.Width),
                        //    parentWindow.windowBitmap.Height - (1 * (PreviewWindow.LedDiam + PreviewWindow.Space)));
                        //Graphics currentWindowGraphics = Graphics.FromImage(currentWindowImage);
                        //currentWindowGraphics.DrawImageUnscaledAndClipped(parentWindow.windowBitmap, new Rectangle(0, -1 * (PreviewWindow.LedDiam + PreviewWindow.Space),
                        //    currentWindowImage.Width, currentWindowImage.Height + 1 * (PreviewWindow.LedDiam + PreviewWindow.Space)));
                        //windowGraphics.DrawImageUnscaled(currentWindowImage, 0, 0);

                        //currentWindowImage.Dispose();
                        //currentWindowGraphics.Dispose();
                        //windowGraphics.Dispose();

                        Bitmap snowPart = new Bitmap(((frameSize.Width) * (PreviewWindow.LedDiam + PreviewWindow.Space)),
                            //parentWindow.windowBitmap.Height - 
                            ((frameSize.Height - (borderY + 1)) * (PreviewWindow.LedDiam + PreviewWindow.Space)));
                        //new Bitmap((frameSize.Width) * (PreviewWindow.LedDiam + PreviewWindow.Space), (borderY + 1) * (PreviewWindow.LedDiam + PreviewWindow.Space));
                        Graphics gSnowPart = Graphics.FromImage(snowPart);


                        //Console.WriteLine("currentY: {0}", currentY);

                        gSnowPart.DrawImageUnscaledAndClipped(parentWindow.windowBitmap, new Rectangle(0, -(borderY + 1) * (PreviewWindow.LedDiam + PreviewWindow.Space),
                            snowPart.Width, snowPart.Height + (borderY + 1) * (PreviewWindow.LedDiam + PreviewWindow.Space)));

                        Graphics windowGraphics = Graphics.FromImage(parentWindow.windowBitmap);
                        windowGraphics.DrawImageUnscaledAndClipped(snowPart, new Rectangle(0, borderY * (PreviewWindow.LedDiam + PreviewWindow.Space),
                            snowPart.Width, snowPart.Height - (0) * (PreviewWindow.LedDiam + PreviewWindow.Space)));

                        for (int x = 0; x < frameSize.Width; x++)
                            DrawLED(x, frameSize.Height - 1, PreviewWindow.OffLEDColor);

                        snowPart.Dispose();
                        gSnowPart.Dispose();
                        windowGraphics.Dispose();




                        //Bitmap snowPart = new Bitmap((frameSize.Width) * (PreviewWindow.LedDiam + PreviewWindow.Space), (borderY + 1) * (PreviewWindow.LedDiam + PreviewWindow.Space));
                        //Graphics gSnowPart = Graphics.FromImage(snowPart);

                        //gSnowPart.DrawImageUnscaledAndClipped(parentWindow.windowBitmap, new Rectangle(0, -1 * (PreviewWindow.LedDiam + PreviewWindow.Space),
                        //    snowPart.Width, snowPart.Height - 1 * (PreviewWindow.LedDiam + PreviewWindow.Space)));

                        //Graphics windowGraphics = Graphics.FromImage(parentWindow.windowBitmap);
                        //windowGraphics.DrawImageUnscaledAndClipped(snowPart, new Rectangle(0, parentWindow.windowBitmap.Height - snowPart.Height - 1 * (PreviewWindow.LedDiam + PreviewWindow.Space), snowPart.Width, snowPart.Height));

                        ////for (int x = 0; x < frameSize.Width; x ++)
                        ////    DrawLED(x, 0, PreviewWindow.OffLEDColor);

                        //snowPart.Dispose();
                        //gSnowPart.Dispose();
                        //windowGraphics.Dispose();
                    }
                    else
                    {
                        for (int i = 0; i < changedPixels.Count; i++)
                        {
                            if (changedPixels[i].PixelPoint.Y > borderY - 1)
                                changedPixels[i].PixelPoint = new Point(changedPixels[i].PixelPoint.X, changedPixels[i].PixelPoint.Y - 1);
                        }
                    }
                }
            }

            //kolejność:
            // 0,4,2,6,1,7,3,5

            int bmph = frameSize.Height - 1;
            #region bubbles switch
            switch ((currentX) % 8)
            {
                case 0:
                    for (int x = 0; x < frameSize.Width; x += 8)
                    {
                        if (staticBitmapPixels[currentBitmap][x][currentY] != PreviewWindow.OffLEDColor)
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, bmph, staticBitmapPixels[currentBitmap][x][currentY]);
                            else
                            {
                                int y = bmph;
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                        //if (currentY <= bmph)
                        //{
                        //    if (MainWindow.oldAnimation)
                        //        DrawLED(x + 5, bmph, PreviewWindow.OffLEDColor);
                        //    else
                        //    {
                        //        int y = bmph;
                        //        Color c = PreviewWindow.OffLEDColor;//parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                        //        if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                        //        {
                        //            frameBitmapPixels[x + 5][y] = c;
                        //            changedPixels.Add(new ColorAtPixel(c, x + 5, y));
                        //        }
                        //    }
                        //}
                        //else
                        //{
                        //    if (MainWindow.oldAnimation)
                        //        DrawLED(x + 5, bmph, staticBitmapPixels[currentBitmap][x + 5][currentY]);
                        //    else
                        //    {
                        //        int y = bmph;
                        //        Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x + 5][currentY]);

                        //        if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                        //        {
                        //            frameBitmapPixels[x + 5][y] = c;
                        //            changedPixels.Add(new ColorAtPixel(c, x + 5, y));
                        //        }
                        //    }
                        //}

                        //if (borderY > 0)
                        //{
                        //    pixelColor = staticBitmapPixels[currentBitmap][x + 5][borderY - 1];
                        //    if (pixelColor != PreviewWindow.OffLEDColor)
                        //    {
                        //        //      DrawLED(x - 5, borderY + 1, pixelColor);
                        //    }
                        //}
                    }
                    break;
                case 1:
                    for (int x = 4; x < frameSize.Width; x += 8)
                    {
                        if (staticBitmapPixels[currentBitmap][x][currentY] != PreviewWindow.OffLEDColor)
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, bmph, staticBitmapPixels[currentBitmap][x][currentY]);
                            else
                            {
                                int y = bmph;
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                        //if (currentY <= bmph)
                        //{
                        //    if (MainWindow.oldAnimation)
                        //        DrawLED(x - 4, 0, PreviewWindow.OffLEDColor);
                        //    else
                        //    {
                        //        int y = bmph;
                        //        Color c = PreviewWindow.OffLEDColor;//parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                        //        if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                        //        {
                        //            frameBitmapPixels[x - 4][y] = c;
                        //            changedPixels.Add(new ColorAtPixel(c, x - 4, y));
                        //        }
                        //    }
                        //}
                        //if (borderY > 0)
                        //{
                        //    pixelColor = staticBitmapPixels[currentBitmap][x - 4][borderY - 1];
                        //    if (pixelColor != PreviewWindow.OffLEDColor)
                        //    {
                        //        //      DrawLED(x + 2, borderY + 1, pixelColor);
                        //    }
                        //}
                    }
                    break;
                case 2:
                    for (int x = 2; x < frameSize.Width; x += 8)
                    {
                        if (staticBitmapPixels[currentBitmap][x][currentY] != PreviewWindow.OffLEDColor)
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, bmph, staticBitmapPixels[currentBitmap][x][currentY]);
                            else
                            {
                                int y = bmph;
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                        //if (currentY <= bmph)
                        //{
                        //    if (MainWindow.oldAnimation)
                        //        DrawLED(x + 2, 0, PreviewWindow.OffLEDColor);
                        //    else
                        //    {
                        //        int y = bmph;
                        //        Color c = PreviewWindow.OffLEDColor;//parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                        //        if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                        //        {
                        //            frameBitmapPixels[x + 2][y] = c;
                        //            changedPixels.Add(new ColorAtPixel(c, x + 2, y));
                        //        }
                        //    }
                        //}


                        //if (borderY > 0)
                        //{
                        //    pixelColor = staticBitmapPixels[currentBitmap][x + 2][borderY - 1];
                        //    if (pixelColor != PreviewWindow.OffLEDColor)
                        //    {
                        //        //      DrawLED(x + 2, borderY + 1, pixelColor);
                        //    }
                        //}
                    }
                    break;
                case 3:
                    for (int x = 6; x < frameSize.Width; x += 8)
                    {
                        if (staticBitmapPixels[currentBitmap][x][currentY] != PreviewWindow.OffLEDColor)
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, bmph, staticBitmapPixels[currentBitmap][x][currentY]);
                            else
                            {
                                int y = bmph;
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                        //if (currentY <= bmph)
                        //{
                        //    if (MainWindow.oldAnimation)
                        //        DrawLED(x - 4, 0, PreviewWindow.OffLEDColor);
                        //    else
                        //    {
                        //        int y = bmph;
                        //        Color c = PreviewWindow.OffLEDColor;//parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                        //        if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                        //        {
                        //            frameBitmapPixels[x - 4][y] = c;
                        //            changedPixels.Add(new ColorAtPixel(c, x - 4, y));
                        //        }
                        //    }
                        //}

                        //if (borderY > 0)
                        //{
                        //    pixelColor = staticBitmapPixels[currentBitmap][x - 4][borderY - 1];
                        //    if (pixelColor != PreviewWindow.OffLEDColor)
                        //    {
                        //        //   DrawLED(x + 2, borderY + 1, pixelColor);
                        //    }
                        //}
                    }
                    break;
                case 4:
                    for (int x = 1; x < frameSize.Width; x += 8)
                    {
                        if (staticBitmapPixels[currentBitmap][x][currentY] != PreviewWindow.OffLEDColor)
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, bmph, staticBitmapPixels[currentBitmap][x][currentY]);
                            else
                            {
                                int y = bmph;
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                        //if (currentY <= bmph)
                        //{
                        //    if (MainWindow.oldAnimation)
                        //        DrawLED(x + 5, 0, PreviewWindow.OffLEDColor);
                        //    else
                        //    {
                        //        int y = bmph;
                        //        Color c = PreviewWindow.OffLEDColor;//parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                        //        if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                        //        {
                        //            frameBitmapPixels[x + 5][y] = c;
                        //            changedPixels.Add(new ColorAtPixel(c, x + 5, y));
                        //        }
                        //    }
                        //}

                        //if (borderY > 0)
                        //{
                        //    pixelColor = staticBitmapPixels[currentBitmap][x + 5][borderY - 1];
                        //    if (pixelColor != PreviewWindow.OffLEDColor)
                        //    {
                        //        //      DrawLED(x - 5, borderY + 1, pixelColor);
                        //    }
                        //}
                    }
                    break;
                case 5:
                    for (int x = 7; x < frameSize.Width; x += 8)
                    {
                        if (staticBitmapPixels[currentBitmap][x][currentY] != PreviewWindow.OffLEDColor)
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, bmph, staticBitmapPixels[currentBitmap][x][currentY]);
                            else
                            {
                                int y = bmph;
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                        //if (currentY <= bmph)
                        //{
                        //    if (MainWindow.oldAnimation)
                        //        DrawLED(x - 6, 0, PreviewWindow.OffLEDColor);
                        //    else
                        //    {
                        //        int y = bmph;
                        //        Color c = PreviewWindow.OffLEDColor;//parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                        //        if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                        //        {
                        //            frameBitmapPixels[x - 6][y] = c;
                        //            changedPixels.Add(new ColorAtPixel(c, x - 6, y));
                        //        }
                        //    }
                        //}

                        //if (borderY > 0)
                        //{
                        //    pixelColor = staticBitmapPixels[currentBitmap][x - 6][borderY - 1];
                        //    if (pixelColor != PreviewWindow.OffLEDColor)
                        //    {
                        //        //     DrawLED(x + 6, borderY + 1, pixelColor);
                        //    }
                        //}
                    }
                    break;
                case 6:
                    for (int x = 3; x < frameSize.Width; x += 8)
                    {
                        if (staticBitmapPixels[currentBitmap][x][currentY] != PreviewWindow.OffLEDColor)
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, bmph, staticBitmapPixels[currentBitmap][x][currentY]);
                            else
                            {
                                int y = bmph;
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                        //if (currentY <= bmph)
                        //{
                        //    if (MainWindow.oldAnimation)
                        //        DrawLED(x + 4, 0, PreviewWindow.OffLEDColor);
                        //    else
                        //    {
                        //        int y = bmph;
                        //        Color c = PreviewWindow.OffLEDColor;//parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                        //        if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                        //        {
                        //            frameBitmapPixels[x + 4][y] = c;
                        //            changedPixels.Add(new ColorAtPixel(c, x + 4, y));
                        //        }
                        //    }
                        //}

                        //if (borderY > 0)
                        //{
                        //    pixelColor = staticBitmapPixels[currentBitmap][x + 4][borderY - 1];
                        //    if (pixelColor != PreviewWindow.OffLEDColor)
                        //    {
                        //        //      DrawLED(x - 4, borderY + 1, pixelColor);
                        //    }
                        //}
                    }
                    break;
                case 7:
                    for (int x = 5; x < frameSize.Width; x += 8)
                    {
                        if (staticBitmapPixels[currentBitmap][x][currentY] != PreviewWindow.OffLEDColor)
                        {
                            if (MainWindow.oldAnimation)
                                DrawLED(x, bmph, staticBitmapPixels[currentBitmap][x][currentY]);
                            else
                            {
                                int y = bmph;
                                Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                                if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                                {
                                    frameBitmapPixels[x][y] = c;
                                    changedPixels.Add(new ColorAtPixel(c, x, y));
                                }
                            }
                        }
                        //if (currentY <= bmph)
                        //{
                        //    if (MainWindow.oldAnimation)
                        //        DrawLED(x - 2, 0, PreviewWindow.OffLEDColor);
                        //    else
                        //    {
                        //        int y = bmph;
                        //        Color c = PreviewWindow.OffLEDColor;//parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][currentY]);

                        //        if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                        //        {
                        //            frameBitmapPixels[x - 2][y] = c;
                        //            changedPixels.Add(new ColorAtPixel(c, x - 2, y));
                        //        }
                        //    }
                        //}

                        //if (borderY > 0)
                        //{
                        //    pixelColor = staticBitmapPixels[currentBitmap][x - 2][borderY - 1];
                        //    if (pixelColor != PreviewWindow.OffLEDColor)
                        //    {
                        //        //       DrawLED(x + 2, borderY + 1, pixelColor);
                        //    }
                        //}
                    }
                    break;

            }
            #endregion
            if ((currentX) % 8 == 7)// && currentX > 7)
                currentY++;

            if (currentY > bmph)
            {
                lastFrame = true;
                return;
            }

            //if (!firstSnowInNewLine && currentX >= frameSize.Height - 1 && (currentX + (frameSize.Height - 1 - borderY) + (8 - frameSize.Height % 8)) % 8 == 7)// && (frameSize.Height - 1 - borderY) % 8 == 7)
            if (!firstSnowInNewLine && currentX >= frameSize.Height - 1 && (currentX + (borderY) + (8 - frameSize.Height % 8)) % 8 == 7)// && (frameSize.Height - 1 - borderY) % 8 == 7)
            {
                borderY++;
                firstSnowInNewLine = true;
            }
            else if (firstSnowInNewLine)
                firstSnowInNewLine = false;

            currentX = (currentX + 1);
        }
        private void AnimateLaserDown()
        {
            if (lastFrame)
                return;

            if (currentY == 0)
                changedPixels.Clear();

            for (int x = 0; x < frameSize.Width; x++)
            {
                if (MainWindow.oldAnimation)
                {
                    for (int y = currentY; y < frameSize.Height; y++)
                        DrawLED(x, y, staticBitmapPixels[currentBitmap][x][currentY]);
                }
                else
                {
                    int y = currentY;
                    Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    {
                        frameBitmapPixels[x][y] = c;
                        changedPixels.Add(new ColorAtPixel(c, x, y));
                    }

                    y = (frameSize.Height - 1) - currentY;
                    c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    {
                        frameBitmapPixels[x][y] = c;
                        changedPixels.Add(new ColorAtPixel(c, x, y));
                    }
                }
            }
            if (currentY >= (frameSize.Height - 1))
            {
                lastFrame = true;
            }
            else
                currentY++;
        }
        private void AnimateLaserUp()
        {
            if (lastFrame)
                return;

            if (currentY == 0)
                changedPixels.Clear();

            for (int x = 0; x < frameSize.Width; x++)
            {
                if (MainWindow.oldAnimation)
                {
                    for (int y = (frameSize.Height - 1) - currentY; y >= 0; y--)
                        DrawLED(x, y, staticBitmapPixels[currentBitmap][x][(frameSize.Height - 1) - currentY]);
                }
                else
                {
                    int y = currentY;
                    Color c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    {
                        frameBitmapPixels[x][y] = c;
                        changedPixels.Add(new ColorAtPixel(c, x, y));
                    }

                    y = (frameSize.Height - 1) - currentY;
                    c = parentWindow.Owner.ApplyColorType(staticBitmapPixels[currentBitmap][x][y]);

                    if (c.ToArgb() != PreviewWindow.OffLEDColor.ToArgb())
                    {
                        frameBitmapPixels[x][y] = c;
                        changedPixels.Add(new ColorAtPixel(c, x, y));
                    }
                }
            }
            if (currentY >= (frameSize.Height - 1))
            {
                lastFrame = true;
            }
            else
                currentY++;
        }
        #endregion
        #endregion

        public void ClearLEDs(List<Rectangle> intersects)
        {
            //for (int x = 0; x < inputBitmaps[currentBitmap].Width; x++)
            //{
            //    for (int y = 0; y < inputBitmaps[currentBitmap].Height; y++)
            //    {
            //        if (frameBitmapPixels[x][y] != PreviewWindow.OffLEDColor)
            //        {
            //            frameBitmapPixels[x][y] = PreviewWindow.OffLEDColor;
            //            changedPixels.Add(new ColorAtPixel(PreviewWindow.OffLEDColor, x, y));
            //        }

            //        //frameBitmapPixels[x][y] = staticBitmapPixels[currentBitmap][x][y];
            //    }
            //}

            if (MainWindow.oldAnimation)
            {
                try
                {
                    using (Graphics windowGraphics = Graphics.FromImage(parentWindow.windowBitmap))
                    {
                        windowGraphics.Clear(PreviewWindow.OffLEDColor);//SpaceColor);
                        //windowGraphics.Clear(Color.FromArgb(0, PreviewWindow.OffLEDColor));
                    }
                }
                catch { /*System.Diagnostics.Debugger.Break();*/ }
            }
            //else
            {
                if (changedPixels == null)
                    changedPixels = new List<ColorAtPixel>();
                changedPixels.Clear();

                if (frameBitmapPixels == null)
                {
                    frameBitmapPixels = new Color[frameSize.Width][];

                    for (int x = 0; x < frameSize.Width; x++)
                        frameBitmapPixels[x] = new Color[frameSize.Height];
                }

                for (int x = 0; x < frameBitmapPixels.Length; x++)
                    for (int y = 0; y < frameBitmapPixels[0].Length; y++)
                        frameBitmapPixels[x][y] = PreviewWindow.OffLEDColor;
            }
        }

        private void DrawLED(Bitmap targetBmp, int x, int y, Color c)
        {
            Graphics targetGraphics = Graphics.FromImage(targetBmp);
            int newx = x * (PreviewWindow.LedDiam + PreviewWindow.Space);
            int newy = y * (PreviewWindow.LedDiam + PreviewWindow.Space);

            lock (targetGraphics)
            {
                Color tmpc = parentWindow.Owner.ApplyColorType(c);

                //int thisIndex = parentWindow.PreviewWindow.programsWithWindows[parentWindow.Owner.ActiveProgram].IndexOf(parentWindow);
                ////foreach (WindowOnPreview otherWindow in PreviewWindow.programsWithWindows[Owner.ActiveProgram])
                //if (thisIndex < parentWindow.PreviewWindow.programsWithWindows[parentWindow.Owner.ActiveProgram].Count)
                //{
                //    for (int i = thisIndex + 1; i < parentWindow.PreviewWindow.programsWithWindows[parentWindow.Owner.ActiveProgram].Count; i++)
                //    {
                //        if (parentWindow.windowRect.IntersectsWith(parentWindow.PreviewWindow.programsWithWindows[parentWindow.Owner.ActiveProgram][i].windowRect))
                //        {
                //            Rectangle intersectRect = Rectangle.Intersect(parentWindow.windowRect, parentWindow.PreviewWindow.programsWithWindows[parentWindow.Owner.ActiveProgram][i].windowRect);

                //            //for (int tmpx = intersectRect.Left; tmpx < intersectRect.Right; tmpx++)
                //            {
                //                //for (int tmpy = intersectRect.Top; tmpy < intersectRect.Bottom; tmpy++)
                //                {
                //                    //Color tmpColor = parentWindow.windowBitmap.GetPixel(tmpx - (parentWindow.windowRect.X), y - (parentWindow.windowRect.Y));
                //                    //parentWindow.windowBitmap.SetPixel(x - (parentWindow.windowRect.X), tmpy - (parentWindow.windowRect.Y), Color.FromArgb(0, tmpColor));
                //                    if (intersectRect.Contains(newx - (parentWindow.windowRect.X), newy - (parentWindow.windowRect.Y)))
                //                    {
                //                        tmpc = Color.FromArgb(0, tmpc);
                //                        break;
                //                    }
                //                }
                //            }
                //        }
                //    }
                //}


                //if (parentWindow.windowBitmap.GetPixel(newx + PreviewWindow.LedDiam / 2, newy + PreviewWindow.LedDiam / 2) != tmpc)
                {
                    targetGraphics.DrawEllipse(new Pen(tmpc), newx, newy, PreviewWindow.LedDiam, PreviewWindow.LedDiam);
                    targetGraphics.FillEllipse(new SolidBrush(tmpc), newx, newy, PreviewWindow.LedDiam, PreviewWindow.LedDiam);
                }
            }
            targetGraphics.Dispose();
        }

        private void DrawLED(Graphics targetGraphics, int x, int y, Color c)
        {
            int newx = x * (PreviewWindow.LedDiam + PreviewWindow.Space);
            int newy = y * (PreviewWindow.LedDiam + PreviewWindow.Space);

            Color tmpc = parentWindow.Owner.ApplyColorType(c);

            //if (parentWindow.windowBitmap.GetPixel(newx + PreviewWindow.LedDiam / 2, newy + PreviewWindow.LedDiam / 2) != tmpc)
            {
                targetGraphics.DrawEllipse(new Pen(tmpc), newx, newy, PreviewWindow.LedDiam, PreviewWindow.LedDiam);
                targetGraphics.FillEllipse(new SolidBrush(tmpc), newx, newy, PreviewWindow.LedDiam, PreviewWindow.LedDiam);
            }
            
        }

        private void DrawLED(int x, int y, Color c)
        {
            Color tmpc = parentWindow.Owner.ApplyColorType(c);

            if (frameBitmapPixels[x][y].ToArgb() != tmpc.ToArgb())
            {

                frameBitmapPixels[x][y] = tmpc;

                if (MainWindow.anotherBitmaps)
                    changedPixels.Add(new ColorAtPixel(tmpc, x, y));
                else
                {
                    try
                    {

                        //lock (parentWindow.testbmp)
                        //{
                        //    parentWindow.testbmp.SetPixel(x, y, tmpc);
                        //}
                        //return;

                        using (Graphics windowGraphics = Graphics.FromImage(parentWindow.windowBitmap))
                        {
                            int newx = x * (PreviewWindow.LedDiam + PreviewWindow.Space);
                            int newy = y * (PreviewWindow.LedDiam + PreviewWindow.Space);

                            lock (windowGraphics)
                            {
                                //if (tmpc.ToArgb() == PreviewWindow.OffLEDColor.ToArgb())
                                //    tmpc = Color.FromArgb(0, tmpc);


                                //int thisIndex = parentWindow.PreviewWindow.programsWithWindows[parentWindow.Owner.ActiveProgram].IndexOf(parentWindow);
                                ////foreach (WindowOnPreview otherWindow in PreviewWindow.programsWithWindows[Owner.ActiveProgram])
                                //if (thisIndex < parentWindow.PreviewWindow.programsWithWindows[parentWindow.Owner.ActiveProgram].Count)
                                //{
                                //    for (int i = thisIndex + 1; i < parentWindow.PreviewWindow.programsWithWindows[parentWindow.Owner.ActiveProgram].Count; i++)
                                //    {
                                //        if (parentWindow.windowRect.IntersectsWith(parentWindow.PreviewWindow.programsWithWindows[parentWindow.Owner.ActiveProgram][i].windowRect))
                                //        {
                                //            Rectangle intersectRect = Rectangle.Intersect(parentWindow.windowRect, parentWindow.PreviewWindow.programsWithWindows[parentWindow.Owner.ActiveProgram][i].windowRect);

                                //            //for (int tmpx = intersectRect.Left; tmpx < intersectRect.Right; tmpx++)
                                //            {
                                //                //for (int tmpy = intersectRect.Top; tmpy < intersectRect.Bottom; tmpy++)
                                //                {
                                //                    //Color tmpColor = parentWindow.windowBitmap.GetPixel(tmpx - (parentWindow.windowRect.X), y - (parentWindow.windowRect.Y));
                                //                    //parentWindow.windowBitmap.SetPixel(x - (parentWindow.windowRect.X), tmpy - (parentWindow.windowRect.Y), Color.FromArgb(0, tmpColor));
                                //                    if (intersectRect.Contains(newx - (parentWindow.windowRect.X), newy - (parentWindow.windowRect.Y)))
                                //                    {
                                //                        tmpc = Color.FromArgb(0, tmpc);
                                //                        break;
                                //                    }
                                //                }
                                //            }
                                //        }
                                //    }
                                //}
                                lock (parentWindow.windowBitmap)
                                {
                                    try
                                    {
                                        //if (parentWindow.windowBitmap.GetPixel(newx + PreviewWindow.LedDiam / 2, newy + PreviewWindow.LedDiam / 2).ToArgb() != tmpc.ToArgb())
                                        {

                                            windowGraphics.DrawEllipse(new Pen(tmpc), newx, newy, PreviewWindow.LedDiam, PreviewWindow.LedDiam);
                                            windowGraphics.FillEllipse(new SolidBrush(tmpc), newx, newy, PreviewWindow.LedDiam, PreviewWindow.LedDiam);
                                        }
                                    }
                                    catch { /*System.Diagnostics.Debugger.Break();*/ }
                                }
                            }
                        }
                    }
                    catch { /*System.Diagnostics.Debugger.Break();*/ }
                }
            }
        }

        private void SetupTimer()
        {
            if (InputBitmaps != null && InputBitmaps.Count > 0)
            {
                int timerInterval = (PlaybillItem.ItemType == PlayWindowItemType.Animator) ? 1 : (Convert.ToInt32((1 - ((speed - 1) / 100.0)) * 10 * 100));
                SetupVariables();

                if (UpdateTimer == null)
                {
                    UpdateTimer = new System.Windows.Forms.Timer();
                    UpdateTimer.Interval = timerInterval;
                    UpdateTimer.Tick += new EventHandler(Timer_Tick);
                    UpdateTimer.Start();
                }
                else if (UpdateTimer.Interval != timerInterval)
                {
                    UpdateTimer.Interval = timerInterval;
                }
            }
        }

        private void Timer_Tick(Object sender, EventArgs e)
        {
            DateTime start = DateTime.Now;

            if (!parentWindow.Owner.Visible)
            {
                StopAnimation();
                return;
            }

            if (lastFrame && !stayNow)
            {
                if (Effect.ToString().Contains("Scroll") && !Effect.ToString().Contains("Cont"))
                {
                    if (InputBitmaps.Count == staticBitmapPixels.Length)
                    {
                        Color[][][] tmparray = new Color[staticBitmapPixels.Length + 1][][];




                        if (Effect.ToString().Contains("Left") || Effect.ToString().Contains("Up") || Effect.ToString().Contains("Down"))
                        {
                            for (int index = 0; index < staticBitmapPixels.Length; index++)
                                tmparray[index] = staticBitmapPixels[index];

                            staticBitmapPixels = new Color[tmparray.Length][][];

                            for (int index = 0; index < staticBitmapPixels.Length; index++)
                                staticBitmapPixels[index] = tmparray[index];


                            int i = staticBitmapPixels.Length - 1;

                            staticBitmapPixels[i] = new Color[frameSize.Width][];

                            for (int x = 0; x < frameSize.Width; x++)
                            {
                                staticBitmapPixels[i][x] = new Color[frameSize.Height];
                            }

                            for (int x = 0; x < frameSize.Width; x++)
                            {
                                for (int y = 0; y < frameSize.Height; y++)
                                {
                                    Color currentPixel = parentWindow.Owner.ApplyColorType(Color.Black);
                                    staticBitmapPixels[i][x][y] = currentPixel;
                                }
                            }
                            currentBitmap = i;
                        }
                        else if (Effect.ToString().Contains("Right"))// || Effect.ToString().Contains("Down"))
                        {
                            for (int index = 0; index < staticBitmapPixels.Length; index++)
                                tmparray[index+1] = staticBitmapPixels[index];

                            staticBitmapPixels = new Color[tmparray.Length][][];

                            for (int index = 1; index < staticBitmapPixels.Length; index++)
                                staticBitmapPixels[index] = tmparray[index];


                            int i = 0;

                            staticBitmapPixels[i] = new Color[frameSize.Width][];

                            for (int x = 0; x < frameSize.Width; x++)
                            {
                                staticBitmapPixels[i][x] = new Color[frameSize.Height];
                            }

                            for (int x = 0; x < frameSize.Width; x++)
                            {
                                for (int y = 0; y < frameSize.Height; y++)
                                {
                                    Color currentPixel = parentWindow.Owner.ApplyColorType(Color.Black);
                                    staticBitmapPixels[i][x][y] = currentPixel;
                                }
                            }
                            currentBitmap = i;
                        }


                        if (Effect.ToString().Contains("Left"))
                            currentX = frameSize.Width - 1;
                        else if (Effect.ToString().Contains("Right"))
                            currentX = 0;
                        else if (Effect.ToString().Contains("Up"))
                            currentY = frameSize.Height - 1;
                        else if (Effect.ToString().Contains("Down"))
                            currentY = 0;

                        lastFrame = false;
                        return;

                    }
                    else
                    {
                        Color[][][] tmparray = new Color[staticBitmapPixels.Length - 1][][];
                        if (Effect.ToString().Contains("Left") || Effect.ToString().Contains("Up") || Effect.ToString().Contains("Down"))
                        {
                            for (int index = 0; index < tmparray.Length; index++)
                                tmparray[index] = staticBitmapPixels[index];

                            staticBitmapPixels = new Color[tmparray.Length][][];

                            for (int index = 0; index < staticBitmapPixels.Length; index++)
                                staticBitmapPixels[index] = tmparray[index];

                            currentBitmap = staticBitmapPixels.Length;
                        }
                        else if (Effect.ToString().Contains("Right"))// || Effect.ToString().Contains("Down"))
                        {
                            for (int index = 0; index < tmparray.Length; index++)
                                tmparray[index] = staticBitmapPixels[index+1];

                            staticBitmapPixels = new Color[tmparray.Length][][];

                            for (int index = 0; index < staticBitmapPixels.Length; index++)
                                staticBitmapPixels[index] = tmparray[index];

                            currentBitmap = 0;
                        }
                    }
                }


                animationEnd = DateTime.Now;
                Console.WriteLine("Animation - {0}; Old - {1}; Time - {2}s", Effect.ToString(), (!MainWindow.anotherBitmaps).ToString(),
                        ((((TimeSpan)(animationEnd - animationStart)).TotalMilliseconds) / 1000).ToString());

                stayNow = true;
                UpdateTimer.Interval = (StayTime > 0) ? ((PlaybillItem.ItemType == PlayWindowItemType.Animator) ? PlaybillItem.FrameDelay*10 : StayTime * 1000) : 1;

                if (Effect == EffectType.ScrollLeftCont || Effect == EffectType.ScrollRightCont
                     || Effect == EffectType.ScrollLeft || Effect == EffectType.ScrollRight
                      || Effect == EffectType.ScrollUpCont || Effect == EffectType.ScrollDownCont
                       || Effect == EffectType.ScrollUp || Effect == EffectType.ScrollDown)
                    UpdateTimer.Interval = 1;
            }
            else if (lastFrame && stayNow)
            {
                lastFrame = false;
                stayNow = false;

                animationStart = DateTime.Now;

                //if (Effect.ToString().Contains("Scroll") && !Effect.ToString().Contains("Cont"))
                //{
                //    if (InputBitmaps.Count != staticBitmapPixels.Length)
                //    {
                //        Color[][][] tmparray = new Color[staticBitmapPixels.Length - 1][][];
                //        for (int index = 0; index < tmparray.Length; index++)
                //            tmparray[index] = staticBitmapPixels[index];

                //        staticBitmapPixels.CopyTo(tmparray, 0);
                //        staticBitmapPixels = new Color[tmparray.Length][][];

                //        for (int index = 0; index < staticBitmapPixels.Length; index++)
                //            staticBitmapPixels[index] = tmparray[index];
                //    }
                //}

                if ((Effect.ToString().Contains("Scroll") && (Effect.ToString().Contains("Right"))) &&--currentBitmap > 0)
                        SetupTimer();
                else if ((!Effect.ToString().Contains("Scroll")
                            || (Effect.ToString().Contains("Scroll") && (Effect.ToString().Contains("Left") || Effect.ToString().Contains("Up") || Effect.ToString().Contains("Down"))))
                    && ++currentBitmap < InputBitmaps.Count)
                    SetupTimer();
                else
                {
                    //Effect == EffectType.ScrollLeftCont
                    if (( Effect.ToString().Contains("Scroll")|| PlaybillItem.ItemType == PlayWindowItemType.Animator)&& scrollTimes > 0)
                    {
                        Console.WriteLine("Effect: {0} Scrolltimes: {1}", Effect.ToString(), scrollTimes);
                        scrollTimes--;
                        currentBitmap = 0;
                        StopAnimation();
                        parentWindow.CurrentAnimation.Animate(animateNext);
                    }
                    else
                    {
                        currentBitmap = 0;
                        scrollTimes = -1;
                        StopAnimation();

                        if (animateNext)
                            parentWindow.CurrentAnimationNumber++;
                        parentWindow.CurrentAnimation.Animate(animateNext);
                    }
                }
            }
            else
            {
                switch (Effect)
                {
                    case EffectType.Instant:
                        AnimateInstant();
                        break;
                    case EffectType.OpenLeft:
                        AnimateOpenLeft();
                        AnimateOpenLeft();
                        break;
                    case EffectType.OpenRight:
                        AnimateOpenRight();
                        AnimateOpenRight();
                        break;
                    case EffectType.OpenLeftRight:
                        AnimateOpenLeftRight();
                        AnimateOpenLeftRight();
                        break;
                    case EffectType.OpenUpDown:
                        AnimateOpenUpDown();
                        AnimateOpenUpDown();
                        break;
                    case EffectType.LaserDown:
                        AnimateLaserDown();
                        AnimateLaserDown();
                        break;
                    case EffectType.LaserUp:
                        AnimateLaserUp();
                        break;
                    case EffectType.BlindHorizontial:
                        AnimateBlindHorizontial();
                        AnimateBlindHorizontial();
                        break;
                    case EffectType.OpenHorizontal:
                        AnimateOpenHorizontal();
                        AnimateOpenHorizontal();
                        break;
                    case EffectType.OpenVertical:
                        AnimateOpenVertical();
                        AnimateOpenVertical();
                        break;
                    case EffectType.SnowFall:
                        AnimateSnow();
                        AnimateSnow();
                        break;
                    case EffectType.ExpandTop:
                        AnimateExpandTop();
                        AnimateExpandTop();
                        break;
                    case EffectType.ExpandBottom:
                        AnimateExpandBottom();
                        AnimateExpandBottom();
                        break;
                    case EffectType.ExpandVertical:
                        AnimateExpandVertical();
                        AnimateExpandVertical();
                        break;
                    case EffectType.ShiftLeft:
                        AnimateShiftLeft();
                        AnimateShiftLeft();
                        break;
                    case EffectType.ShiftRight:
                        AnimateShiftRight();
                        AnimateShiftRight();
                        break;
                    case EffectType.ShiftUp:
                        AnimateShiftUp();
                        AnimateShiftUp();
                        break;
                    case EffectType.ShiftDown:
                        AnimateShiftDown();
                        AnimateShiftDown();
                        break;
                    case EffectType.ScrollUp:
                        AnimateScrollUp();
                        AnimateScrollUp();
                        break;
                    case EffectType.ScrollDown:
                        AnimateScrollDown();
                        AnimateScrollDown();
                        break;
                    case EffectType.ScrollUpCont:
                        AnimateScrollUpCont();
                        AnimateScrollUpCont();
                        break;
                    case EffectType.ScrollDownCont:
                        AnimateScrollDownCont();
                        AnimateScrollDownCont();
                        break;
                    case EffectType.ScrollLeftCont:
                        AnimateScrollLeftCont();
                        AnimateScrollLeftCont();
                        break;
                    case EffectType.ScrollRightCont:
                        AnimateScrollRightCont();
                        AnimateScrollRightCont();
                        break;
                    case EffectType.ScrollLeft:
                        AnimateScrollLeft();
                        AnimateScrollLeft();
                        break;
                    case EffectType.ScrollRight:
                        AnimateScrollRight();
                        AnimateScrollRight();
                        break;
                    case EffectType.Blink:
                        AnimateBlink();
                        break;
                    case EffectType.BlinkInverse:
                        AnimateBlinkInverse();
                        break;
                    case EffectType.OpenClockwise:
                        AnimateOpenClock();
                        break;
                    case EffectType.OpenAntiClockwise:
                        AnimateOpenClock();
                        break;
                    case EffectType.WindmillClockwise:
                        AnimateWindmill();
                        break;
                    case EffectType.WindmillAntiClockwise:
                        AnimateWindmill();
                        break;
                    case EffectType.Open2Fan:
                        AnimateOpen2Fan();
                        break;
                    case EffectType.ZebraHorizontal:
                        AnimateZebraHorizontal();
                        AnimateZebraHorizontal();
                        break;
                    case EffectType.ZebraVertical:
                        AnimateZebraVertical();
                        AnimateZebraVertical();
                        break;
                    case EffectType.RectangleOut:
                        AnimateRectangleOut();
                        AnimateRectangleOut();
                        break;
                    case EffectType.RectangleIn:
                        AnimateRectangleIn();
                        AnimateRectangleIn();
                        break;
                    case EffectType.CornerOut:
                        AnimateCornerOut();
                        AnimateCornerOut();
                        break;
                    case EffectType.CornerIn:
                        AnimateCornerIn();
                        AnimateCornerIn();
                        break;
                    case EffectType.OpenTopLeft:
                        AnimateOpenTopLeft();
                        AnimateOpenTopLeft();
                        break;
                    case EffectType.OpenTopRight:
                        AnimateOpenTopRight();
                        AnimateOpenTopRight();
                        break;
                    case EffectType.OpenBottomLeft:
                        AnimateOpenBottomLeft();
                        AnimateOpenBottomLeft();
                        break;
                    case EffectType.OpenBottomRight:
                        AnimateOpenBottomRight();
                        AnimateOpenBottomRight();
                        break;
                    case EffectType.OpenSlash:
                        AnimateOpenSlash();
                        AnimateOpenSlash();
                        break;
                    case EffectType.OpenBackslash:
                        AnimateOpenBackslash();
                        AnimateOpenBackslash();
                        break;
                    case EffectType.RoundOut:
                        AnimateRoundOut();
                        AnimateRoundOut();
                        break;
                    case EffectType.SlideZebraHorizontal:
                        AnimateSlideZebraHorizontal();
                        AnimateSlideZebraHorizontal();
                        break;
                    case EffectType.SlideZebraVertical:
                        AnimateSlideZebraVertical();
                        AnimateSlideZebraVertical();
                        break;
                    case EffectType.SlideTopLeft:
                        AnimateSlideTopLeft();
                        AnimateSlideTopLeft();
                        break;
                    case EffectType.SlideTopRight:
                        AnimateSlideTopRight();
                        AnimateSlideTopRight();
                        break;
                    case EffectType.SlideBottomLeft:
                        AnimateSlideBottomLeft();
                        AnimateSlideBottomLeft();
                        break;
                    case EffectType.SlideBottomRight:
                        AnimateSlideBottomRight();
                        AnimateSlideBottomRight();
                        break;
                    case EffectType.SlewingOut:
                        AnimateSlewingOut();
                        AnimateSlewingOut();
                        break;
                    case EffectType.ScrapeUp:
                        AnimateScrapeUp();
                        AnimateScrapeUp();
                        break;
                    case EffectType.Bubbles:
                        AnimateBubbles();
                        AnimateBubbles();
                        break;
                    case EffectType.ShutterHorizontal:
                        AnimateShutterHorizontal();
                        AnimateShutterHorizontal();
                        break;
                    case EffectType.ShutterVertical:
                        AnimateShutterVertical();
                        AnimateShutterVertical();
                        break;
                    case EffectType.ChessboardHorizontal:
                        AnimateChessboardHorizontal();
                        AnimateChessboardHorizontal();
                        break;
                }
                parentWindow.Redraw();
                //using(Bitmap b = new Bitmap(frameBitmapPixels.Length, frameBitmapPixels[0].Length, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                //{
                //    for (int x = 0; x < b.Width; x++)
                //    {
                //        for (int y = 0; y < b.Height; y++)
                //        {
                //            b.SetPixel(x, y, frameBitmapPixels[x][y]);
                //        }
                //    }
                //    b.Save(DateTime.Now.ToFileTime().ToString() + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);
                //}
                
                DateTime end = DateTime.Now;
                TimeSpan tickLength =  end - start;
                //if (tickLength.TotalMilliseconds >= UpdateTimer.Interval)
                //    UpdateTimer.Interval = 1;
                //else
                //{
                //    UpdateTimer.Interval = UpdateTimer.Interval - (int)tickLength.TotalMilliseconds;
                //}
                //MessageBox.Show((((TimeSpan)(end - start)).TotalMilliseconds).ToString(), "AnimateInstant");
                //Console.WriteLine("TimerTick: time - {0}ms; points - {1}", (((TimeSpan)(end - start)).TotalMilliseconds).ToString(), changedPixels.Count);
            }
        }

         public void AnimateFrame_new()
        {
            DateTime start = DateTime.Now;

            if (!parentWindow.Owner.Visible)
            {
                StopAnimation();
                return;
            }

            if (lastFrame && !stayNow)
            {
                if (Effect.ToString().Contains("Scroll") && !Effect.ToString().Contains("Cont"))
                {
                    if (InputBitmaps.Count == staticBitmapPixels.Length)
                    {
                        Color[][][] tmparray = new Color[staticBitmapPixels.Length + 1][][];




                        if (Effect.ToString().Contains("Left") || Effect.ToString().Contains("Up") || Effect.ToString().Contains("Down"))
                        {
                            for (int index = 0; index < staticBitmapPixels.Length; index++)
                                tmparray[index] = staticBitmapPixels[index];

                            staticBitmapPixels = new Color[tmparray.Length][][];

                            for (int index = 0; index < staticBitmapPixels.Length; index++)
                                staticBitmapPixels[index] = tmparray[index];


                            int i = staticBitmapPixels.Length - 1;

                            staticBitmapPixels[i] = new Color[frameSize.Width][];

                            for (int x = 0; x < frameSize.Width; x++)
                            {
                                staticBitmapPixels[i][x] = new Color[frameSize.Height];
                            }

                            for (int x = 0; x < frameSize.Width; x++)
                            {
                                for (int y = 0; y < frameSize.Height; y++)
                                {
                                    Color currentPixel = parentWindow.Owner.ApplyColorType(Color.Black);
                                    staticBitmapPixels[i][x][y] = currentPixel;
                                }
                            }
                            currentBitmap = i;
                        }
                        else if (Effect.ToString().Contains("Right"))// || Effect.ToString().Contains("Down"))
                        {
                            for (int index = 0; index < staticBitmapPixels.Length; index++)
                                tmparray[index+1] = staticBitmapPixels[index];

                            staticBitmapPixels = new Color[tmparray.Length][][];

                            for (int index = 1; index < staticBitmapPixels.Length; index++)
                                staticBitmapPixels[index] = tmparray[index];


                            int i = 0;

                            staticBitmapPixels[i] = new Color[frameSize.Width][];

                            for (int x = 0; x < frameSize.Width; x++)
                            {
                                staticBitmapPixels[i][x] = new Color[frameSize.Height];
                            }

                            for (int x = 0; x < frameSize.Width; x++)
                            {
                                for (int y = 0; y < frameSize.Height; y++)
                                {
                                    Color currentPixel = parentWindow.Owner.ApplyColorType(Color.Black);
                                    staticBitmapPixels[i][x][y] = currentPixel;
                                }
                            }
                            currentBitmap = i;
                        }


                        if (Effect.ToString().Contains("Left"))
                            currentX = frameSize.Width - 1;
                        else if (Effect.ToString().Contains("Right"))
                            currentX = 0;
                        else if (Effect.ToString().Contains("Up"))
                            currentY = frameSize.Height - 1;
                        else if (Effect.ToString().Contains("Down"))
                            currentY = 0;

                        lastFrame = false;
                        return;

                    }
                    else
                    {
                        Color[][][] tmparray = new Color[staticBitmapPixels.Length - 1][][];
                        if (Effect.ToString().Contains("Left") || Effect.ToString().Contains("Up") || Effect.ToString().Contains("Down"))
                        {
                            for (int index = 0; index < tmparray.Length; index++)
                                tmparray[index] = staticBitmapPixels[index];

                            staticBitmapPixels = new Color[tmparray.Length][][];

                            for (int index = 0; index < staticBitmapPixels.Length; index++)
                                staticBitmapPixels[index] = tmparray[index];

                            currentBitmap = staticBitmapPixels.Length;
                        }
                        else if (Effect.ToString().Contains("Right"))// || Effect.ToString().Contains("Down"))
                        {
                            for (int index = 0; index < tmparray.Length; index++)
                                tmparray[index] = staticBitmapPixels[index+1];

                            staticBitmapPixels = new Color[tmparray.Length][][];

                            for (int index = 0; index < staticBitmapPixels.Length; index++)
                                staticBitmapPixels[index] = tmparray[index];

                            currentBitmap = 0;
                        }
                    }
                }


                animationEnd = DateTime.Now;
                Console.WriteLine("Animation - {0}; Old - {1}; Time - {2}s", Effect.ToString(), MainWindow.oldAnimation.ToString(),
                        ((((TimeSpan)(animationEnd - animationStart)).TotalMilliseconds) / 1000).ToString());

                stayNow = true;
                //UpdateTimer.Interval = (StayTime > 0) ? ((PlaybillItem.ItemType == PlayWindowItemType.Animator) ? StayTime : StayTime * 1000) : 1;
                if (UpdateTimer != null)
                    UpdateTimer.Interval = (StayTime > 0) ? ((PlaybillItem.ItemType == PlayWindowItemType.Animator) ? StayTime : StayTime * 1000) : 1;
                else if (parentWindow.animationTimer != null)
                    parentWindow.animationTimer.Change((StayTime > 0) ? ((PlaybillItem.ItemType == PlayWindowItemType.Animator) ? StayTime : StayTime * 1000) : 1, System.Threading.Timeout.Infinite);


                if (Effect == EffectType.ScrollLeftCont || Effect == EffectType.ScrollRightCont
                     || Effect == EffectType.ScrollLeft || Effect == EffectType.ScrollRight
                      || Effect == EffectType.ScrollUpCont || Effect == EffectType.ScrollDownCont
                       || Effect == EffectType.ScrollUp || Effect == EffectType.ScrollDown)
                    UpdateTimer.Interval = 1;
            }
            else if (lastFrame && stayNow)
            {
                //lastFrame = false;
                //stayNow = false;

                //if (Effect.ToString().Contains("Scroll") && !Effect.ToString().Contains("Cont"))
                //{
                //    if (InputBitmaps.Count != staticBitmapPixels.Length)
                //    {
                //        Color[][][] tmparray = new Color[staticBitmapPixels.Length - 1][][];
                //        for (int index = 0; index < tmparray.Length; index++)
                //            tmparray[index] = staticBitmapPixels[index];

                //        staticBitmapPixels.CopyTo(tmparray, 0);
                //        staticBitmapPixels = new Color[tmparray.Length][][];

                //        for (int index = 0; index < staticBitmapPixels.Length; index++)
                //            staticBitmapPixels[index] = tmparray[index];
                //    }
                //}


                lastFrame = false;
                stayNow = false;
                running = false;

                if (++currentBitmap < InputBitmaps.Count)
                {
                    int timerInterval = (PlaybillItem.ItemType == PlayWindowItemType.Animator) ?
                        1 : (Convert.ToInt32((1 - ((Speed - 1) / 100.0)) * 10 * 100));

                    SetupVariables();
                    parentWindow.animationTimer.Change(0, timerInterval);
                    //SetupTimer();
                }
                else
                {
                    //if (Effect == EffectType.ScrollLeftCont && scrollTimes > 0)
                    //{
                    //    scrollTimes--;
                    //    currentBitmap = 0;
                    //    StopAnimation();
                    //    parentWindow.Animations[parentWindow.CurrentAnimation].Animate(animateNext);
                    //}
                    //else
                    currentBitmap = 0;
                    StopAnimation();
                    //if(false)
                    {

                        //  if (animateNext)
                        //    parentWindow.CurrentAnimationNumber++;
                        //parentWindow.CurrentAnimation.Animate(animateNext);

                        if (!animateNext)
                        {
                            int timerInterval = (PlaybillItem.ItemType == PlayWindowItemType.Animator) ?
                                    1 : (Convert.ToInt32((1 - ((Speed - 1) / 100.0)) * 10 * 100));

                            parentWindow.animationTimer.Change(1, timerInterval);
                        }
                    }
                }

                //if ((Effect.ToString().Contains("Scroll") && (Effect.ToString().Contains("Right"))) &&--currentBitmap > 0)
                //        SetupTimer();
                //else if ((!Effect.ToString().Contains("Scroll")
                //            || (Effect.ToString().Contains("Scroll") && (Effect.ToString().Contains("Left") || Effect.ToString().Contains("Up") || Effect.ToString().Contains("Down"))))
                //    && ++currentBitmap < InputBitmaps.Count)
                //    SetupTimer();
                //else
                //{
                //    //Effect == EffectType.ScrollLeftCont
                //    if (( Effect.ToString().Contains("Scroll")|| PlaybillItem.ItemType == PlayWindowItemType.Animator)&& scrollTimes > 0)
                //    {
                //        Console.WriteLine("Effect: {0} Scrolltimes: {1}", Effect.ToString(), scrollTimes);
                //        scrollTimes--;
                //        currentBitmap = 0;
                //        StopAnimation();
                //        parentWindow.CurrentAnimation.Animate(animateNext);
                //    }
                //    else
                //    {
                //        currentBitmap = 0;
                //        scrollTimes = -1;
                //        StopAnimation();

                //        if (animateNext)
                //            parentWindow.CurrentAnimationNumber++;
                //        parentWindow.CurrentAnimation.Animate(animateNext);
                //    }
                //}
            }
            else
            {
                switch (Effect)
                {
                    case EffectType.Instant:
                        AnimateInstant();
                        break;
                    case EffectType.OpenLeft:
                        AnimateOpenLeft();
                        break;
                    case EffectType.OpenRight:
                        AnimateOpenRight();
                        break;
                    case EffectType.OpenLeftRight:
                        AnimateOpenLeftRight();
                        break;
                    case EffectType.OpenUpDown:
                        AnimateOpenUpDown();
                        break;
                    case EffectType.LaserDown:
                        AnimateLaserDown();
                        break;
                    case EffectType.LaserUp:
                        AnimateLaserUp();
                        break;
                    case EffectType.BlindHorizontial:
                        AnimateBlindHorizontial();
                        break;
                    case EffectType.OpenHorizontal:
                        AnimateOpenHorizontal();
                        break;
                    case EffectType.OpenVertical:
                        AnimateOpenVertical();
                        break;
                    case EffectType.SnowFall:
                        AnimateSnow();
                        break;
                    case EffectType.ExpandTop:
                        AnimateExpandTop();
                        break;
                    case EffectType.ExpandBottom:
                        AnimateExpandBottom();
                        break;
                    case EffectType.ExpandVertical:
                        AnimateExpandVertical();
                        break;
                    case EffectType.ShiftLeft:
                        AnimateShiftLeft();
                        break;
                    case EffectType.ShiftRight:
                        AnimateShiftRight();
                        break;
                    case EffectType.ShiftUp:
                        AnimateShiftUp();
                        break;
                    case EffectType.ShiftDown:
                        AnimateShiftDown();
                        break;
                    case EffectType.ScrollUp:
                        AnimateScrollUp();
                        break;
                    case EffectType.ScrollDown:
                        AnimateScrollDown();
                        break;
                    case EffectType.ScrollUpCont:
                        AnimateScrollUpCont();
                        break;
                    case EffectType.ScrollDownCont:
                        AnimateScrollDownCont();
                        break;
                    case EffectType.ScrollLeftCont:
                        AnimateScrollLeftCont();
                        break;
                    case EffectType.ScrollRightCont:
                        AnimateScrollRightCont();
                        break;
                    case EffectType.ScrollLeft:
                        AnimateScrollLeft();
                        break;
                    case EffectType.ScrollRight:
                        AnimateScrollRight();
                        break;
                    case EffectType.Blink:
                        AnimateBlink();
                        break;
                    case EffectType.BlinkInverse:
                        AnimateBlinkInverse();
                        break;
                    case EffectType.OpenClockwise:
                        AnimateOpenClock();
                        break;
                    case EffectType.OpenAntiClockwise:
                        AnimateOpenClock();
                        break;
                    case EffectType.WindmillClockwise:
                        AnimateWindmill();
                        break;
                    case EffectType.WindmillAntiClockwise:
                        AnimateWindmill();
                        break;
                    case EffectType.Open2Fan:
                        AnimateOpen2Fan();
                        break;
                    case EffectType.ZebraHorizontal:
                        AnimateZebraHorizontal();
                        break;
                    case EffectType.ZebraVertical:
                        AnimateZebraVertical();
                        break;
                    case EffectType.RectangleOut:
                        AnimateRectangleOut();
                        break;
                    case EffectType.RectangleIn:
                        AnimateRectangleIn();
                        break;
                    case EffectType.CornerOut:
                        AnimateCornerOut();
                        break;
                    case EffectType.CornerIn:
                        AnimateCornerIn();
                        break;
                    case EffectType.OpenTopLeft:
                        AnimateOpenTopLeft();
                        break;
                    case EffectType.OpenTopRight:
                        AnimateOpenTopRight();
                        break;
                    case EffectType.OpenBottomLeft:
                        AnimateOpenBottomLeft();
                        break;
                    case EffectType.OpenBottomRight:
                        AnimateOpenBottomRight();
                        break;
                    case EffectType.OpenSlash:
                        AnimateOpenSlash();
                        break;
                    case EffectType.OpenBackslash:
                        AnimateOpenBackslash();
                        break;
                    case EffectType.RoundOut:
                        AnimateRoundOut();
                        break;
                    case EffectType.SlideZebraHorizontal:
                        AnimateSlideZebraHorizontal();
                        break;
                    case EffectType.SlideZebraVertical:
                        AnimateSlideZebraVertical();
                        break;
                    case EffectType.SlideTopLeft:
                        AnimateSlideTopLeft();
                        break;
                    case EffectType.SlideTopRight:
                        AnimateSlideTopRight();
                        break;
                    case EffectType.SlideBottomLeft:
                        AnimateSlideBottomLeft();
                        break;
                    case EffectType.SlideBottomRight:
                        AnimateSlideBottomRight();
                        break;
                    case EffectType.SlewingOut:
                        AnimateSlewingOut();
                        break;
                    case EffectType.ScrapeUp:
                        AnimateScrapeUp();
                        break;
                    case EffectType.Bubbles:
                        AnimateBubbles();
                        break;
                    case EffectType.ShutterHorizontal:
                        AnimateShutterHorizontal();
                        break;
                    case EffectType.ShutterVertical:
                        AnimateShutterVertical();
                        break;
                    case EffectType.ChessboardHorizontal:
                        AnimateChessboardHorizontal();
                        break;
                }
                parentWindow.Redraw();
                //using(Bitmap b = new Bitmap(frameBitmapPixels.Length, frameBitmapPixels[0].Length, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                //{
                //    for (int x = 0; x < b.Width; x++)
                //    {
                //        for (int y = 0; y < b.Height; y++)
                //        {
                //            b.SetPixel(x, y, frameBitmapPixels[x][y]);
                //        }
                //    }
                //    b.Save(DateTime.Now.ToFileTime().ToString() + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);
                //}
                
                DateTime end = DateTime.Now;
                TimeSpan tickLength =  end - start;
                //if (tickLength.TotalMilliseconds >= UpdateTimer.Interval)
                //    UpdateTimer.Interval = 1;
                //else
                //{
                //    UpdateTimer.Interval = UpdateTimer.Interval - (int)tickLength.TotalMilliseconds;
                //}
                //MessageBox.Show((((TimeSpan)(end - start)).TotalMilliseconds).ToString(), "AnimateInstant");
                //Console.WriteLine("TimerTick: time - {0}ms; points - {1}", (((TimeSpan)(end - start)).TotalMilliseconds).ToString(), changedPixels.Count);
            }
        }



         public void AnimateFrame()
         {
             if (!parentWindow.Owner.Visible)
             {
                 StopAnimation();
                 return;
             }

             if (!running)
                 Animate(animateNext);


             if (lastFrame && !stayNow)
             {
                 stayNow = true;
                 if (UpdateTimer != null)
                     UpdateTimer.Interval = (StayTime > 0) ? ((PlaybillItem.ItemType == PlayWindowItemType.Animator) ? StayTime : StayTime * 1000) : 1;
                 else if (parentWindow.animationTimer != null)
                     parentWindow.animationTimer.Change((StayTime > 0) ? ((PlaybillItem.ItemType == PlayWindowItemType.Animator) ? StayTime : StayTime * 1000) : 1, System.Threading.Timeout.Infinite);

                 if ((Effect == EffectType.ScrollLeftCont || Effect == EffectType.ScrollRightCont
                      || Effect == EffectType.ScrollLeft || Effect == EffectType.ScrollRight
                       || Effect == EffectType.ScrollUpCont || Effect == EffectType.ScrollDownCont
                        || Effect == EffectType.ScrollUp || Effect == EffectType.ScrollDown))
                     UpdateTimer.Interval = 1;

                 DateTime animationEnd = DateTime.Now;
                 Console.WriteLine("Animation - {0}; Old - {1}; Time - {2}s", Effect.ToString(), MainWindow.oldAnimation.ToString(),
                        ((((TimeSpan)(animationEnd - animationStart)).TotalMilliseconds) / 1000).ToString());

             }
             else if (lastFrame && stayNow)
             {

                 lastFrame = false;
                 stayNow = false;
                 running = false;

                 if (++currentBitmap < InputBitmaps.Count)
                 {
                     int timerInterval = (PlaybillItem.ItemType == PlayWindowItemType.Animator) ?
                         1 : (Convert.ToInt32((1 - ((Speed - 1) / 100.0)) * 10 * 100));

                     SetupVariables();
                     parentWindow.animationTimer.Change(0, timerInterval);
                     //SetupTimer();
                 }
                 else
                 {
                     //if (Effect == EffectType.ScrollLeftCont && scrollTimes > 0)
                     //{
                     //    scrollTimes--;
                     //    currentBitmap = 0;
                     //    StopAnimation();
                     //    parentWindow.Animations[parentWindow.CurrentAnimation].Animate(animateNext);
                     //}
                     //else
                     currentBitmap = 0;
                     StopAnimation();
                     //if(false)
                     {

                         //  if (animateNext)
                         //    parentWindow.CurrentAnimationNumber++;
                         //parentWindow.CurrentAnimation.Animate(animateNext);

                         if (!animateNext)
                         {
                             int timerInterval = (PlaybillItem.ItemType == PlayWindowItemType.Animator) ?
                                     1 : (Convert.ToInt32((1 - ((Speed - 1) / 100.0)) * 10 * 100));

                             parentWindow.animationTimer.Change(1, timerInterval);
                         }
                     }
                 }
             }
             else
             {
                 try
                 {
                     switch (Effect)
                     {
                         case EffectType.Instant:
                             AnimateInstant();
                             break;
                         case EffectType.OpenLeft:
                             AnimateOpenLeft();
                             AnimateOpenLeft();
                             break;
                         case EffectType.OpenRight:
                             AnimateOpenRight();
                             AnimateOpenRight();
                             break;
                         case EffectType.OpenLeftRight:
                             AnimateOpenLeftRight();
                             AnimateOpenLeftRight();
                             break;
                         case EffectType.OpenUpDown:
                             AnimateOpenUpDown();
                             AnimateOpenUpDown();
                             break;
                         case EffectType.LaserDown:
                             AnimateLaserDown();
                             AnimateLaserDown();
                             break;
                         case EffectType.LaserUp:
                             AnimateLaserUp();
                             break;
                         case EffectType.BlindHorizontial:
                             AnimateBlindHorizontial();
                             AnimateBlindHorizontial();
                             break;
                         case EffectType.OpenHorizontal:
                             AnimateOpenHorizontal();
                             AnimateOpenHorizontal();
                             break;
                         case EffectType.OpenVertical:
                             AnimateOpenVertical();
                             AnimateOpenVertical();
                             break;
                         case EffectType.SnowFall:
                             AnimateSnow();
                             AnimateSnow();
                             break;
                         case EffectType.ExpandTop:
                             AnimateExpandTop();
                             AnimateExpandTop();
                             break;
                         case EffectType.ExpandBottom:
                             AnimateExpandBottom();
                             AnimateExpandBottom();
                             break;
                         case EffectType.ExpandVertical:
                             AnimateExpandVertical();
                             AnimateExpandVertical();
                             break;
                         case EffectType.ShiftLeft:
                             AnimateShiftLeft();
                             AnimateShiftLeft();
                             break;
                         case EffectType.ShiftRight:
                             AnimateShiftRight();
                             AnimateShiftRight();
                             break;
                         case EffectType.ShiftUp:
                             AnimateShiftUp();
                             AnimateShiftUp();
                             break;
                         case EffectType.ShiftDown:
                             AnimateShiftDown();
                             AnimateShiftDown();
                             break;
                         case EffectType.ScrollUp:
                             AnimateScrollUp();
                             AnimateScrollUp();
                             break;
                         case EffectType.ScrollDown:
                             AnimateScrollDown();
                             AnimateScrollDown();
                             break;
                         case EffectType.ScrollUpCont:
                             AnimateScrollUpCont();
                             AnimateScrollUpCont();
                             break;
                         case EffectType.ScrollDownCont:
                             AnimateScrollDownCont();
                             AnimateScrollDownCont();
                             break;
                         case EffectType.ScrollLeftCont:
                             AnimateScrollLeftCont();
                             AnimateScrollLeftCont();
                             break;
                         case EffectType.ScrollRightCont:
                             AnimateScrollRightCont();
                             AnimateScrollRightCont();
                             break;
                         case EffectType.ScrollLeft:
                             AnimateScrollLeft();
                             AnimateScrollLeft();
                             break;
                         case EffectType.ScrollRight:
                             AnimateScrollRight();
                             AnimateScrollRight();
                             break;
                         case EffectType.Blink:
                             AnimateBlink();
                             break;
                         case EffectType.BlinkInverse:
                             AnimateBlinkInverse();
                             break;
                         case EffectType.OpenClockwise:
                             AnimateOpenClock();
                             break;
                         case EffectType.OpenAntiClockwise:
                             AnimateOpenClock();
                             break;
                         case EffectType.WindmillClockwise:
                             AnimateWindmill();
                             break;
                         case EffectType.WindmillAntiClockwise:
                             AnimateWindmill();
                             break;
                         case EffectType.Open2Fan:
                             AnimateOpen2Fan();
                             break;
                         case EffectType.ZebraHorizontal:
                             AnimateZebraHorizontal();
                             AnimateZebraHorizontal();
                             break;
                         case EffectType.ZebraVertical:
                             AnimateZebraVertical();
                             AnimateZebraVertical();
                             break;
                         case EffectType.RectangleOut:
                             AnimateRectangleOut();
                             AnimateRectangleOut();
                             break;
                         case EffectType.RectangleIn:
                             AnimateRectangleIn();
                             AnimateRectangleIn();
                             break;
                         case EffectType.CornerOut:
                             AnimateCornerOut();
                             AnimateCornerOut();
                             break;
                         case EffectType.CornerIn:
                             AnimateCornerIn();
                             AnimateCornerIn();
                             break;
                         case EffectType.OpenTopLeft:
                             AnimateOpenTopLeft();
                             AnimateOpenTopLeft();
                             break;
                         case EffectType.OpenTopRight:
                             AnimateOpenTopRight();
                             AnimateOpenTopRight();
                             break;
                         case EffectType.OpenBottomLeft:
                             AnimateOpenBottomLeft();
                             AnimateOpenBottomLeft();
                             break;
                         case EffectType.OpenBottomRight:
                             AnimateOpenBottomRight();
                             AnimateOpenBottomRight();
                             break;
                         case EffectType.OpenSlash:
                             AnimateOpenSlash();
                             AnimateOpenSlash();
                             break;
                         case EffectType.OpenBackslash:
                             AnimateOpenBackslash();
                             AnimateOpenBackslash();
                             break;
                         case EffectType.RoundOut:
                             AnimateRoundOut();
                             AnimateRoundOut();
                             break;
                         case EffectType.SlideZebraHorizontal:
                             AnimateSlideZebraHorizontal();
                             AnimateSlideZebraHorizontal();
                             break;
                         case EffectType.SlideZebraVertical:
                             AnimateSlideZebraVertical();
                             AnimateSlideZebraVertical();
                             break;
                         case EffectType.SlideTopLeft:
                             AnimateSlideTopLeft();
                             AnimateSlideTopLeft();
                             break;
                         case EffectType.SlideTopRight:
                             AnimateSlideTopRight();
                             AnimateSlideTopRight();
                             break;
                         case EffectType.SlideBottomLeft:
                             AnimateSlideBottomLeft();
                             AnimateSlideBottomLeft();
                             break;
                         case EffectType.SlideBottomRight:
                             AnimateSlideBottomRight();
                             AnimateSlideBottomRight();
                             break;
                         case EffectType.SlewingOut:
                             AnimateSlewingOut();
                             AnimateSlewingOut();
                             break;
                         case EffectType.ScrapeUp:
                             AnimateScrapeUp();
                             AnimateScrapeUp();
                             break;
                         case EffectType.Bubbles:
                             AnimateBubbles();
                             AnimateBubbles();
                             break;
                         case EffectType.ShutterHorizontal:
                             AnimateShutterHorizontal();
                             AnimateShutterHorizontal();
                             break;
                         case EffectType.ShutterVertical:
                             AnimateShutterVertical();
                             AnimateShutterVertical();
                             break;
                         case EffectType.ChessboardHorizontal:
                             AnimateChessboardHorizontal();
                             AnimateChessboardHorizontal();
                             break;
                     }
                     //parentWindow.Redraw();
                 }
                 catch { /*System.Diagnostics.Debugger.Break();*/ }
             }
         }
        
        #endregion

    }

    public enum EffectType
    {
        Instant = 0,
        OpenLeft = 1,
        OpenRight = 2,
        OpenHorizontal = 3,
        OpenVertical = 4,
        ShiftLeft = 6,
        ShiftRight = 7,
        ShiftUp = 8,
        ShiftDown = 9,
        ScrollLeftCont = 14,
        Blink = 13,
        RectangleOut = 21,
        CornerOut = 23,
        RoundOut = 25,
        ZebraHorizontal = 39,
        ZebraVertical = 40,
        ExpandTop = 57,
        ExpandBottom = 58,
        ExpandVertical = 60,
        SnowFall = 63,
        SlewingOut = 49,
        //BlindHorizontal = 58,
        //BlindVertical = 59,
        SlideTopLeft = 33,
        SlideTopRight = 34,
        SlideBottomLeft = 35,
        SlideBottomRight = 36,
        OpenClockwise = 17,
        OpenAntiClockwise = 18,
        WindmillClockwise = 19,
        WindmillAntiClockwise = 20,
        ScrapeUp = 45,
        Bubbles = 56,
        BlindHorizontial = 61,
        Open2Fan = 67,
        BlinkInverse = 62,
        OpenLeftRight = 65,
        OpenUpDown = 66,
        LaserUp = 43,
        LaserDown = 44,
        ScrollRightCont = 15,
        OpenTopLeft = 27,
        OpenTopRight = 28,
        OpenBottomLeft = 29,
        OpenBottomRight = 30,
        OpenSlash = 31,
        OpenBackslash = 32,
        CornerIn = 24,
        RectangleIn = 22,
        ScrollDown = 64,
        ScrollUp = 10,
        ScrollDownCont = 54,
        ScrollUpCont = 53,
        ScrollRight = 12,
        ScrollLeft = 11,
        SlideZebraHorizontal = 69,
        SlideZebraVertical = 70,
        ChessboardHorizontal = 51,
        ShutterHorizontal = 16,
        ShutterVertical = 5
    }
}
