using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BengiLED_for_C_Power
{
    public partial class PreviewWindow : Form
    {
        #region Private fields
        private Bitmap offScreenBitmap = null;
        private int activeWindow = 0;
        private int activeProgram = 0;
        private static int space = 0;
        private static int diam = 1;
        private int screenWidth, screenHeight;
        private bool moveWindow, moveLeftBorder, moveRightBorder, moveTopBorder, moveBottomBorder, selectOtherWindow;
        private Point baseMousePosition;
        private int hoverWindow = -1;
        private bool movingItem = false, resizeItem = false, resizingBottomItemBorder = false, resizingTopItemBorder = false;
        private int itemSizeChange = 0;
        private System.Windows.Forms.Timer manipulateItemTimer = null;
        private int ticksCount = 0;
        #endregion


        public Point screenBitmapOffset;
        public static List<List<WindowOnPreview>> programsWithWindows = null;
        public Bitmap screenBitmap = null, ghostBitmap = null;       
        public static Color SpaceColor = Color.FromArgb(10,10,10);
        public static Color OffLEDColor = Color.Black;
        public static bool playEverything = false;
        //public //CloseReason 

        #region Properties
        public int ActiveProgram
        {
            get 
            {
                if (activeProgram >= programsWithWindows.Count)
                    ActiveProgram = 0;
                return activeProgram; 
            }
            set
            {
                activeProgram = value;
                if(screenBitmap != null)
                    ClearPreview();                
            }
        }
        public int ActiveWindow
        {
            get { return activeWindow; }
            set
            {
                if (ActiveProgram >= 0)
                {
                    if (activeWindow < programsWithWindows[ActiveProgram].Count)
                    {
                        if (SelectedWindow.CurrentAnimationNumber > 0 && SelectedWindow.CurrentAnimationNumber < SelectedWindow.Animations.Count)
                            SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].StopAnimation();
                    }
                    activeWindow = value;
                }
                else
                    activeWindow = -1;
            }
        }

        public WindowOnPreview SelectedWindow
        {
            get
            {
                try { return programsWithWindows[ActiveProgram][ActiveWindow]; }
                catch { return null; }
            }
        }

        public static int LedDiam
        {
            get { return diam; }
            set
            {
                diam = (value > 0) ? value : 1;
                if (diam == 1)
                    space = 0;
                else if (diam == 2)
                    space = 1;
                else
                    space = 2;
            }
        }
        public static int Space
        {
            get { return space; }
            set { space = value; }
        }
        #endregion

        #region Methods

        #region Construct
        public PreviewWindow()
        {
            InitializeComponent();

            this.ResizeEnd += new EventHandler(PreviewWindow_ResizeEnd);

            this.SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            this.UpdateStyles();

            int BorderWidth = (this.Width);
            BorderWidth -= this.ClientSize.Width;
            BorderWidth /= 2;

            int TitlebarHeight = this.Height;
            TitlebarHeight -= this.ClientSize.Height;
            TitlebarHeight -= 2 * BorderWidth;

            this.MaximumSize = new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            this.MinimumSize = new Size(previewZoomUpDown.Width + previewZoomInButton.Width + previewZoomLabel.Width + 2 * BorderWidth,
                    previewZoomUpDown.Height + 50 + TitlebarHeight + 2 * BorderWidth);
            
            programsWithWindows = new List<List<WindowOnPreview>>();

            int tmpZoomValue = Convert.ToInt32(previewZoomUpDown.Value);
            LedDiam = tmpZoomValue / 100;
            previewZoomLabel.Text = string.Format("{0}%", tmpZoomValue);
            screenBitmapOffset = new Point(0, previewZoomInButton.Height + previewZoomInButton.Top + 1);

            moveWindow = false;
            moveLeftBorder = false;
            moveRightBorder = false;
            moveTopBorder = false;
            moveBottomBorder = false;
            selectOtherWindow = false;
        }
        #endregion

        #region Preview window methods
        public void  ChangeSize(int w, int h)
        {
            Cursor currentCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;

            TreeNode selectedNode = ((MainWindow)Owner).playbillTree.SelectedNode;

            if (ActiveProgram >= programsWithWindows.Count)
                ActiveProgram = programsWithWindows.Count - 1;
            if (programsWithWindows.Count > 0 && programsWithWindows[ActiveProgram].Count > 0)
            {
                foreach (WindowOnPreview item in programsWithWindows[ActiveProgram])
                {
                    if(item.Animations.Count > 0)
                        item.Animations[item.CurrentAnimationNumber].StopAnimation();
                }

                for (int i = 0; i < programsWithWindows.Count; i++)
                {
                    ActiveProgram = i;
                    for (int j = 0; j < programsWithWindows[i].Count; j++)
                    {
                        ActiveWindow = j;
                        PlayWindow playbillWindow = ((MainWindow)Owner).CurrentPlaybill.ProgramsList[i].WindowsList[j];
                        WindowOnPreview previewWindow = programsWithWindows[i][j];

                        //previewWindow.windowRect = new Rectangle(previewWindow.X, previewWindow.Y, previewWindow.Width, previewWindow.Height);

                        previewWindow.X = (int)playbillWindow.X * (LedDiam + Space);
                        previewWindow.Y = (int)playbillWindow.Y * (LedDiam + Space);
                        previewWindow.Width = (int)playbillWindow.Width * (LedDiam + Space);
                        previewWindow.Height = (int)playbillWindow.Height * (LedDiam + Space);
                        previewWindow.windowRect = new Rectangle(previewWindow.X, previewWindow.Y, previewWindow.Width, previewWindow.Height);

                        if (((MainWindow)Owner).windowsizechanged)
                        {
                            for (int k = 0; k < previewWindow.Animations.Count; k++)
                            {
                                ((MainWindow)Owner).RegenerateItemBitmaps(((MainWindow)Owner).playbillTree.Nodes[0].Nodes[i].Nodes[j].Nodes[k]);
                                //((MainWindow)Owner).playbillTree.SelectedNode = ((MainWindow)Owner).playbillTree.Nodes[0].Nodes[i].Nodes[j].Nodes[k];

                            }
                            ((MainWindow)Owner).windowsizechanged = false;
                        }
                    }
                }

                ((MainWindow)Owner).playbillTree.SelectedNode = selectedNode;//((MainWindow)Owner).playbillTree.Nodes[0];

                this.Cursor = currentCursor;
            }

            int BorderWidth = (this.Width);
            BorderWidth -= this.ClientSize.Width;
            BorderWidth /= 2;

            int TitlebarHeight = this.Height;
            TitlebarHeight -= this.ClientSize.Height;
            TitlebarHeight -= 2 * BorderWidth;

            this.Width = w * (LedDiam + Space) + BorderWidth * 2;
            this.Height = h * (LedDiam + Space) + TitlebarHeight + BorderWidth * 2 + screenBitmapOffset.Y;

            screenWidth = w;
            screenHeight = h;

            if (screenBitmap != null)
                screenBitmap.Dispose();
            screenBitmap = new Bitmap(w * (LedDiam + Space), h * (LedDiam + Space), System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            if (offScreenBitmap != null)
                offScreenBitmap.Dispose();
            offScreenBitmap = new Bitmap(w * (LedDiam + Space), h * (LedDiam + Space), System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            //WaitCallback wc = new System.Threading.WaitCallback(SetOffScreen);
            //ThreadPool.QueueUserWorkItem(wc);

            SetOffScreen();
            
            
            if (ghostBitmap != null)
            {
                ghostBitmap.Dispose();
                ghostBitmap = null;
            }
            
            ClearPreview();

            DrawStaticWindows();
        }

        public void SetOffScreen()//(Object state)
        {
            Graphics offScreenGraphics = Graphics.FromImage(offScreenBitmap);

            offScreenGraphics.Clear(OffLEDColor);//SpaceColor);

            //for (int x = 0; x < offScreenBitmap.Width; x++)
            //{
            //    for (int y = 0; y < offScreenBitmap.Height; y++)
            //    {
            //        int newx = x * (LedDiam + Space);
            //        int newy = y * (LedDiam + Space);

            //        offScreenGraphics.DrawEllipse(new Pen(OffLEDColor), newx, newy, LedDiam, LedDiam);
            //        offScreenGraphics.FillEllipse(new SolidBrush(OffLEDColor), newx, newy, LedDiam, LedDiam);
            //    }
            //}

            offScreenGraphics.Dispose();
        }
        
        public void ClearPreview()
        {
            Graphics screenGraphics = Graphics.FromImage(screenBitmap);

            //if (programsWithWindows == null)
            {
                screenGraphics.Clear(OffLEDColor);//DrawImageUnscaled(offScreenBitmap, 0, 0);
            }

            //CreateTestAnimations();

            //foreach (WindowOnPreview item in programsWithWindows[activeProgram])
            //{
            //    item.Animations[0].ShowStatic();
            //    screenGraphics.DrawImageUnscaled(item.windowBitmap, item.windowRect);
            //}
            
            screenGraphics.Dispose();
            this.Invalidate();
        }
        
        public void ClearProgramsList()
        {
            if (programsWithWindows != null && programsWithWindows.Count > 0)
            {
                foreach (List<WindowOnPreview> windowsList in programsWithWindows)
                {
                    foreach (WindowOnPreview window in windowsList)
                        window.Clear();
                    windowsList.Clear();
                }
                programsWithWindows.Clear();
            }
        }


        public void StopProgram(int program)
        {
            if (programsWithWindows == null || program < 0 || programsWithWindows.Count <= program)
                return;

            foreach (WindowOnPreview window in programsWithWindows[program])
            {
                window.StopAnimation();
                using(Graphics wndg = Graphics.FromImage(window.windowBitmap))
                    wndg.Clear(OffLEDColor);

                using (Graphics testgr = Graphics.FromImage(window.testbmp))
                {
                    testgr.Clear(PreviewWindow.OffLEDColor);
                }
            }
        }

        public void PlayNewActiveProgram(int newProgram)
        {
            playEverything = false;

            if (programsWithWindows[ActiveProgram] != null)
            {
                StopProgram(ActiveProgram);
            }

            ActiveProgram = (newProgram < programsWithWindows.Count) ? newProgram : 0;
            PlayGivenProgram(ActiveProgram);
            //if (programsWithWindows[ActiveProgram] != null)
            //{
            //    foreach (WindowOnPreview window in programsWithWindows[ActiveProgram])
            //        window.Animate(true, 0);
            //}
        }

        public void PlayGivenProgram(int program)
        {
            if (programsWithWindows[program] != null)
            {
                foreach (WindowOnPreview window in programsWithWindows[program])
                    window.Animate(true, 0);
            }
        }

        public void PlayAllPrograms()
        { 
            if(programsWithWindows != null && programsWithWindows.Count > 0)
            {
                for (int i = 0; i < programsWithWindows.Count; i++)
                {
                    StopProgram(i);
                }
                ActiveProgram = 0;
                playEverything = true;
                PlayGivenProgram(ActiveProgram);
            }
        }

        private void CreateTestAnimations()
        {
            /*
            if (programsWithWindows != null)
                ClearProgramsList();
            else
                programsWithWindows = new List<List<WindowOnPreview>>();

            programsWithWindows.Add(new List<WindowOnPreview>());

            programsWithWindows[activeProgram].Add(new WindowOnPreview(this, 0, 0, screenWidth / 2, screenHeight/2, LedDiam + Space));
            
            programsWithWindows[activeProgram].Add(new WindowOnPreview(this, screenWidth / 2, 0, screenWidth / 2, screenHeight / 2, LedDiam + Space));
            programsWithWindows[activeProgram].Add(new WindowOnPreview(this, screenWidth / 2, screenHeight / 2, screenWidth / 2, screenHeight / 2, LedDiam + Space));
            
            programsWithWindows[activeProgram].Add(new WindowOnPreview(this, 0, screenHeight / 2, screenWidth / 4, screenHeight / 2, LedDiam + Space));
            programsWithWindows[activeProgram].Add(new WindowOnPreview(this, screenWidth / 4, screenHeight / 2, screenWidth / 4, screenHeight / 2, LedDiam + Space));
            

            List<Bitmap> tmpbmpl = new List<Bitmap>();
            
            Image gifImg = Image.FromFile("12.gif");
            FrameDimension dimension = new FrameDimension(gifImg.FrameDimensionsList[0]);
            
            for (int i = 0; i < gifImg.GetFrameCount(dimension); i++)
            {
                gifImg.SelectActiveFrame(dimension, i);
                tmpbmpl.Add(new Bitmap(gifImg));
            }

            programsWithWindows[activeProgram][0].Animations.Add(new PreviewAnimation(programsWithWindows[activeProgram][0], tmpbmpl, EffectType.OpenLeft, 100, 0));

            foreach (Bitmap b in tmpbmpl)
                b.Dispose();

            tmpbmpl.Clear();
            tmpbmpl.Add(new Bitmap("Font - test.bmp"));
            tmpbmpl.Add(new Bitmap("Bez tytułu.bmp"));

            programsWithWindows[activeProgram][0].Animations.Add(new PreviewAnimation(programsWithWindows[activeProgram][0], tmpbmpl, EffectType.SnowFall, 90, 0));

            foreach (Bitmap b in tmpbmpl)
                b.Dispose();

            tmpbmpl.Clear();
            tmpbmpl.Add(new Bitmap("Font - test.bmp"));
            tmpbmpl.Add(new Bitmap("Test - bialy.bmp"));
            programsWithWindows[activeProgram][0].Animations.Add(new PreviewAnimation(programsWithWindows[activeProgram][0], tmpbmpl, EffectType.ExpandBottom, 100, 3));

            foreach (Bitmap b in tmpbmpl)
                b.Dispose();

            tmpbmpl.Clear();
            tmpbmpl.Add(new Bitmap("Test-mono.bmp"));
            tmpbmpl.Add(new Bitmap("Font - test.bmp"));

            programsWithWindows[activeProgram][1].Animations.Add(new PreviewAnimation(programsWithWindows[activeProgram][1], tmpbmpl, EffectType.ShiftLeft, 100, 3));

            foreach (Bitmap b in tmpbmpl)
                b.Dispose();

            tmpbmpl.Clear();
            tmpbmpl.Add(new Bitmap("Test - bialy.bmp"));
            tmpbmpl.Add(new Bitmap("test - kolor.jpg"));

            programsWithWindows[activeProgram][1].Animations.Add(new PreviewAnimation(programsWithWindows[activeProgram][1], tmpbmpl, EffectType.SnowFall, 100, 3));

            foreach (Bitmap b in tmpbmpl)
                b.Dispose();

            tmpbmpl.Clear();
            tmpbmpl.Add(new Bitmap("Font - test.bmp"));
            tmpbmpl.Add(new Bitmap("Test - bialy.bmp"));

            programsWithWindows[activeProgram][2].Animations.Add(new PreviewAnimation(programsWithWindows[activeProgram][2], tmpbmpl, EffectType.ExpandBottom, 70, 0));

            foreach (Bitmap b in tmpbmpl)
                b.Dispose();

            tmpbmpl.Clear();
            tmpbmpl.Add(new Bitmap("Font - test.bmp"));
            tmpbmpl.Add(new Bitmap("Test - bialy.bmp"));

            programsWithWindows[activeProgram][3].Animations.Add(new PreviewAnimation(programsWithWindows[activeProgram][3], tmpbmpl, EffectType.ExpandBottom, 100, 5));

            foreach (Bitmap b in tmpbmpl)
                b.Dispose();

            tmpbmpl.Clear();
            tmpbmpl.Add(new Bitmap("Font - test.bmp"));
            tmpbmpl.Add(new Bitmap("Test - bialy.bmp"));

            programsWithWindows[activeProgram][4].Animations.Add(new PreviewAnimation(programsWithWindows[activeProgram][4], tmpbmpl, EffectType.OpenLeft, 90, 2));

            foreach (Bitmap b in tmpbmpl)
                b.Dispose();

            activeWindow = 0;
             * */
        }

        public Color ApplyColorType(Color c)
        {
            if (Owner == null)
                return OffLEDColor;

            Color retval;

            if (((MainWindow)Owner).CurrentPlaybill.Color == 0x77)
            {
                retval = c;
            }
            else
            {
                Color availableColor;

                if (((MainWindow)Owner).CurrentPlaybill.Color == 1)
                    availableColor = Color.Red;
                else if (((MainWindow)Owner).CurrentPlaybill.Color == 2)
                    availableColor = Color.Lime;
                else if (((MainWindow)Owner).CurrentPlaybill.Color == 3)
                    availableColor = Color.Yellow;
                else if (((MainWindow)Owner).CurrentPlaybill.Color == 4)
                    availableColor = Color.Blue;
                else if (((MainWindow)Owner).CurrentPlaybill.Color == 5)
                    availableColor = Color.Magenta;
                else if (((MainWindow)Owner).CurrentPlaybill.Color == 6)
                    availableColor = Color.Aqua;
                else //if (((MainWindow)Owner).CurrentPlaybill.Color == 7)
                    availableColor = Color.White;

                retval = Color.FromArgb((c.R < 128) ? 0 : 255 & availableColor.R,
                                        (c.G < 128) ? 0 : 255 & availableColor.G,
                                        (c.B < 128) ? 0 : 255 & availableColor.B);

                //retval = Color.FromArgb((c.R < 20) ? 0 : 255 & availableColor.R,
                //                        (c.G < 20) ? 0 : 255 & availableColor.G,
                //                        (c.B < 20) ? 0 : 255 & availableColor.B);
            }

            return retval;
        }

        public void DrawWindowBorder()
        {
            if (ghostBitmap == null)
                ghostBitmap = new Bitmap(screenBitmap.Width, screenBitmap.Height);

            Graphics ghostGraphic = Graphics.FromImage(ghostBitmap);

            foreach (List<WindowOnPreview> windowsList in programsWithWindows)
            {
                //foreach (WindowOnPreview window in windowsList)
                {
                    //ghostGraphic.DrawRectangle(new Pen(Color.Gold), window.windowRect);
                }

                //ghostGraphic.DrawRectangle(new Pen(Color.Gold), windowsList[activeWindow].windowRect);
            }

            ghostGraphic.Dispose();
        }
        #endregion

        #region Connection with main window
        public Bitmap LoadBitmapForItem(string fileName)
        {
            Bitmap retval = null;

            if (!string.IsNullOrEmpty(fileName))
            {
                retval = new Bitmap(fileName);
            }

            return retval;
        }
        #endregion
        
        #endregion

        #region Event handlers
        private void PreviewWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            StopProgram(ActiveProgram);

            if(e.CloseReason == CloseReason.UserClosing)
                e.Cancel = true;
        }


        private void DrawLED(Graphics targetGraphics, int x, int y, Point offset, Rectangle rect, Color c)
        {
            int newx = x * (PreviewWindow.LedDiam + PreviewWindow.Space) + offset.X + rect.Left;
            int newy = y * (PreviewWindow.LedDiam + PreviewWindow.Space) + offset.Y + rect.Top;

            Color tmpc = ApplyColorType(c);

            //if (parentWindow.windowBitmap.GetPixel(newx + PreviewWindow.LedDiam / 2, newy + PreviewWindow.LedDiam / 2) != tmpc)
            {
                targetGraphics.DrawEllipse(new Pen(tmpc), newx, newy, PreviewWindow.LedDiam, PreviewWindow.LedDiam);
                targetGraphics.FillEllipse(new SolidBrush(tmpc), newx, newy, PreviewWindow.LedDiam, PreviewWindow.LedDiam);
            }

        }

        private void PreviewWindow_Paint(object sender, PaintEventArgs e)
        {
            DateTime start = DateTime.Now;

            Graphics oGraphics;

            oGraphics = e.Graphics;
            
            //oGraphics.DrawImage(screenBitmap, screenBitmapOffset.X, screenBitmapOffset.Y,
              //                  screenBitmap.Width, screenBitmap.Height);

            if (ActiveProgram > -1 && programsWithWindows != null && programsWithWindows.Count > ActiveProgram)
            {
                foreach (var window in programsWithWindows[ActiveProgram])
                {
                    //if (!MainWindow.anotherBitmaps)//MainWindow.oldAnimation)
                    //{
                    //    if (window.testbmp != null)
                    //    {
                    //        //using (Bitmap tmpbmp = (Bitmap)window.windowRedrawBitmap.Clone())
                    //        {
                    //            oGraphics.CompositingQuality = CompositingQuality.HighSpeed;
                    //            oGraphics.InterpolationMode = InterpolationMode.Low;
                    //            oGraphics.SmoothingMode = SmoothingMode.HighSpeed;

                    //            oGraphics.DrawImage(window.testbmp, screenBitmapOffset.X + window.windowRect.Left, screenBitmapOffset.Y + window.windowRect.Top,
                    //                                window.windowBitmap.Width, window.windowBitmap.Height);
                    //        }
                    //    }
                    //}

                    if (!MainWindow.anotherBitmaps)//MainWindow.oldAnimation)
                    {
                        if (window.windowRedrawBitmap != null)
                        {
                            //using (Bitmap tmpbmp = (Bitmap)window.windowRedrawBitmap.Clone())
                            {


                                oGraphics.DrawImageUnscaled(window.windowRedrawBitmap, screenBitmapOffset.X + window.windowRect.Left, screenBitmapOffset.Y + window.windowRect.Top,
                                                    window.windowRedrawBitmap.Width, window.windowRedrawBitmap.Height);


                                //oGraphics.DrawImageUnscaled(window.windowBitmap, screenBitmapOffset.X + window.windowRect.Left, screenBitmapOffset.Y + window.windowRect.Top,
                                //                    window.windowBitmap.Width, window.windowBitmap.Height);
                            }
                        }
                        //            window.repaintNow = false;
                        //        }
                    }
                    else
                    {
                        if (window.CurrentAnimation != null && window.CurrentAnimation.changedPixels != null)
                        {
                            lock (oGraphics)
                            {
                                foreach (var pix in window.CurrentAnimation.changedPixels)
                                {
                                    //DrawLED(oGraphics, pix.PixelPoint.X, pix.PixelPoint.Y, pix.PixelColor);

                                    DrawLED(oGraphics,
                                        pix.PixelPoint.X,
                                        pix.PixelPoint.Y,
                                        screenBitmapOffset,
                                        window.windowRect,
                                        pix.PixelColor);

                                    //DrawLED(oGraphics,
                                    //    pix.PixelPoint.X,
                                    //    pix.PixelPoint.Y,
                                    //    screenBitmapOffset,
                                    //    window.windowRect,
                                    //    window.CurrentAnimation.frameBitmapPixels[pix.PixelPoint.X][pix.PixelPoint.Y]);

                                }
                            }
                            window.CurrentAnimation.changedPixels.Clear();
                        }
                    }
                }
            }
            //}


            if (ghostBitmap != null)
            {
                oGraphics.DrawImageUnscaled(ghostBitmap, screenBitmapOffset.X, screenBitmapOffset.Y,
                                    screenBitmap.Width, screenBitmap.Height);
            }


            DateTime end = DateTime.Now;
            //MessageBox.Show((((TimeSpan)(end - start)).TotalMilliseconds).ToString(), "AnimateInstant");
            //Console.WriteLine("Paint - {0}ms", (((TimeSpan)(end - start)).TotalMilliseconds).ToString());
             
        }

        private void PreviewWindow_Load(object sender, EventArgs e)
        {
        }
        
        private void previewZoomUpDown_ValueChanged(object sender, EventArgs e)
        {
            StopProgram(ActiveProgram);
            LedDiam = (int)((NumericUpDown)sender).Value / 100;

            //if (screenBitmap != null && ((screenWidth * (LedDiam + Space)) >= Screen.PrimaryScreen.Bounds.Width
            //    || (screenHeight * (LedDiam + Space) + previewZoomInButton.Height) >= Screen.PrimaryScreen.Bounds.Height))
            //{
            //    MessageBox.Show("Your screen resolution is to small to zoom so much");


            //    LedDiam = ((int)((NumericUpDown)sender).Value - 100) / 100;
            //    ((NumericUpDown)sender).Value = LedDiam * 100;
            //}
            //else
            {
                if (screenBitmap != null)
                {
                    previewZoomLabel.Text = string.Format("{0}%", ((NumericUpDown)sender).Value);
                    ChangeSize(screenWidth, screenHeight);
                }
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            MessageBox.Show(SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].PlaybillItem.Text);
            //foreach (WindowOnPreview item in programsWithWindows[activeProgram])
            //{
                
            //    item.Animations[item.CurrentAnimation].StopAnimation();
            //    item.Animations[item.CurrentAnimation].ClearLEDs(null);
            //    item.Animations[item.CurrentAnimation].Animate(true);

            //    //item.animations[0].ShowStatic();
            //    //screenGraphics.DrawImageUnscaled(item.windowBitmap, item.X, item.Y);
            //}
        }

        private void previewZoomInButton_Click(object sender, EventArgs e)
        {
            if (previewZoomUpDown.Value + 100 <= previewZoomUpDown.Maximum)
                previewZoomUpDown.Value += 100;
            else
            { 
                
            }
        }

        private void previewZoomOutButton_Click(object sender, EventArgs e)
        {
            if (previewZoomUpDown.Value - 100 >= previewZoomUpDown.Minimum)
                previewZoomUpDown.Value -= 100;
        }
        #endregion

        private void MoveLeftBorder(Point mouseLocation)
        {
            Rectangle ghostRectangle = programsWithWindows[activeProgram][activeWindow].windowRect;

            if (ghostBitmap != null)
                ghostBitmap.Dispose();
            ghostBitmap = new Bitmap(screenBitmap.Width, screenBitmap.Height);

            using (Graphics ghostGraphics = Graphics.FromImage(ghostBitmap))
            {
                int tmpChangeVal = 0;
                Pen ghostPen = new Pen(Color.Azure);
                ghostPen.DashStyle = DashStyle.DashDot;

                if ((Math.Abs(mouseLocation.X - baseMousePosition.X) / (LedDiam + Space)) % 8 == 0
                    && (mouseLocation.X - baseMousePosition.X) / (LedDiam + Space) != 0)
                {
                    tmpChangeVal = Math.Sign(mouseLocation.X - baseMousePosition.X) * 8 * (LedDiam + Space);

                    ghostRectangle.X += tmpChangeVal;
                    ghostRectangle.Width -= tmpChangeVal;


                    if (ghostRectangle != programsWithWindows[activeProgram][activeWindow].windowRect)
                        baseMousePosition.X = mouseLocation.X;
                }


                if (ghostRectangle.X >= 0 && ghostRectangle.Right <= screenBitmap.Width
                    && ghostRectangle.Y >= 0 && ghostRectangle.Bottom <= screenBitmap.Height)
                {

                    programsWithWindows[activeProgram][activeWindow].windowRect = ghostRectangle;
                }

                ghostGraphics.DrawRectangle(ghostPen, programsWithWindows[activeProgram][activeWindow].windowRect);
            }
        }

        private void MoveRightBorder(Point mouseLocation)
        {
            Rectangle ghostRectangle = programsWithWindows[activeProgram][activeWindow].windowRect;

            if (ghostBitmap != null)
                ghostBitmap.Dispose();
            ghostBitmap = new Bitmap(screenBitmap.Width, screenBitmap.Height);

            using (Graphics ghostGraphics = Graphics.FromImage(ghostBitmap))
            {
                int tmpChangeVal = 0;
                Pen ghostPen = new Pen(Color.Azure);
                ghostPen.DashStyle = DashStyle.DashDot;

                if ((Math.Abs(mouseLocation.X - baseMousePosition.X) / (LedDiam + Space)) % 8 == 0
                   && (mouseLocation.X - baseMousePosition.X) / (LedDiam + Space) != 0)
                {
                    tmpChangeVal = Math.Sign(mouseLocation.X - baseMousePosition.X) * 8 * (LedDiam + Space);
                    ghostRectangle.Width += tmpChangeVal;

                    if (ghostRectangle != programsWithWindows[activeProgram][activeWindow].windowRect)
                        baseMousePosition.X = mouseLocation.X;
                }


                if (ghostRectangle.X >= 0 && ghostRectangle.Right <= screenBitmap.Width
                    && ghostRectangle.Y >= 0 && ghostRectangle.Bottom <= screenBitmap.Height)
                {

                    programsWithWindows[activeProgram][activeWindow].windowRect = ghostRectangle;
                }

                ghostGraphics.DrawRectangle(ghostPen, programsWithWindows[activeProgram][activeWindow].windowRect);
            }
        }
         
        private void MoveTopBorder(Point mouseLocation)
        {
            Rectangle ghostRectangle = programsWithWindows[activeProgram][activeWindow].windowRect;

            if (ghostBitmap != null)
                ghostBitmap.Dispose();
            ghostBitmap = new Bitmap(screenBitmap.Width, screenBitmap.Height);

            using (Graphics ghostGraphics = Graphics.FromImage(ghostBitmap))
            {
                int tmpChangeVal = 0;
                Pen ghostPen = new Pen(Color.Azure);
                ghostPen.DashStyle = DashStyle.DashDot;

                if ((Math.Abs(mouseLocation.Y - baseMousePosition.Y) / (LedDiam + Space)) != 0)
                {
                    tmpChangeVal = Math.Sign(mouseLocation.Y - baseMousePosition.Y) * 1 * (LedDiam + Space);
                    ghostRectangle.Y += tmpChangeVal;
                    ghostRectangle.Height -= tmpChangeVal;


                    if (ghostRectangle != programsWithWindows[activeProgram][activeWindow].windowRect)
                        baseMousePosition.Y = mouseLocation.Y;
                }


                if (ghostRectangle.X >= 0 && ghostRectangle.Right <= screenBitmap.Width
                    && ghostRectangle.Y >= 0 && ghostRectangle.Bottom <= screenBitmap.Height)
                {

                    programsWithWindows[activeProgram][activeWindow].windowRect = ghostRectangle;
                }

                ghostGraphics.DrawRectangle(ghostPen, programsWithWindows[activeProgram][activeWindow].windowRect);
            }
        }

        private void MoveBottomBorder(Point mouseLocation)
        {
            Rectangle ghostRectangle = programsWithWindows[activeProgram][activeWindow].windowRect;

            if (ghostBitmap != null)
                ghostBitmap.Dispose();
            ghostBitmap = new Bitmap(screenBitmap.Width, screenBitmap.Height);

            using (Graphics ghostGraphics = Graphics.FromImage(ghostBitmap))
            {
                int tmpChangeVal = 0;
                Pen ghostPen = new Pen(Color.Azure);
                ghostPen.DashStyle = DashStyle.DashDot;

                if ((Math.Abs(mouseLocation.Y - baseMousePosition.Y) / (LedDiam + Space)) != 0)
                {
                    tmpChangeVal = Math.Sign(mouseLocation.Y - baseMousePosition.Y) * 1 * (LedDiam + Space);
                    ghostRectangle.Height += tmpChangeVal;

                    if (ghostRectangle != programsWithWindows[activeProgram][activeWindow].windowRect)
                        baseMousePosition.Y = mouseLocation.Y;
                }


                if (ghostRectangle.X >= 0 && ghostRectangle.Right <= screenBitmap.Width
                    && ghostRectangle.Y >= 0 && ghostRectangle.Bottom <= screenBitmap.Height)
                {

                    programsWithWindows[activeProgram][activeWindow].windowRect = ghostRectangle;
                }

                ghostGraphics.DrawRectangle(ghostPen, programsWithWindows[activeProgram][activeWindow].windowRect);
            }
        }

        private void MoveActiveWindow(Point mouseLocation)
        {
            Rectangle ghostRectangle = programsWithWindows[activeProgram][activeWindow].windowRect;

            if (ghostBitmap != null)
                ghostBitmap.Dispose();
            ghostBitmap = new Bitmap(screenBitmap.Width, screenBitmap.Height);
            //ghostRectangle.Width, ghostRectangle.Height);


            using (Graphics ghostGraphics = Graphics.FromImage(ghostBitmap))
            {
                int tmpChangeVal = 0;
                Pen ghostPen = new Pen(Color.Azure);
                ghostPen.DashStyle = DashStyle.DashDot;

                if ((Math.Abs(mouseLocation.X - baseMousePosition.X) / (LedDiam + Space)) % 8 == 0
                    && (mouseLocation.X - baseMousePosition.X) / (LedDiam + Space) != 0)
                {
                    tmpChangeVal = Math.Sign(mouseLocation.X - baseMousePosition.X) * 8 * (LedDiam + Space);
                    //(mouseLocation.X - baseMousePosition.X) / 8 * (LedDiam + Space);
                    //- (8 - (mouseLocation.X - baseMousePosition.X)) * (LedDiam + Space);
                    ghostRectangle.X += tmpChangeVal;

                    if (ghostRectangle != programsWithWindows[activeProgram][activeWindow].windowRect)
                        baseMousePosition.X = mouseLocation.X;
                }

                if ((Math.Abs(mouseLocation.Y - baseMousePosition.Y) / (LedDiam + Space)) != 0)
                {
                    tmpChangeVal = Math.Sign(mouseLocation.Y - baseMousePosition.Y) * 1 * (LedDiam + Space);
                    //(mouseLocation.Y - baseMousePosition.Y) / 8 * (LedDiam + Space);// +1 * (LedDiam + Space);
                    ghostRectangle.Y += tmpChangeVal;


                    if (ghostRectangle != programsWithWindows[activeProgram][activeWindow].windowRect)
                        baseMousePosition.Y = mouseLocation.Y;
                }


                if (ghostRectangle.X >= 0 && ghostRectangle.Right <= screenBitmap.Width
                    && ghostRectangle.Y >= 0 && ghostRectangle.Bottom <= screenBitmap.Height)
                {

                    programsWithWindows[activeProgram][activeWindow].windowRect = ghostRectangle;
                }

                ghostGraphics.DrawRectangle(ghostPen, programsWithWindows[activeProgram][activeWindow].windowRect);
            }
        }

        private void MoveActiveItem(Point mouseLocation)
        {
            Rectangle ghostRectangle = programsWithWindows[activeProgram][activeWindow].windowRect;

            //ghostRectangle.Offset(
            //    SelectedWindow.Animations[SelectedWindow.CurrentAnimation].PlaybillItem.Offset.X * (LedDiam + Space),
            //    SelectedWindow.Animations[SelectedWindow.CurrentAnimation].PlaybillItem.Offset.Y * (LedDiam + Space));
            //Point location = ghostRectangle.Location;
            Point location = ghostRectangle.Location;
            Point itemOffset = SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].PlaybillItem.Offset;
            itemOffset = new Point(itemOffset.X * (LedDiam + Space), itemOffset.Y * (LedDiam + Space));

            location.Offset(itemOffset);
            ghostRectangle.Location = location;


            ghostRectangle.Location = location;
            ghostRectangle.Width -= itemOffset.X;
            ghostRectangle.Height = SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].TextBitmapHeight * (LedDiam + Space);
            if (ghostRectangle.Bottom > SelectedWindow.windowRect.Bottom)
                ghostRectangle.Height -= SelectedWindow.windowRect.Bottom - ghostRectangle.Bottom;
            //ghostRectangle.Height -= itemOffset.Y;
            //ghostRectangle.Height -= itemOffset.Y - itemSizeChange * (LedDiam + Space);

            if (ghostBitmap != null)
                ghostBitmap.Dispose();
            ghostBitmap = new Bitmap(screenBitmap.Width, screenBitmap.Height);
            //ghostRectangle.Width, ghostRectangle.Height);


            using (Graphics ghostGraphics = Graphics.FromImage(ghostBitmap))
            {
                int tmpChangeVal = 0;
                Pen ghostPen = new Pen(Color.Azure);
                ghostPen.DashStyle = DashStyle.DashDot;
                ghostPen.Width = (LedDiam + Space);

                if ((Math.Abs(mouseLocation.X - baseMousePosition.X) / (LedDiam + Space)) != 0
                    && (mouseLocation.X - baseMousePosition.X) / (LedDiam + Space) != 0)
                {
                    tmpChangeVal = Math.Sign(mouseLocation.X - baseMousePosition.X) * 1 * (LedDiam + Space);
                    ghostRectangle.X += tmpChangeVal;


                    //if (ghostRectangle.X >= 0 && ghostRectangle.Y >= 0)
                    {
                        SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].PlaybillItem.Offset =
                            new Point(SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].PlaybillItem.Offset.X + tmpChangeVal / (LedDiam + Space),
                                SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].PlaybillItem.Offset.Y);


                        //SelectedWindow.Animations[SelectedWindow.CurrentAnimation].PlaybillItem.Offset.
                        //    Offset(tmpChangeVal / (LedDiam + Space), 0);




                        //((MainWindow)Owner).CurrentPlaybill.ProgramsList[ActiveProgram].WindowsList[ActiveWindow].ItemsList[SelectedWindow.CurrentAnimation].Offset.
                        //    Offset(tmpChangeVal / (LedDiam + Space), 0);
                    }
                    //SelectedWindow.Animations[SelectedWindow.CurrentAnimation].PlaybillItem.Text = "test :(";
                    //if (ghostRectangle != programsWithWindows[activeProgram][activeWindow].windowRect)
                    baseMousePosition.X = mouseLocation.X;
                }

                if ((Math.Abs(mouseLocation.Y - baseMousePosition.Y) / (LedDiam + Space)) != 0)
                {
                    tmpChangeVal = Math.Sign(mouseLocation.Y - baseMousePosition.Y) * 1 * (LedDiam + Space);
                    //(mouseLocation.Y - baseMousePosition.Y) / 8 * (LedDiam + Space);// +1 * (LedDiam + Space);
                    ghostRectangle.Y += tmpChangeVal;

                    //if (ghostRectangle.X >= 0 && ghostRectangle.Y >= 0)
                    {
                        SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].PlaybillItem.Offset =
                            new Point(SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].PlaybillItem.Offset.X,
                                SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].PlaybillItem.Offset.Y + tmpChangeVal / (LedDiam + Space));

                        //SelectedWindow.Animations[SelectedWindow.CurrentAnimation].PlaybillItem.Offset.
                        //((MainWindow)Owner).CurrentPlaybill.ProgramsList[ActiveProgram].WindowsList[ActiveWindow].ItemsList[SelectedWindow.CurrentAnimation].Offset.
                        //    Offset(0, tmpChangeVal / (LedDiam + Space));
                    }

                    //if (ghostRectangle != programsWithWindows[activeProgram][activeWindow].windowRect)
                    baseMousePosition.Y = mouseLocation.Y;
                }


                if (ghostRectangle.X >= 0
                    && ghostRectangle.Y >= 0)
                {

                    //programsWithWindows[activeProgram][activeWindow].windowRect = ghostRectangle;
                }

                ghostGraphics.DrawRectangle(ghostPen, ghostRectangle);
                ghostGraphics.FillRectangle(new SolidBrush(Color.FromArgb(50, Color.Azure)), ghostRectangle);
            }
        }

        public void ResizeActiveItem(Point mouseLocation)
        {
            Rectangle ghostRectangle = programsWithWindows[activeProgram][activeWindow].windowRect;
            Point location = ghostRectangle.Location;
            Point itemOffset = SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].PlaybillItem.Offset;
            itemOffset = new Point(itemOffset.X * (LedDiam + Space), itemOffset.Y * (LedDiam + Space));

            location.Offset(itemOffset);
            ghostRectangle.Location = location;
            ghostRectangle.Width -= itemOffset.X;
            ghostRectangle.Height = SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].TextBitmapHeight * (LedDiam + Space);
            if (ghostRectangle.Bottom > SelectedWindow.windowRect.Bottom)
                ghostRectangle.Height -= SelectedWindow.windowRect.Bottom - ghostRectangle.Bottom;
            //itemRect.Height -= itemOffset.Y;
            ghostRectangle.Height += itemSizeChange * (LedDiam + Space);
            //ghostRectangle.Height -= itemOffset.Y - itemSizeChange * (LedDiam + Space);
            //ghostRectangle.Height = SelectedWindow.Animations[SelectedWindow.CurrentAnimation].TextBitmapHeight * (LedDiam + Space) - itemSizeChange * (LedDiam + Space); 

            if (ghostBitmap != null)
                ghostBitmap.Dispose();
            ghostBitmap = new Bitmap(screenBitmap.Width, screenBitmap.Height);
            //ghostRectangle.Width, ghostRectangle.Height);

            using (Graphics ghostGraphics = Graphics.FromImage(ghostBitmap))
            {
                int tmpChangeVal = 0;
                Pen ghostPen = new Pen(Color.Azure);
                ghostPen.DashStyle = DashStyle.DashDot;
                ghostPen.Width = (LedDiam + Space);

                if ((Math.Abs(mouseLocation.X - baseMousePosition.X) / (LedDiam + Space)) != 0
                    && (mouseLocation.X - baseMousePosition.X) / (LedDiam + Space) != 0)
                {
                    //tmpChangeVal = Math.Sign(mouseLocation.X - baseMousePosition.X) * 1 * (LedDiam + Space);
                    //ghostRectangle.X += tmpChangeVal;


                    //if (ghostRectangle.X >= 0 && ghostRectangle.Y >= 0)
                    {
                        //SelectedWindow.Animations[SelectedWindow.CurrentAnimation].PlaybillItem.Offset =
                        //    new Point(SelectedWindow.Animations[SelectedWindow.CurrentAnimation].PlaybillItem.Offset.X + tmpChangeVal / (LedDiam + Space),
                        //        SelectedWindow.Animations[SelectedWindow.CurrentAnimation].PlaybillItem.Offset.Y);


                        //SelectedWindow.Animations[SelectedWindow.CurrentAnimation].PlaybillItem.Offset.
                        //    Offset(tmpChangeVal / (LedDiam + Space), 0);




                        //((MainWindow)Owner).CurrentPlaybill.ProgramsList[ActiveProgram].WindowsList[ActiveWindow].ItemsList[SelectedWindow.CurrentAnimation].Offset.
                        //    Offset(tmpChangeVal / (LedDiam + Space), 0);
                    }
                    //SelectedWindow.Animations[SelectedWindow.CurrentAnimation].PlaybillItem.Text = "test :(";
                    //if (ghostRectangle != programsWithWindows[activeProgram][activeWindow].windowRect)
                    baseMousePosition.X = mouseLocation.X;
                }

                if ((Math.Abs(mouseLocation.Y - baseMousePosition.Y) / (LedDiam + Space)) != 0
                    && (Math.Abs(mouseLocation.Y - baseMousePosition.Y) / (LedDiam + Space)) % 2 == 0)
                {
                    tmpChangeVal = Math.Sign(mouseLocation.Y - baseMousePosition.Y) * 2 * (LedDiam + Space);
                    //(mouseLocation.Y - baseMousePosition.Y) / 8 * (LedDiam + Space);// +1 * (LedDiam + Space);

                    if (((MainWindow)Owner).bitmaptextFontSizeComboBox.SelectedIndex + (itemSizeChange + tmpChangeVal / (LedDiam + Space)) / 2 > -1
                            && ((MainWindow)Owner).bitmaptextFontSizeComboBox.SelectedIndex + (itemSizeChange + tmpChangeVal / (LedDiam + Space)) / 2 < ((MainWindow)Owner).bitmaptextFontSizeComboBox.Items.Count)
                    {
                        if (resizingBottomItemBorder)
                        {
                            ghostRectangle.Height += tmpChangeVal;
                            itemSizeChange += tmpChangeVal / (LedDiam + Space);
                        }
                        else if (resizingTopItemBorder)
                        {
                            ghostRectangle.Height -= tmpChangeVal;
                            itemSizeChange -= tmpChangeVal / (LedDiam + Space);
                            ghostRectangle.Y += tmpChangeVal;

                            Point tmppoint = SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].PlaybillItem.Offset;
                            tmppoint.Offset(0, tmpChangeVal / (LedDiam + Space));
                            SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].PlaybillItem.Offset = tmppoint;
                        }
                    }
                    //if (ghostRectangle.X >= 0 && ghostRectangle.Y >= 0)
                    {

                        //SelectedWindow.Animations[SelectedWindow.CurrentAnimation].PlaybillItem

                        //SelectedWindow.Animations[SelectedWindow.CurrentAnimation].PlaybillItem.Offset.
                        //((MainWindow)Owner).CurrentPlaybill.ProgramsList[ActiveProgram].WindowsList[ActiveWindow].ItemsList[SelectedWindow.CurrentAnimation].Offset.
                        //    Offset(0, tmpChangeVal / (LedDiam + Space));
                    }

                    //if (ghostRectangle != programsWithWindows[activeProgram][activeWindow].windowRect)
                    baseMousePosition.Y = mouseLocation.Y;
                }


                if (ghostRectangle.X >= 0
                    && ghostRectangle.Y >= 0)
                {

                    //programsWithWindows[activeProgram][activeWindow].windowRect = ghostRectangle;
                }

                ghostGraphics.DrawRectangle(ghostPen, ghostRectangle);
                ghostGraphics.FillRectangle(new SolidBrush(Color.FromArgb(50, Color.Azure)), ghostRectangle);
            }
        }

        
        private void PreviewWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (((MainWindow)Owner).playbillTree.SelectedNode == null || (((MainWindow)Owner).playbillTree.SelectedNode.Tag.ToString() != "window"
                && ((MainWindow)Owner).playbillTree.SelectedNode.Tag.ToString() != "item")
                || (((MainWindow)Owner).playbillTree.SelectedNode.Tag.ToString() == "item"
                && SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].PlaybillItem.ItemType != PlayWindowItemType.BitmapText))
            {
                this.Cursor = Cursors.Default;
                return;
            }

            if (programsWithWindows == null || programsWithWindows.Count == 0
                || programsWithWindows[ActiveProgram] == null || programsWithWindows[ActiveProgram].Count == 0)
                return;
            
            Point mouseLocation = e.Location;
            mouseLocation.Offset(-screenBitmapOffset.X, -screenBitmapOffset.Y);

            if (!manipulateItemCheckBox.Checked && ((MainWindow)Owner).playbillTree.SelectedNode.Tag.ToString() == "item")
            {
                //if (manipulateItemTimer != null)
                //{
                //    manipulateItemTimer.Stop();
                //    manipulateItemTimer.Dispose();
                //    manipulateItemTimer = null;
                //    ticksCount = 0;
                //}

                //manipulateItemTimer = new System.Windows.Forms.Timer();
                //manipulateItemTimer.Interval = 100;
                //manipulateItemTimer.Tick += new EventHandler(manipulateItemTimer_Tick);
                //manipulateItemTimer.Start();
            }

            if (manipulateItemCheckBox.Checked && ((MainWindow)Owner).playbillTree.SelectedNode.Tag.ToString() == "item")
            {
                

                //if (moveWindow && mouseLocation.X >= 0 && mouseLocation.X <= screenBitmap.Width && mouseLocation.Y >= 0 && mouseLocation.Y <= screenBitmap.Height)
                Rectangle itemRect = programsWithWindows[activeProgram][activeWindow].windowRect;
                Point location = itemRect.Location;
                Point itemOffset = SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].PlaybillItem.Offset;
                itemOffset = new Point(itemOffset.X * (LedDiam + Space), itemOffset.Y * (LedDiam + Space));

                location.Offset(itemOffset);
                itemRect.Location = location;
                itemRect.Width -= itemOffset.X;
                //itemRect.Height -= itemOffset.Y;
                itemRect.Height = SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].TextBitmapHeight * (LedDiam + Space);

                if (itemRect.Bottom > SelectedWindow.windowRect.Bottom)
                    itemRect.Height -= SelectedWindow.windowRect.Bottom - itemRect.Bottom;
                //itemRect.Height -= itemOffset.Y;

                if(resizeItem)
                    itemRect.Height -= itemSizeChange * (LedDiam + Space);
                //itemRect.Height -= itemOffset.Y - itemSizeChange * (LedDiam + Space);

                if (resizeItem || (((mouseLocation.Y >= itemRect.Bottom - LedDiam - Space) || (mouseLocation.Y <= itemRect.Top + LedDiam + Space))
                    && itemRect.Contains(mouseLocation)))
                {
                    this.Cursor = Cursors.SizeNWSE;
                    if (resizeItem)
                        ResizeActiveItem(mouseLocation);
                }
                else if (movingItem || itemRect.Contains(mouseLocation))
                {
                    this.Cursor = Cursors.SizeAll;
                    if (movingItem)
                        MoveActiveItem(mouseLocation);
                }
                else
                    this.Cursor = Cursors.Default;
            }
            else if (((MainWindow)Owner).playbillTree.SelectedNode.Tag.ToString() == "window")
            {

                if ((mouseLocation.X <= programsWithWindows[activeProgram][activeWindow].windowRect.Left + LedDiam + Space
                    && mouseLocation.Y <= programsWithWindows[activeProgram][activeWindow].windowRect.Top + LedDiam + Space
                    && programsWithWindows[activeProgram][activeWindow].windowRect.Contains(mouseLocation)) || (moveLeftBorder && moveTopBorder))
                {
                    this.Cursor = Cursors.SizeNWSE;

                    if (moveLeftBorder && moveTopBorder)
                    {
                        MoveLeftBorder(mouseLocation);
                        MoveTopBorder(mouseLocation);
                    }
                }
                else if ((mouseLocation.X <= programsWithWindows[activeProgram][activeWindow].windowRect.Left + LedDiam + Space
                    && mouseLocation.Y >= programsWithWindows[activeProgram][activeWindow].windowRect.Bottom - LedDiam - Space
                    && programsWithWindows[activeProgram][activeWindow].windowRect.Contains(mouseLocation)) || (moveLeftBorder && moveBottomBorder))
                {
                    this.Cursor = Cursors.SizeNESW;
                    if (moveLeftBorder && moveBottomBorder)
                    {
                        MoveLeftBorder(mouseLocation);
                        MoveBottomBorder(mouseLocation);
                    }
                }
                else if ((mouseLocation.X >= programsWithWindows[activeProgram][activeWindow].windowRect.Right - LedDiam - Space
                    && mouseLocation.Y <= programsWithWindows[activeProgram][activeWindow].windowRect.Top + LedDiam + Space
                    && programsWithWindows[activeProgram][activeWindow].windowRect.Contains(mouseLocation)) || (moveRightBorder && moveTopBorder))
                {
                    this.Cursor = Cursors.SizeNESW;

                    if (moveRightBorder && moveTopBorder)
                    {
                        MoveRightBorder(mouseLocation);
                        MoveTopBorder(mouseLocation);
                    }
                }
                else if ((mouseLocation.X >= programsWithWindows[activeProgram][activeWindow].windowRect.Right - LedDiam - Space
                    && mouseLocation.Y >= programsWithWindows[activeProgram][activeWindow].windowRect.Bottom - LedDiam - Space
                    && programsWithWindows[activeProgram][activeWindow].windowRect.Contains(mouseLocation)) || (moveRightBorder && moveBottomBorder))
                {
                    this.Cursor = Cursors.SizeNWSE;
                    if (moveRightBorder && moveBottomBorder)
                    {
                        MoveRightBorder(mouseLocation);
                        MoveBottomBorder(mouseLocation);
                    }
                }
                else if (((mouseLocation.X <= programsWithWindows[activeProgram][activeWindow].windowRect.Left + LedDiam + Space
                    || mouseLocation.X >= programsWithWindows[activeProgram][activeWindow].windowRect.Right - LedDiam - Space)
                    && programsWithWindows[activeProgram][activeWindow].windowRect.Contains(mouseLocation)) || (moveLeftBorder || moveRightBorder))
                {
                    this.Cursor = Cursors.SizeWE;

                    if (moveLeftBorder && mouseLocation.X >= 0
                        && mouseLocation.X + 8 * (LedDiam + Space) <= programsWithWindows[activeProgram][activeWindow].windowRect.Right)
                    {
                        MoveLeftBorder(mouseLocation);
                    }
                    else if (moveRightBorder && mouseLocation.X <= screenBitmap.Width
                        && mouseLocation.X - 8 * (LedDiam + Space) >= programsWithWindows[activeProgram][activeWindow].windowRect.X)
                    {
                        MoveRightBorder(mouseLocation);
                    }
                }
                else if (((mouseLocation.Y <= programsWithWindows[activeProgram][activeWindow].windowRect.Top + LedDiam + Space
                    || mouseLocation.Y >= programsWithWindows[activeProgram][activeWindow].windowRect.Bottom - LedDiam - Space)
                    && programsWithWindows[activeProgram][activeWindow].windowRect.Contains(mouseLocation)) || (moveTopBorder || moveBottomBorder))
                {
                    this.Cursor = Cursors.SizeNS;

                    if (moveTopBorder && mouseLocation.Y >= 0
                        && mouseLocation.Y + 2 * (LedDiam + Space) <= programsWithWindows[activeProgram][activeWindow].windowRect.Bottom)
                    {
                        MoveTopBorder(mouseLocation);
                    }
                    else if (moveBottomBorder && mouseLocation.Y <= screenBitmap.Height
                        && mouseLocation.Y - 2 * (LedDiam + Space) >= programsWithWindows[activeProgram][activeWindow].windowRect.Y)
                    {
                        MoveBottomBorder(mouseLocation);
                    }
                }
                else if (programsWithWindows[activeProgram][activeWindow].windowRect.Contains(mouseLocation) || moveWindow)
                {
                    this.Cursor = Cursors.SizeAll;

                    if (moveWindow && mouseLocation.X >= 0 && mouseLocation.X <= screenBitmap.Width && mouseLocation.Y >= 0 && mouseLocation.Y <= screenBitmap.Height)
                    {
                        MoveActiveWindow(mouseLocation);
                    }
                }
                else if (!programsWithWindows[activeProgram][activeWindow].windowRect.Contains(mouseLocation) && mouseLocation.Y > screenBitmapOffset.Y)
                {
                    //foreach (WindowOnPreview window in programsWithWindows[activeProgram])
                    for (int i = programsWithWindows[activeProgram].Count - 1; i >= 0; i--)
                    {
                        //if (window.windowRect.Contains(mouseLocation))
                        if (programsWithWindows[activeProgram][i].windowRect.Contains(mouseLocation))
                        {
                            this.Cursor = Cursors.Hand;
                            hoverWindow = i;
                            break;
                        }
                    }
                }
                else //if(!moveWindow && !moveLeftBorder && !moveRightBorder && !moveTopBorder && !moveBottomBorder)
                {
                    this.Cursor = Cursors.Default;
                }
            }
            this.Invalidate();
        }

        private void PreviewWindow_MouseDown(object sender, MouseEventArgs e)
        {
            if (((MainWindow)Owner).playbillTree.SelectedNode == null || (((MainWindow)Owner).playbillTree.SelectedNode.Tag.ToString() != "window"
                && ((MainWindow)Owner).playbillTree.SelectedNode.Tag.ToString() != "item")
                || (((MainWindow)Owner).playbillTree.SelectedNode.Tag.ToString() == "item"
                && SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].PlaybillItem.ItemType != PlayWindowItemType.BitmapText))
            {
                this.Cursor = Cursors.Default;
                return;
            }

            Point mouseLocation = e.Location;
            mouseLocation.Offset(-screenBitmapOffset.X, -screenBitmapOffset.Y);
            baseMousePosition = mouseLocation;

            if (manipulateItemCheckBox.Checked)
            {
                //Rectangle itemRect = programsWithWindows[activeProgram][activeWindow].windowRect;
                //Point location = itemRect.Location;
                //Point itemOffset = SelectedWindow.Animations[SelectedWindow.CurrentAnimation].PlaybillItem.Offset;
                //itemOffset = new Point(itemOffset.X * (LedDiam + Space), itemOffset.Y  * (LedDiam + Space));

                //location.Offset(itemOffset);
                //itemRect.Location = location;
                //itemRect.Width -= itemOffset.X;
                ////itemRect.Height -= itemOffset.Y;
                //itemRect.Height = SelectedWindow.Animations[SelectedWindow.CurrentAnimation].TextBitmapHeight * (LedDiam + Space);
                //itemRect.Height -= itemOffset.Y - itemSizeChange * (LedDiam + Space);

                Rectangle itemRect = programsWithWindows[activeProgram][activeWindow].windowRect;
                Point location = itemRect.Location;
                Point itemOffset = SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].PlaybillItem.Offset;
                itemOffset = new Point(itemOffset.X * (LedDiam + Space), itemOffset.Y * (LedDiam + Space));

                location.Offset(itemOffset);
                itemRect.Location = location;
                itemRect.Width -= itemOffset.X;
                //itemRect.Height -= itemOffset.Y;
                itemRect.Height = SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].TextBitmapHeight * (LedDiam + Space);
                if (itemRect.Bottom > SelectedWindow.windowRect.Bottom)
                    itemRect.Height -= SelectedWindow.windowRect.Bottom - itemRect.Bottom;
                    //itemRect.Height -= itemOffset.Y;
                if (resizeItem)
                    itemRect.Height += itemSizeChange * (LedDiam + Space);
                //itemRect.Height -= itemOffset.Y - itemSizeChange * (LedDiam + Space);

                if (((mouseLocation.Y >= itemRect.Bottom - LedDiam - Space) || (mouseLocation.Y <= itemRect.Top + LedDiam + Space)) && itemRect.Contains(mouseLocation))
                {
                    if ((mouseLocation.Y >= itemRect.Bottom - LedDiam - Space))
                        resizingBottomItemBorder = true;
                    else
                        resizingTopItemBorder = true;

                    resizeItem = true;
                    ResizeActiveItem(mouseLocation);
                }
                else if (itemRect.Contains(mouseLocation))
                {
                    movingItem = true;
                    MoveActiveItem(mouseLocation);
                }
            } 
            else if (mouseLocation.X <= programsWithWindows[activeProgram][activeWindow].windowRect.Left + LedDiam + Space
                && mouseLocation.Y <= programsWithWindows[activeProgram][activeWindow].windowRect.Top + LedDiam + Space
                && programsWithWindows[activeProgram][activeWindow].windowRect.Contains(mouseLocation))
            {
                moveLeftBorder = true;
                moveTopBorder = true;
            }
            else if (mouseLocation.X <= programsWithWindows[activeProgram][activeWindow].windowRect.Left + LedDiam + Space
                && mouseLocation.Y >= programsWithWindows[activeProgram][activeWindow].windowRect.Bottom - LedDiam - Space)
            {
                moveLeftBorder = true;
                moveBottomBorder = true;
            }
            else if (mouseLocation.X >= programsWithWindows[activeProgram][activeWindow].windowRect.Right - LedDiam - Space
                && mouseLocation.Y <= programsWithWindows[activeProgram][activeWindow].windowRect.Top + LedDiam + Space
                && programsWithWindows[activeProgram][activeWindow].windowRect.Contains(mouseLocation))
            {
                moveRightBorder = true;
                moveTopBorder = true;
            }
            else if (mouseLocation.X >= programsWithWindows[activeProgram][activeWindow].windowRect.Right - LedDiam - Space
                && mouseLocation.Y >= programsWithWindows[activeProgram][activeWindow].windowRect.Bottom - LedDiam - Space
                && programsWithWindows[activeProgram][activeWindow].windowRect.Contains(mouseLocation))
            {
                moveRightBorder = true;
                moveBottomBorder = true;
            }
            else if ((mouseLocation.X <= programsWithWindows[activeProgram][activeWindow].windowRect.Left + LedDiam + Space)
                && programsWithWindows[activeProgram][activeWindow].windowRect.Contains(mouseLocation))
            {
                moveLeftBorder = true;
            }
            else if ((mouseLocation.X >= programsWithWindows[activeProgram][activeWindow].windowRect.Right - LedDiam - Space)
                && programsWithWindows[activeProgram][activeWindow].windowRect.Contains(mouseLocation))
            {
                moveRightBorder = true;
            }
            else if ((mouseLocation.Y <= programsWithWindows[activeProgram][activeWindow].windowRect.Top + LedDiam + Space)
                && programsWithWindows[activeProgram][activeWindow].windowRect.Contains(mouseLocation))
            {
                moveTopBorder = true;
            }
            else if ((mouseLocation.Y >= programsWithWindows[activeProgram][activeWindow].windowRect.Bottom - LedDiam - Space)
                && programsWithWindows[activeProgram][activeWindow].windowRect.Contains(mouseLocation))
            {
                moveBottomBorder = true;
            }
            else if (programsWithWindows[activeProgram][activeWindow].windowRect.Contains(mouseLocation))
            {
                moveWindow = true;
            }
            else if (this.Cursor == Cursors.Hand)
            {
                selectOtherWindow = true;
            }
            else //if (!moveWindow && !moveLeftBorder && !moveRightBorder && !moveTopBorder && !moveBottomBorder)
            {
                //this.Cursor = Cursors.Default;
            }


            if (!manipulateItemCheckBox.Checked)
                ((MainWindow)this.Owner).windowsizechanged = true;
        }

        private void PreviewWindow_MouseUp(object sender, MouseEventArgs e)
        {
            if (((MainWindow)Owner).playbillTree.SelectedNode == null || (((MainWindow)Owner).playbillTree.SelectedNode.Tag.ToString() != "window"
                && ((MainWindow)Owner).playbillTree.SelectedNode.Tag.ToString() != "item")
                || (((MainWindow)Owner).playbillTree.SelectedNode.Tag.ToString() == "item"
                && SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].PlaybillItem.ItemType != PlayWindowItemType.BitmapText))
            {
                this.Cursor = Cursors.Default;
                return;
            }

            Point mouseLocation = e.Location;
            mouseLocation.Offset(-screenBitmapOffset.X, -screenBitmapOffset.Y);

            //if (moveWindow || moveLeftBorder || moveRightBorder || moveTopBorder || moveBottomBorder || selectOtherWindow)
            {
                if (((MainWindow)Owner).playbillTree.SelectedNode.Tag.ToString() == "window")
                {
                    if (selectOtherWindow)
                    {
                        activeWindow = hoverWindow;
                        hoverWindow = -1;
                    }

                    if (moveWindow || moveLeftBorder || moveRightBorder || moveTopBorder || moveBottomBorder)
                    {
                        Rectangle activeWindowRect = programsWithWindows[activeProgram][activeWindow].windowRect;

                        ((MainWindow)this.Owner).CurrentPlaybill.ProgramsList[ActiveProgram].WindowsList[ActiveWindow].X = Convert.ToUInt16(activeWindowRect.X / (LedDiam + Space));
                        ((MainWindow)this.Owner).CurrentPlaybill.ProgramsList[ActiveProgram].WindowsList[ActiveWindow].Y = Convert.ToUInt16(activeWindowRect.Y / (LedDiam + Space));
                        ((MainWindow)this.Owner).CurrentPlaybill.ProgramsList[ActiveProgram].WindowsList[ActiveWindow].Width = Convert.ToUInt16(activeWindowRect.Width / (LedDiam + Space));
                        ((MainWindow)this.Owner).CurrentPlaybill.ProgramsList[ActiveProgram].WindowsList[ActiveWindow].Height = Convert.ToUInt16(activeWindowRect.Height / (LedDiam + Space));

                        ((MainWindow)this.Owner).playbillTree.SelectedNode = null; //((MainWindow)this.Owner).playbillTree.Nodes["mainNode"];
                    }

                    if (((MainWindow)this.Owner).playbillTree.Nodes["mainNode"].Nodes.Count > ActiveProgram
                        && ((MainWindow)this.Owner).playbillTree.Nodes["mainNode"].Nodes.Count > 1
                        && ((MainWindow)this.Owner).playbillTree.Nodes["mainNode"].Nodes[ActiveProgram].Nodes.Count > ActiveWindow
                        && ((MainWindow)this.Owner).playbillTree.Nodes["mainNode"].Nodes[ActiveProgram].Nodes.Count > 1)
                    {
                        ((MainWindow)this.Owner).playbillTree.SelectedNode = ((MainWindow)this.Owner).playbillTree.Nodes["mainNode"].Nodes[ActiveProgram].Nodes[ActiveWindow];
                    }



                    if (moveWindow || moveLeftBorder || moveRightBorder || moveTopBorder || moveBottomBorder)
                    {
                        if (((MainWindow)this.Owner).windowsizechanged == true)
                        {
                            TreeNode selectedNode = ((MainWindow)this.Owner).playbillTree.SelectedNode;
                            int programNumber = selectedNode.Parent.Index;


                            foreach (TreeNode t in selectedNode.Nodes)
                            {
                                if (t != selectedNode.LastNode)
                                {
                                    ((MainWindow)this.Owner).RegenerateItemBitmaps(t);
                                }
                            }

                            //((MainWindow)this.Owner).playbillTree.SelectedNode = selectedNode;
                            ((MainWindow)this.Owner).wndchangingItemNumber = SelectedWindow.CurrentAnimationNumber = 0;
                            ((MainWindow)this.Owner).windowsizechanged = false;
                            
                        }
                    }

                    DrawStaticWindows();


                    moveWindow = false;
                    moveLeftBorder = false;
                    moveRightBorder = false;
                    moveTopBorder = false;
                    moveBottomBorder = false;
                    selectOtherWindow = false;

                    if (ghostBitmap != null)
                    {
                        ghostBitmap.Dispose();
                        ghostBitmap = null;
                    }

                    //Graphics ghostGraphics = Graphics.FromImage(ghostBitmap);
                    //ghostGraphics.Clear();
                    
                    //ghostGraphics.Dispose();
                }
                else if (((MainWindow)Owner).playbillTree.SelectedNode.Tag.ToString() == "item" && movingItem)
                {
                    ((MainWindow)Owner).OpenItemTab(SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].PlaybillItem, false);
                    
                    SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].InputBitmaps = 
                        SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].PlaybillItem.BitmapsList;

                    SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].ShowStatic();
                    SelectedWindow.Redraw();
                    movingItem = false;
                }
                else if (((MainWindow)Owner).playbillTree.SelectedNode.Tag.ToString() == "item" && resizeItem)
                {
                    if (((MainWindow)Owner).bitmaptextFontSizeComboBox.SelectedIndex + itemSizeChange/2 > -1
                        && ((MainWindow)Owner).bitmaptextFontSizeComboBox.SelectedIndex + itemSizeChange/2 < ((MainWindow)Owner).bitmaptextFontSizeComboBox.Items.Count)
                    {
                        //((MainWindow)Owner).ChangeTextSize(itemSizeChange / 2, 0, ((MainWindow)Owner).bitmaptextRichTextBox.TextLength);
                        ((MainWindow)Owner).bitmaptextRichTextBox.SelectAll();
                        
                        ((MainWindow)Owner).bitmaptextFontSizeComboBox.SelectedIndex += itemSizeChange/2;
                        SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].PlaybillItem.FontSize += itemSizeChange / 2;
                        ((MainWindow)Owner).OpenItemTab(SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].PlaybillItem, false);

                        SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].InputBitmaps =
                            SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].PlaybillItem.BitmapsList;
                    }
                    SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].ShowStatic();
                    SelectedWindow.Redraw();
                    
                    resizeItem = false;
                    resizingTopItemBorder = false;
                    resizingBottomItemBorder = false;
                    itemSizeChange = 0;



                    Rectangle itemRect = programsWithWindows[activeProgram][activeWindow].windowRect;
                    Point location = itemRect.Location;
                    Point itemOffset = SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].PlaybillItem.Offset;
                    itemOffset = new Point(itemOffset.X * (LedDiam + Space), itemOffset.Y * (LedDiam + Space));

                    location.Offset(itemOffset);
                    itemRect.Location = location;
                    itemRect.Width -= itemOffset.X;
                    //itemRect.Height -= itemOffset.Y;
                    itemRect.Height = SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].TextBitmapHeight * (LedDiam + Space);

                    if (itemRect.Bottom > SelectedWindow.windowRect.Bottom)
                        itemRect.Height -= SelectedWindow.windowRect.Bottom - itemRect.Bottom;
                    //itemRect.Height -= itemOffset.Y;

                    if (resizeItem)
                        itemRect.Height -= itemSizeChange * (LedDiam + Space);

                    if (ghostBitmap != null)
                        ghostBitmap.Dispose();
                    ghostBitmap = new Bitmap(screenBitmap.Width, screenBitmap.Height);
                    //ghostRectangle.Width, ghostRectangle.Height);

                    Graphics ghostGraphics = Graphics.FromImage(ghostBitmap);

                    int tmpChangeVal = 0;
                    Pen ghostPen = new Pen(Color.Azure);
                    ghostPen.DashStyle = DashStyle.DashDot;
                    ghostPen.Width = (LedDiam + Space);


                    ghostGraphics.DrawRectangle(ghostPen, itemRect);
                    ghostGraphics.FillRectangle(new SolidBrush(Color.FromArgb(50, Color.Azure)), itemRect);
                    ghostGraphics.Dispose();
                }
            }

            this.Cursor = Cursors.Default;
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DrawStaticWindows();
        }

        public void DrawStaticWindows()
        {
            using(Graphics screenGraphics = Graphics.FromImage(screenBitmap))
            {
                //screenGraphics.DrawImageUnscaled(offScreenBitmap, 0, 0);

            //if (selectOtherWindow)
            //{
            //    programsWithWindows[activeProgram][activeWindow].DrawStatic(false);
            //    programsWithWindows[activeProgram][hoverWindow].DrawStatic(true);

            //    screenGraphics.DrawImageUnscaled(programsWithWindows[activeProgram][activeWindow].windowBitmap,
            //        programsWithWindows[activeProgram][activeWindow].windowRect);
            //    screenGraphics.DrawImageUnscaled(programsWithWindows[activeProgram][hoverWindow].windowBitmap,
            //        programsWithWindows[activeProgram][hoverWindow].windowRect);

            //    activeWindow = hoverWindow;
            //    hoverWindow = -1;
            //    selectOtherWindow = false;
            //}
            //else
            //{
                
                if (programsWithWindows != null && programsWithWindows.Count > 0 && ActiveProgram > -1 && ActiveProgram < programsWithWindows.Count
                    && programsWithWindows[ActiveProgram] != null && programsWithWindows[ActiveProgram].Count > 0)
                {
                    for (int i = 0; i < programsWithWindows[ActiveProgram].Count; i++)
                    {
                        if (i == activeWindow)
                        {
                            List<Rectangle> tmpIntersets = null;
                            /*for (int j = 0; j < programsWithWindows[ActiveProgram].Count; j++)
                            {
                                if (i != j &&
                                    programsWithWindows[ActiveProgram][i].windowRect.IntersectsWith(programsWithWindows[ActiveProgram][j].windowRect))
                                {
                                    if (tmpIntersets == null)
                                        tmpIntersets = new List<Rectangle>();

                                    tmpIntersets.Add(Rectangle.Intersect(programsWithWindows[ActiveProgram][i].windowRect, programsWithWindows[ActiveProgram][j].windowRect));
                                }
                            }*/
                            programsWithWindows[ActiveProgram][i].DrawStatic(true, tmpIntersets);
                        }
                        else
                            programsWithWindows[ActiveProgram][i].DrawStatic(false, null);

                        //screenGraphics.DrawImageUnscaled(programsWithWindows[ActiveProgram][i].windowBitmap, programsWithWindows[ActiveProgram][i].windowRect);
                    }
                }
            //}
            }
            this.Invalidate();
        }

        private void manipulateItemCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked)
            {
                if (((MainWindow)Owner).playbillTree.SelectedNode.Tag.ToString() == "item")
                {
                    int selectedItem = SelectedWindow.CurrentAnimationNumber;
                    SelectedWindow.StopAnimation();
                    SelectedWindow.CurrentAnimationNumber = selectedItem;
                    SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].ShowStatic();
                    SelectedWindow.Redraw();
                    //this.Invalidate();
                    //movingItem = true;

                    //SelectedWindow.Animations[SelectedWindow.CurrentAnimation].
                }
            }
            else
            {
                //Graphics ghostGraphics = Graphics.FromImage(ghostBitmap);
                //ghostGraphics.
                movingItem = false;
                
                ghostBitmap = null;
                if (((MainWindow)Owner).playbillTree.SelectedNode.Tag.ToString() == "item")
                {
                   // ((MainWindow)Owner).RtbToBitmap();
                    SelectedWindow.Animate(false, SelectedWindow.CurrentAnimationNumber);

                    //((MainWindow)Owner).OpenItemTab(SelectedWindow.Animations[SelectedWindow.CurrentAnimation].PlaybillItem, false);
                }
            }
        }

        void manipulateItemTimer_Tick(object sender, EventArgs e)
        {
            if (++ticksCount >= 2)
            {
                Cursor currentCursor = this.Cursor;
                this.Cursor = Cursors.WaitCursor;

                int curr = SelectedWindow.CurrentAnimationNumber;
                SelectedWindow.StopAnimation();
                SelectedWindow.CurrentAnimationNumber = curr;
                SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].ShowStatic();
                SelectedWindow.Redraw();

                manipulateItemCheckBox.Checked = true;

                Rectangle ghostRectangle = programsWithWindows[activeProgram][activeWindow].windowRect;

                //ghostRectangle.Offset(
                //    SelectedWindow.Animations[SelectedWindow.CurrentAnimation].PlaybillItem.Offset.X * (LedDiam + Space),
                //    SelectedWindow.Animations[SelectedWindow.CurrentAnimation].PlaybillItem.Offset.Y * (LedDiam + Space));
                //Point location = ghostRectangle.Location;
                Point location = ghostRectangle.Location;
                Point itemOffset = SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].PlaybillItem.Offset;
                itemOffset = new Point(itemOffset.X * (LedDiam + Space), itemOffset.Y * (LedDiam + Space));

                location.Offset(itemOffset);
                ghostRectangle.Location = location;


                ghostRectangle.Location = location;
                ghostRectangle.Width -= itemOffset.X;
                ghostRectangle.Height = SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].TextBitmapHeight * (LedDiam + Space);
                if (ghostRectangle.Bottom > SelectedWindow.windowRect.Bottom)
                    ghostRectangle.Height -= SelectedWindow.windowRect.Bottom - ghostRectangle.Bottom;
                //ghostRectangle.Height -= itemOffset.Y;
                //ghostRectangle.Height -= itemOffset.Y - itemSizeChange * (LedDiam + Space);

                if (ghostBitmap != null)
                    ghostBitmap.Dispose();
                ghostBitmap = new Bitmap(screenBitmap.Width, screenBitmap.Height);
                //ghostRectangle.Width, ghostRectangle.Height);

                using (Graphics ghostGraphics = Graphics.FromImage(ghostBitmap))
                {
                    int tmpChangeVal = 0;
                    Pen ghostPen = new Pen(Color.Azure);
                    ghostPen.DashStyle = DashStyle.DashDot;
                    ghostPen.Width = (LedDiam + Space);

                    ghostGraphics.DrawRectangle(ghostPen, ghostRectangle);
                    ghostGraphics.FillRectangle(new SolidBrush(Color.FromArgb(50, Color.Azure)), ghostRectangle);
                }

                this.Cursor = currentCursor;

                manipulateItemTimer.Stop();
                manipulateItemTimer.Dispose();
                manipulateItemTimer = null;
                ticksCount = 0;
            }
            //previewZoomLabel.Text = ticksCount.ToString();
        }

        private void PreviewWindow_MouseEnter(object sender, EventArgs e)
        {
            if (((MainWindow)Owner).playbillTree.SelectedNode.Tag.ToString() != "item" 
                || SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].PlaybillItem.ItemType != PlayWindowItemType.BitmapText)
                return;

            if (manipulateItemTimer != null)
            {
                manipulateItemTimer.Stop();
                manipulateItemTimer.Dispose();
                manipulateItemTimer = null;
                ticksCount = 0;
            }

            manipulateItemTimer = new System.Windows.Forms.Timer();
            manipulateItemTimer.Interval = 100;
            manipulateItemTimer.Tick += new EventHandler(manipulateItemTimer_Tick);
            manipulateItemTimer.Start();
        }

        private void PreviewWindow_MouseLeave(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Default;

            if (((MainWindow)Owner).playbillTree.SelectedNode.Tag.ToString() != "item"
                || SelectedWindow.Animations[SelectedWindow.CurrentAnimationNumber].PlaybillItem.ItemType != PlayWindowItemType.BitmapText)
                return;

            if (manipulateItemTimer != null)
            {
                manipulateItemTimer.Stop();
                manipulateItemTimer.Dispose();
                manipulateItemTimer = null;
                ticksCount = 0;
            }

            if (manipulateItemCheckBox.Checked == true)
            {
                Cursor currentCursor = this.Cursor;
                this.Cursor = Cursors.WaitCursor;


                ghostBitmap = null;
                manipulateItemCheckBox.Checked = false;

                SelectedWindow.Animate(false, SelectedWindow.CurrentAnimationNumber);



                this.Cursor = Cursors.Default;//currentCursor;
            }
        }

        private void PreviewWindow_Move(object sender, EventArgs e)
        {
            foreach (var p in programsWithWindows)
            {
                foreach (WindowOnPreview w in p)
                {
                    foreach (PreviewAnimation a in w.Animations)
                        a.PauseAnimation();
                }
            }
        }

        private void PreviewWindow_ResizeEnd(object sender, EventArgs e)
        {
            foreach (var p in programsWithWindows)
            {
                foreach (WindowOnPreview w in p)
                {
                    foreach (PreviewAnimation a in w.Animations)
                        a.UnpauseAnimation();
                }
            }
        }

    }



    public enum PreviewMode
    {
        StaticItem,
        AnimateItem,
        AnimateActiveWindow,
        StaticProgram,
        AnimateActiveProgram,
        AnimateAll
    }
}
