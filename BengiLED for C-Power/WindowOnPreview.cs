using System.Collections.Generic;

using System.Drawing;
using System.Threading;
using System;

namespace BengiLED_for_C_Power
{
    public class WindowOnPreview
    {
        public Bitmap windowBitmap;

        public Bitmap testbmp;
        public Bitmap windowRedrawBitmap;
        public Rectangle windowRect;
        private int x, y, width, height;
        private int currentAnimationNumber = 0;
        private bool playNextProgram = false;
        public System.Threading.Timer animationTimer = null;
        delegate void AnimateFunc();
        bool animateAll = false;

        public bool active;
        public List<PreviewAnimation> Animations = null;
        public bool repaintNow = false;

        public PreviewWindow Owner { get; set; }
        
        public int X
        {
            get { return x; }
            set { x = value; }
        }
        public int Y
        {
            get { return y; }
            set { y = value; }
        }
        public int Width
        {
            get { return width; }
            set 
            {
                width = value;
                if (active)
                {
                    if (windowBitmap != null)
                        windowBitmap.Dispose();
                    windowBitmap = new Bitmap(Width, Height);

                    if (testbmp != null)
                        testbmp.Dispose();
                    testbmp = new Bitmap(Width / (PreviewWindow.LedDiam + PreviewWindow.Space), Height / (PreviewWindow.LedDiam + PreviewWindow.Space));
                }
            }
        }
        public int Height
        {
            get { return height; }
            set 
            { 
                height = value;
                if (active)
                {
                    if (windowBitmap != null)
                        windowBitmap.Dispose();
                    windowBitmap = new Bitmap(Width, Height);

                    if (testbmp != null)
                        testbmp.Dispose();
                    testbmp = new Bitmap(Width / (PreviewWindow.LedDiam + PreviewWindow.Space), Height / (PreviewWindow.LedDiam + PreviewWindow.Space));
                }
            }
        }
        public int CurrentAnimationNumber
        {
            get { return currentAnimationNumber; } 
            set
            {
                if (Animations.Count > 0 && currentAnimationNumber < Animations.Count)
                    Animations[currentAnimationNumber].StopAnimation();
                currentAnimationNumber = (value < Animations.Count) ? value : 0;
            }
        }

        public PreviewAnimation CurrentAnimation
        {
            get 
            {
                try { return Animations[CurrentAnimationNumber]; }
                catch { return null; }
            }
        }

        #region Methods

        #region Constructing
        public WindowOnPreview(PreviewWindow ownerForm, int x, int y, int w, int h, int diamWithSpace)
        {
            Owner = ownerForm;
            
            windowRect = new Rectangle(x * diamWithSpace, y * diamWithSpace, w * diamWithSpace, h * diamWithSpace);

            this.X = x * diamWithSpace;
            this.Y = y * diamWithSpace;
            this.Width = w * diamWithSpace;
            this.Height = h * diamWithSpace;

            active = false;

            windowBitmap = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);


            if (testbmp != null)
                testbmp.Dispose();
            testbmp = new Bitmap(Width / (PreviewWindow.LedDiam + PreviewWindow.Space), Height / (PreviewWindow.LedDiam + PreviewWindow.Space));

            Animations = new List<PreviewAnimation>();
            CurrentAnimationNumber = 0;
        }

        ~WindowOnPreview()
        {
            Animations.Clear();
            if (windowBitmap != null)
                windowBitmap.Dispose();


            if (testbmp != null)
                testbmp.Dispose();
        }

        public void Clear()
        {
            //StopAnimation();
            foreach (PreviewAnimation animation in Animations)
                animation.Clear();
            Animations.Clear();

            if (windowBitmap != null)
                windowBitmap.Dispose();

            if (testbmp != null)
                testbmp.Dispose();

            if (windowRedrawBitmap != null)
                windowRedrawBitmap.Dispose();

            if (animationTimer != null)
                animationTimer.Dispose();
        } 
        #endregion

        public delegate void MethodInvoker();
        
        public void Redraw()
        {
            DateTime start = DateTime.Now;


            if (!Owner.IsHandleCreated && !Owner.IsDisposed) return;

            if (//Owner.screenBitmap != null && 
                Owner.ActiveProgram < PreviewWindow.programsWithWindows.Count)
            {
                try
                {
                    //Graphics previewGraphics = Graphics.FromImage(Owner.screenBitmap);
                    //previewGraphics.DrawImageUnscaled(windowBitmap, X, Y);
                    //previewGraphics.Dispose();

                    int thisIndex = PreviewWindow.programsWithWindows[Owner.ActiveProgram].IndexOf(this);
                    //foreach (WindowOnPreview otherWindow in PreviewWindow.programsWithWindows[Owner.ActiveProgram])
                    if (thisIndex >= 0 && PreviewWindow.programsWithWindows[Owner.ActiveProgram].Count > 1 && thisIndex < PreviewWindow.programsWithWindows[Owner.ActiveProgram].Count)
                    {
                        for (int i = thisIndex + 1; i < PreviewWindow.programsWithWindows[Owner.ActiveProgram].Count; i++)
                        {
                            if (this.windowRect.IntersectsWith(PreviewWindow.programsWithWindows[Owner.ActiveProgram][i].windowRect))
                            {
                                Rectangle intersectRect = Rectangle.Intersect(this.windowRect, PreviewWindow.programsWithWindows[Owner.ActiveProgram][i].windowRect);

                                //for (int x = intersectRect.Left; x < intersectRect.Right; x++)
                                //{
                                //    for (int y = intersectRect.Top; y < intersectRect.Bottom; y++)
                                //    {
                                //        Color tmpColor = windowBitmap.GetPixel(x - (windowRect.X), y - (windowRect.Y));
                                //        windowBitmap.SetPixel(x - (windowRect.X), y - (windowRect.Y), Color.FromArgb(0, tmpColor));
                                //    }
                                //}
                                PreviewWindow.programsWithWindows[Owner.ActiveProgram][i].Redraw();
                            }
                        }
                    }

                    //Graphics previewGraphics = Graphics.FromImage(Owner.screenBitmap);
                    //previewGraphics.DrawImageUnscaled(windowBitmap, X, Y);
                    //previewGraphics.Dispose();

                    //windowRedrawBitmap = windowBitmap.Clone(new Rectangle(0, 0, windowBitmap.Width, windowBitmap.Height), System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    windowRedrawBitmap = new Bitmap(windowBitmap);
                    repaintNow = true;


                    Owner.Invoke((MethodInvoker)delegate
                    {
                        Owner.Invalidate(new Rectangle(Owner.screenBitmapOffset.X + X, Owner.screenBitmapOffset.Y + Y, Width, Height));
                        Owner.Update();
                    });
                }
                catch { }//System.Diagnostics.Debugger.Break(); }
                //repaintNow = false;
            }

            DateTime end = DateTime.Now;
            //MessageBox.Show((((TimeSpan)(end - start)).TotalMilliseconds).ToString(), "AnimateInstant");
            //Console.WriteLine("Redraw: - {0}ms", (((TimeSpan)(end - start)).TotalMilliseconds).ToString());
        }

        public void DrawStatic(bool active, List<Rectangle> intersects)
        {
            //if (MainWindow.regeneratePreviewTimer != null)
            //{
            //    MainWindow.regeneratePreviewTimer.Stop();
            //    MainWindow.regeneratePreviewTimer.Dispose();
            //    MainWindow.regeneratePreviewTimer = null;
            //}

            if(Animations.Count > 0)
                Animations[CurrentAnimationNumber].StopAnimation();
            CurrentAnimationNumber = 0;

            if (windowBitmap != null)
            {
                //windowGraphics = Graphics.FromImage(windowBitmap);
                //windowGraphics.Clear(PreviewWindow.SpaceColor);
                //windowGraphics.Dispose();

                //Animations[CurrentAnimation].ClearLEDs(intersects);
                //Redraw();
                windowBitmap.Dispose();
            }
            if (testbmp != null)
                testbmp.Dispose();
            windowBitmap = new Bitmap(windowRect.Width, windowRect.Height);



            if (testbmp != null)
                testbmp.Dispose();
            testbmp = new Bitmap(windowRect.Width / (PreviewWindow.LedDiam + PreviewWindow.Space), windowRect.Height / (PreviewWindow.LedDiam + PreviewWindow.Space));



            if (Animations.Count > 0) 
                Animations[CurrentAnimationNumber].ShowStatic();

            using (Graphics windowGraphics = Graphics.FromImage(windowBitmap))
            {
                if (active)
                {
                    windowGraphics.DrawRectangle(new Pen(Color.Gold, PreviewWindow.LedDiam + PreviewWindow.Space), 0, 0, windowRect.Width, windowRect.Height);
                    //windowGraphics.FillRectangle(new SolidBrush(Color.FromArgb(127, 200, 200, 200)), 0, 0, windowRect.Width, windowRect.Height);

                }
                else
                {
                    windowGraphics.DrawRectangle(new Pen(Color.HotPink), 0, 0, windowRect.Width, windowRect.Height);
                    windowGraphics.FillRectangle(new SolidBrush(Color.FromArgb(127, 100, 100, 100)), 0, 0, windowRect.Width, windowRect.Height);
                }
            }

            this.X = windowRect.X;
            this.Y = windowRect.Y;
            this.Width = windowRect.Width;
            this.Height = windowRect.Height;

            Redraw();
        }

        public void StopAnimation()
        {
            if(Animations.Count > 0 && CurrentAnimationNumber < Animations.Count)
                Animations[CurrentAnimationNumber].StopAnimation();
            CurrentAnimationNumber = 0;
        }
        #endregion


        /*
        public void Animate(bool animateAll, int startAnimation)
        {
            if (!Owner.Visible)
                StopAnimation();
            else if (Animations.Count > 0)
            {
                StopAnimation();
                CurrentAnimationNumber = startAnimation;
                this.animateAll = animateAll;
                Animations[CurrentAnimationNumber].Animate(animateAll);

            }
        }*/

        public void Animate(bool animateAll, int startAnimation)
        {
            if (!Owner.Visible)
            {
                StopAnimation();
                CurrentAnimationNumber = startAnimation;
            }
            else if (Animations.Count > 0)
            {
                //animationTimer.Change(0, Timeout.Infinite);
                StopAnimation();
                CurrentAnimationNumber = startAnimation;
                this.animateAll = animateAll;

                if (CurrentAnimation.InputBitmaps != null && CurrentAnimation.InputBitmaps.Count > 0)
                {
                    int timerInterval = (CurrentAnimation.PlaybillItem.ItemType == PlayWindowItemType.Animator) ?
                        1 : (Convert.ToInt32((1 - ((CurrentAnimation.Speed - 1) / 100.0)) * 10 * 100));
                    //CurrentAnimation
                    while (animationTimer != null)
                    {
                        //animationTimer.Change(Timeout.Infinite, Timeout.Infinite);
                        animationTimer.Dispose();
                        //if(animationTimer.i
                        animationTimer = null;
                    }

                    //animationTimer = new Timer(TimerTick, CurrentAnimation, 100, timerInterval);
                    //animationTimer = new Timer(TimerTick, this, 100, timerInterval);

                    Animations[CurrentAnimationNumber].Animate(animateAll);
                }
            }
        }

        private void TimerTick(object obj)
        {
            DateTime animationStart = DateTime.Now;

            if (obj == null)
            {
                animationTimer.Change(Timeout.Infinite, Timeout.Infinite);
                return;
            }
            //PreviewAnimation animation = obj as PreviewAnimation;
            WindowOnPreview window = (obj as WindowOnPreview);
            PreviewAnimation animation = window.CurrentAnimation;

            if (animation == null)
            {
                //animationTimer.Change(Timeout.Infinite, Timeout.Infinite);
                return;
            }

            if (animation.lastFrame && (animation.staticBitmapPixels.Length == 1 ||
                    (animation.staticBitmapPixels.Length > 1 && animation.currentBitmap == animation.staticBitmapPixels.Length-1)))
            {
                //animation.lastFrame = true;
                int stay = animation.StayTime*1000;
                window.CurrentAnimationNumber++;
                animation = window.CurrentAnimation;
                int timerInterval = (CurrentAnimation.PlaybillItem.ItemType == PlayWindowItemType.Animator) ?
                    1 : (Convert.ToInt32((1 - ((CurrentAnimation.Speed - 1) / 100.0)) * 10 * 100));
                animation.currentBitmap = 0;
                animationTimer.Change(stay, timerInterval);
            }
            animation.animateNext = animateAll;
            animation.AnimateFrame();
            //if(!animation.running)
            //animation.Animate(animateAll);
            //animation.ShowStatic();
            this.Redraw();
            animation.changedPixels.Clear();


            DateTime animationEnd = DateTime.Now;

        }
    }
}
