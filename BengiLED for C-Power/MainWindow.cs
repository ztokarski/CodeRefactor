using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Globalization;

namespace BengiLED_for_C_Power
{
    public partial class MainWindow : Form
    {
        #region Private fields
        private Playbill testPlaybill;
        private System.Resources.ResourceManager rm;
        private System.Collections.Hashtable LangHash = new System.Collections.Hashtable();
        private System.Collections.Hashtable mark8fontHashTable = new System.Collections.Hashtable();
        private System.Collections.Hashtable mark12fontHashTable = new System.Collections.Hashtable();
        private System.Collections.Hashtable mark16fontHashTable = new System.Collections.Hashtable();
        private System.Collections.Hashtable markBoldfontHashTable = new System.Collections.Hashtable();

        private string AutosaveFileName;// = string.Format("{0}/autosave.bpr", programSettingsFolder);
        private string NotBeginnerAutosaveFileName;// = string.Format("{0}/autosave.apr", programSettingsFolder);
        private string configFileName;// = string.Format("{0}/ledconfig.xml", programSettingsFolder);
      

        private ApplicationUserLevel userLevel = ApplicationUserLevel.Advanced;
        private bool changingLevelAtBeginning = true;
        private TabPage[] allSettingsTabs = new TabPage[4];
        private TabPage[] allItemsTabs;
        private Boolean delete = false;
        private Boolean allBrightnessAutoChanging = false;
        private Boolean allBrightnessValueChanging = false;
        private bool lmbDown = false;
        Image logoFromFile = null;

        private object[] allcolours = new object[] { "Black", "Red", "Lime", "Yellow", "Blue", "Magenta", "Aqua", "White" };
      //  private object[] allcolours = new object[] { "ff000000", "ffff0000", "ff00ff00", "ffffff00", "ff0000ff", "ffff00ff", "ff00ffff", "ffffffff" };

        ToolTip forButton = new ToolTip();
        private bool levelChanging = false, itemChanging = false;
        public static System.Windows.Forms.Timer regeneratePreviewTimer = null;
        private System.Windows.Forms.Timer testVersionTimer = null;
        PreviewWindow previewWindow;
        AlertWindow alertWindow;
        private TabPage simpleText;
        private int searchingFailResult = -1;
        NetworkSearchWindow netSearchWindow;
        public bool network = false;
        passWindow accessWindow;
        private ScreenSettingsForm settingsForm;
        private ProgramSettingsForm programSettingsForm;
        private BrightnessSettingsForm brightnessForm;
        private System.Collections.Hashtable configHash;
        private int bitmapTextSelectedFontColor = 0, bitmapTextSelectedBackgroundColor = 0, clockFontColor = 0, temperatureFontColor = 0;
        private Image backgroundImage = null, beginnerBackgroundImage = null;
        private bool changingTab = false;

        private BackgroundWorker bgWorker;
        private bool fromTimer = false;

        public LogWindow logWindow;
        public System.Collections.Hashtable logMessagesHashTable = new System.Collections.Hashtable();
        public System.Collections.Hashtable tooltipsHashTable = new System.Collections.Hashtable();
        public System.Collections.Hashtable messageBoxesHashTable = new System.Collections.Hashtable();

        public static ControllerSettings controllerSettings;

        private System.Windows.Forms.Timer programCurrentTimeTimer = null;
        private System.Windows.Forms.Timer clockCurrentTimeTimer = null;

        public bool windowsizechanged = false;
        public int wndchangingItemNumber = 0;

        #endregion

        public static string programSettingsFolder;// = string.Format("{0}/LedConfig", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData).ToString());
        public static bool oldAnimation = true;
        public static bool anotherBitmaps = false;
        public bool oldbitmaps = false;

        public int CurrentProgramNumber = -1;
        public int CurrentWindowNumber = -1;
        public int CurrentItemNumber = -1;
        public bool IsCurrentElementNew = false;

        public PlayProgram CurrentProgram
        {
            get { return CurrentPlaybill.ProgramsList[CurrentProgramNumber]; }
        }

        public PlayWindow CurrentWindow
        {
            get { return CurrentProgram.WindowsList[CurrentWindowNumber]; }
        }

        public PlayWindowItem CurrentItem
        {
            get { return CurrentWindow.ItemsList[CurrentItemNumber]; }
        }

        #region Structures & variables for converting rtf->bmp

        [DllImport("USER32.dll")]
        private static extern Int32 SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam);
        private const int WM_USER = 0x400;
        private const int EM_FORMATRANGE = WM_USER + 57;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CHARRANGE
        {
            public int cpMin;
            public int cpMax;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FORMATRANGE
        {
            public IntPtr hdc;
            public IntPtr hdcTarget;
            public RECT rc;
            public RECT rcPage;
            public CHARRANGE chrg;
        }

        private const double inch = 14.4;
        private Rectangle contentRectangle;
        private int selectedCountry = 0;
        private ImageList flagsList;

        #endregion

        #region Properties
        public Playbill CurrentPlaybill
        {
            get { return testPlaybill; }
            set { testPlaybill = value; }
        }
        public System.Collections.Hashtable ConfigHashtable
        {
            get { return configHash; }
            set { configHash = value; }
        }

        public Boolean AllBrightnessAutoChanging
        {
            get { return allBrightnessAutoChanging; }
            set { allBrightnessAutoChanging = value; }
        }

        public Boolean AllBrightnessValueChanging
        {
            get { return allBrightnessValueChanging; }
            set { allBrightnessValueChanging = value; }
        }

        public ApplicationUserLevel UserLevel
        {
            get { return userLevel; }
            set
            {
                levelChanging = true;
                ApplyUserLevel(value);
                userLevel = value;
                levelChanging = false;
            }
        }

        public Boolean Delete
        {
            get { return delete; }
            set { delete = value; }
        }
        public int SearchingFailResult
        {
            get { return searchingFailResult; }
            set { searchingFailResult = value; }
        }

        #endregion

        #region Methods

        public static void Log(ref LogWindow rf, string text)
        {
            rf.Message = text;
        }

        public MainWindow()
        {
            DateTime programStart = DateTime.Now;

            InitializeComponent();


            logWindow = new LogWindow();
            logWindow.Owner = this;

            logWindow.Clearlog();
            logWindow.Show();
            logWindow.Message = "Loading...";


            programSettingsFolder = string.Format("{0}/LedConfig", 
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).ToString());

            //programSettingsFolder = string.Format("{0}/LedConfig", 
            //    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData).ToString());

            if (!Directory.Exists(programSettingsFolder)
   //             || !Directory.Exists(string.Format("{0}/Languages", programSettingsFolder))
     //           || !File.Exists(string.Format("{0}/alleffects.xml", programSettingsFolder))
       //         || !File.Exists(string.Format("{0}/ledconfig.xml", programSettingsFolder))
                )
            {
                if(!Directory.Exists(string.Format("{0}/Languages", Application.StartupPath))
                    || !File.Exists(string.Format("{0}/alleffects.xml", Application.StartupPath))
                    || !File.Exists(string.Format("{0}/ledconfig.xml", Application.StartupPath)))
                {
                    Directory.CreateDirectory(programSettingsFolder);
                }
                else
                {
                    programSettingsFolder = Application.StartupPath;
                }
            }

          

            AutosaveFileName = string.Format("{0}/autosave.bpr", programSettingsFolder);
            NotBeginnerAutosaveFileName = string.Format("{0}/autosave.apr", programSettingsFolder);
            configFileName = string.Format("{0}/ledconfig.xml", programSettingsFolder);

   

            if (IsReleaseInvalid())
            {
                return;
            }


            this.ResizeEnd += new EventHandler(MainWindow_ResizeEnd);

            previewWindow = new PreviewWindow();
            previewWindow.Owner = this;
            netSearchWindow = new NetworkSearchWindow();
            netSearchWindow.Owner = this;
            settingsForm = new ScreenSettingsForm();
            settingsForm.Owner = this;
            //programSettingsForm = new ProgramSettingsForm();
            //programSettingsForm.Owner = this;
            brightnessForm = new BrightnessSettingsForm();
            brightnessForm.Owner = this;
            //accessWindow = new passWindow("1321");
            accessWindow = new passWindow("f9be311e65d81a9ad8150a60844bb94c");
            accessWindow.Owner = this;

           
            SetupDateTimeFormats();
            GenerateFontsHashTables();

            GenerateLocalizationHashTables();
            LoadLanguages();

            LoadConfiguration();


            SetTooltips();

            SearchingFailResult = -1;
            InitializeBgWorker();

            screenFoundInfoLabel.Text = "NOT FOUND";
            screenFoundInfoLabel.ForeColor = Color.Red;

            DateTime searchStart = DateTime.Now;
            SearchForScreen();
            DateTime searchEnd = DateTime.Now;



            poweredByLinkLabel.Links.Clear();
            poweredByLinkLabel.Links.Add(new LinkLabel.Link(11, poweredByLinkLabel.Text.Length - 11, "www.bengiteam.pl"));

            this.Text = string.Format("{0} - {1}", this.Text, RetrieveLinkerTimestamp());

            dayOfWeekItemLabel.Text = dateClockPicker.Value.DayOfWeek.ToString();
            dayOfWeekProgramLabel.Text = dateProgramPicker.Value.DayOfWeek.ToString();


            controllerSettings = new ControllerSettings();
            
            simpleText = itemTypeTabControl.TabPages["textItemTab"];
            itemTypeTabControl.TabPages.RemoveByKey("textItemTab");
            allItemsTabs = new TabPage[itemTypeTabControl.TabPages.Count];
            for (int i = 0; i < settingsTabControl.TabPages.Count; i++)
            {
                allSettingsTabs[i] = settingsTabControl.TabPages[i];
            }

            for (int i = 0; i < itemTypeTabControl.TabPages.Count; i++)
            {
                allItemsTabs[i] = itemTypeTabControl.TabPages[i];
            }
            
            UserLevel = ApplicationUserLevel.Advanced;
            //expertLevelCheckBox.Checked = true;

            //UserLevel = ApplicationUserLevel.Beginner;
            //beginnerLevelCheckBox.Checked = true;


            //UserLevel = ApplicationUserLevel.Advanced;
            advancedLevelCheckBox.Checked = true;

            try
            {
                // advanced is checked, so program should load autosave.apr, not .bpr
                System.IO.Stream tmp = File.Open(NotBeginnerAutosaveFileName, FileMode.Open);
                testPlaybill = Playbill.LoadProgramsFromFile(tmp);
                tmp.Close();
            }
            catch
            {
                testPlaybill = new Playbill();
            }

            SetupTree();

            backgroundImage = this.BackgroundImage;

            beginnerBackgroundImage = new Bitmap(backgroundImage.Width, backgroundImage.Width - playbillTree.Width);
            using (Graphics bgGraphics = Graphics.FromImage(beginnerBackgroundImage))
            {
                bgGraphics.DrawImageUnscaledAndClipped(backgroundImage,
                    new Rectangle(-playbillTree.Width, 0, backgroundImage.Width, backgroundImage.Height));
            }


            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            MakeTransparentControls(this);

            bool tester = false;
            if (ConfigHashtable.ContainsKey("TesterPassword"))
            {
                using (MD5 md5Hash = MD5.Create())
                {
                    string hash = "6f4f83fddde73ce39df75b85df1e2a52"; //GetMd5Hash(md5Hash, ConfigHashtable["TesterPassword"].ToString());

                    if (VerifyMd5Hash(md5Hash, ConfigHashtable["TesterPassword"].ToString(), hash))
                        tester = true;
                }
            }

            //if (!ConfigHashtable.ContainsKey("TesterPassword") || ConfigHashtable["TesterPassword"].ToString() != "thebestledcompany")
            if (!tester)
            {
                MessageBox.Show("This is test version of the program. You use it on your own risk.", "LedConfig TEST VERSION");

                if (testVersionTimer != null)
                {
                    testVersionTimer.Stop();
                    testVersionTimer.Dispose();
                    testVersionTimer = null;
                }

                testVersionTimer = new System.Windows.Forms.Timer();
                testVersionTimer.Interval = 60000;
                testVersionTimer.Tick += new EventHandler(testVersionTimer_Tick);
                testVersionTimer.Start();
            }

            LoadEffects();

            

            

            try
            {
                Image logoFromFile = Image.FromFile("logo.jpg");
                logoPictureBox.Image = logoFromFile;
            }
            catch (FileNotFoundException)
            {
            }

            previewWindow.ChangeSize(CurrentPlaybill.Width, CurrentPlaybill.Height);
            previewWindow.Show();

            System.Drawing.Text.InstalledFontCollection fonts = new System.Drawing.Text.InstalledFontCollection();
            foreach (FontFamily font in fonts.Families)
                bitmaptextFontComboBox.Items.Add(font.Name);

            MainWindowButtonInitialization();

            MainWindowDateTimeControlsEnable(!Convert.ToBoolean(ConfigHashtable["AutoSendTime"].ToString()));


            if (Convert.ToBoolean(ConfigHashtable["AutoSendTime"].ToString()) && Communication.Type != CommunicationType.None)
                automaticallySetTime();

            logWindow.Hide();

           
            changingLevelAtBeginning = false;



            DateTime loadEnd = DateTime.Now;

            Console.WriteLine("Loading program: {0}s; From start to search start: {1}s; Searching: {2}s",
                        ((((TimeSpan)(loadEnd - programStart)).TotalMilliseconds) / 1000).ToString(),
                        ((((TimeSpan)(searchStart - programStart)).TotalMilliseconds) / 1000).ToString(),
                        ((((TimeSpan)(searchEnd - searchStart)).TotalMilliseconds) / 1000).ToString());
        }

        private bool IsReleaseInvalid()
        {
            var now = DateTime.Now;
            var assembly = GetCreationDate().AddDays(30);

            return now.Ticks >= assembly.Ticks;
        }

        private DateTime GetCreationDate()
        {
            return File.GetCreationTime(Assembly.GetEntryAssembly().Location);
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            if (IsReleaseInvalid())
            {
                this.Close();
                Application.Exit();
                return;
            }
        }

        private void SetupDateTimeFormats()
        {
            CultureInfo ci = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            DateTime now = DateTime.Now;
            dateTimeFormatComboBox.Items.Clear();
            
            ////0 = chinese
            ////1 = Day,DD/MM/YYYY HH:MM
            //dateTimeFormatComboBox.Items.Add(now.ToString("ddd, dd/MM/yyyy HH:mm:ss"));
            ////2 = YYY-MM-DD Day. HH:MM
            //dateTimeFormatComboBox.Items.Add(now.ToString("yyyy-MM-dd ddd HH:mm:ss"));
            ////3 = Daylong, DD Month YYYY HH:MM
            //dateTimeFormatComboBox.Items.Add(now.ToString("dddd, dd MMMM yyyy HH:mm:ss"));
            ////4 = Day, Mon DD, YYYY HH:MM
            //dateTimeFormatComboBox.Items.Add(now.ToString("ddd, MMM dd, yyyy HH:mm:ss"));
            ////5 = Daylong,  Month DD,YYYY HH:MM
            //dateTimeFormatComboBox.Items.Add(now.ToString("dddd, MMMM dd, yyyy HH:mm:ss"));
            ////6 = Day,M/DD/YYYY HH:MM
            //dateTimeFormatComboBox.Items.Add(now.ToString("ddd, M/dd/yyyy HH:mm:ss"));
            ////7 = YYYY/MM/DD, Day. HH:MM
            //dateTimeFormatComboBox.Items.Add(now.ToString("yyyy/MM/dd, ddd HH:mm:ss"));

            //0 = chinese
            //1 = Day,DD/MM/YYYY HH:MM
            dateTimeFormatComboBox.Items.Add(now.ToString("ddd,dd/MM/yyyy HH:mm:ss"));
            //2 = YYY-MM-DD Day. HH:MM
            dateTimeFormatComboBox.Items.Add(now.ToString("yyyy-MM-dd ddd. HH:mm:ss"));
            //3 = Day, DD Mon YYYY HH:MM
            dateTimeFormatComboBox.Items.Add(now.ToString("ddd,dd MMM yyyy HH:mm:ss"));
            //4 = Day, Mon DD, YYYY HH:MM
            dateTimeFormatComboBox.Items.Add(now.ToString("ddd,MMM dd,yyyy HH:mm:ss"));
            //5 = Day, Mon DD,YYYY HH:MM
            dateTimeFormatComboBox.Items.Add(now.ToString("ddd,MMM dd,yyyy HH:mm:ss"));
            //6 = Day, M/DD/YYYY HH:MM
            dateTimeFormatComboBox.Items.Add(now.ToString("ddd,M/dd/yyyy HH:mm:ss"));
            //7 = YYYY/MM/DD, Day. HH:MM
            dateTimeFormatComboBox.Items.Add(now.ToString("yyyy/MM/dd ddd. HH:mm:ss"));

            //dateTimeFormatComboBox.SelectedItem = dateTimeFormatComboBox.Items[0];

            Thread.CurrentThread.CurrentCulture = ci;
        }

        public static string GetMd5Hash(MD5 md5Hash, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        // Verify a hash against a string.
        public static bool VerifyMd5Hash(MD5 md5Hash, string input, string hash)
        {
            // Hash the input.
            string hashOfInput = GetMd5Hash(md5Hash, input);

            // Create a StringComparer an compare the hashes.
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;

            if (0 == comparer.Compare(hashOfInput, hash))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void playbillColorInitialization()
        {
            if (CurrentPlaybill == null) // this should be called only if the playbill exists
                return;

            LoadConfiguration();

            bitmaptextFontColorComboBox.Items.Clear();
            bitmaptextBackgroundComboBox.Items.Clear();
            clockFontColorComboBox.Items.Clear();
            temperatureFontColorComboBox.Items.Clear();

            // add black to text font and background color
            bitmaptextFontColorComboBox.Items.Add("Black");
            bitmaptextBackgroundComboBox.Items.Add("Black"); // "Black"

            switch (CurrentPlaybill.Color)
            {
                case 1: // MONO - Red
                    foreach (object color in allcolours)
                        if (color.Equals((object)"Red")) // "Red"
                        {
                            bitmaptextFontColorComboBox.Items.Add(color);
                            bitmaptextBackgroundComboBox.Items.Add(color);
                            clockFontColorComboBox.Items.Add(color);
                            temperatureFontColorComboBox.Items.Add(color);
                        }
                    break;
                case 2: // MONO - Green (Lime)
                    foreach (object color in allcolours)
                        if (color.Equals((object)"Lime")) // "Lime"
                        {
                            bitmaptextFontColorComboBox.Items.Add(color);
                            bitmaptextBackgroundComboBox.Items.Add(color);
                            clockFontColorComboBox.Items.Add(color);
                            temperatureFontColorComboBox.Items.Add(color);
                        }
                    break;
                case 3: // RG - Red, Green (Lime), Yellow
                    foreach (object color in allcolours)
                        if (color.Equals((object)"Red") || color.Equals((object)"Lime") || color.Equals((object)"Yellow")) // Red, Lime, Yellow
                        {
                            bitmaptextFontColorComboBox.Items.Add(color);
                            bitmaptextBackgroundComboBox.Items.Add(color);
                            clockFontColorComboBox.Items.Add(color);
                            temperatureFontColorComboBox.Items.Add(color);
                        }
                    break;
                case 4: // MONO - Blue
                    foreach (object color in allcolours)
                        if (color.Equals((object)"Blue")) // Blue
                        {
                            bitmaptextFontColorComboBox.Items.Add(color);
                            bitmaptextBackgroundComboBox.Items.Add(color);
                            clockFontColorComboBox.Items.Add(color);
                            temperatureFontColorComboBox.Items.Add(color);
                        }
                    break;
                case 5: // RB - Red, Blue, Magenta
                    foreach (object color in allcolours)
                        if (color.Equals((object)"Red") || color.Equals((object)"Blue") || color.Equals((object)"Magenta")) // Red, Blue, Magenta
                        {
                            bitmaptextFontColorComboBox.Items.Add(color);
                            bitmaptextBackgroundComboBox.Items.Add(color);
                            clockFontColorComboBox.Items.Add(color);
                            temperatureFontColorComboBox.Items.Add(color);
                        }
                    break;
                case 6: // GB - Green (Lime), Blue, Aqua
                    foreach (object color in allcolours)
                        if (color.Equals((object)"Line") || color.Equals((object)"Blue") || color.Equals((object)"Aqua")) // Lime, Blue, Aqua
                        {
                            bitmaptextFontColorComboBox.Items.Add(color);
                            bitmaptextBackgroundComboBox.Items.Add(color);
                            clockFontColorComboBox.Items.Add(color);
                            temperatureFontColorComboBox.Items.Add(color);
                        }
                    break;
                case 7: // RGB - Red, Green (Lime), Yellow, Blue, Magenta, Aqua, White
                    foreach (object color in allcolours)
                        if (color.Equals((object)"Red") || color.Equals((object)"Lime") || color.Equals((object)"Yellow") || // Red, Lime, Yellow
                            color.Equals((object)"Blue") || color.Equals((object)"Magenta") || color.Equals((object)"Aqua") || color.Equals((object)"White")) // Blue, Magenta, Aqua, White
                        {
                            bitmaptextFontColorComboBox.Items.Add(color);
                            bitmaptextBackgroundComboBox.Items.Add(color);
                            clockFontColorComboBox.Items.Add(color);
                            temperatureFontColorComboBox.Items.Add(color);
                        }
                    break;
                case 0x77: // full color should support every color - there should be possibility to choose a different color
                    foreach (object color in allcolours)
                    {
                        bitmaptextFontColorComboBox.Items.Add(color);
                        bitmaptextBackgroundComboBox.Items.Add(color);
                        clockFontColorComboBox.Items.Add(color);
                        temperatureFontColorComboBox.Items.Add(color);
                    }
                    break;
            }

            bitmapTextSelectedFontColor = 1;
            bitmapTextSelectedBackgroundColor = 0;
            temperatureFontColor = 0;
            clockFontColor = 0;

            ConfigurationColorCheck();
        }

        private void MainWindowButtonInitialization()
        {
            bitmaptextBoldButton.FlatAppearance.BorderColor = Color.Gray;
            bitmaptextItalicButton.FlatAppearance.BorderColor = Color.Gray;
            bitmaptextUnderlineButton.FlatAppearance.BorderColor = Color.Gray;

            bitmaptextCenterButton.FlatAppearance.BorderColor = Color.Gray;
            bitmaptextLeftButton.FlatAppearance.BorderColor = Color.Gray;
            bitmaptextRightButton.FlatAppearance.BorderColor = Color.Gray;
        }

        public void MainWindowDateTimeControlsEnable(bool state)
        {
            currentProgramTimeCheckBox.Checked = state;
            DateTimeProgramGroupBox.Enabled = state;

            currentClockTimeCheckBox.Checked = state;
            dateTimeClockGroupBox.Enabled = state;
        }

        public void GenerateFontsHashTables()
        {
            #region Mark 6x8 font
            #region ' '
            mark8fontHashTable.Add(32, new bool[,] {
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region ,
            mark8fontHashTable.Add(44, new bool[,] {
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ false, false, true, false, false, false },
	{ false, false, true, false, false, false },
	{ false, true, false, false, false, false }});
            #endregion
            #region °
            mark8fontHashTable.Add(176, new bool[,] {
	{ false, true, true, true, false, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ false, true, true, true, false, false },
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region .
            mark8fontHashTable.Add(46, new bool[,] {
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ false, true, true, false, false, false },
	{ false, true, true, false, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region :
            mark8fontHashTable.Add(58, new bool[,] {
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ false, false, true, false, false, false },
	{ false, false, true, false, false, false },
	{ false, false, false, false, false, false },
	{ false, false, true, false, false, false },
	{ false, false, true, false, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region /
            mark8fontHashTable.Add(47, new bool[,] {
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, true },
	{ false, false, false, false, true, false },
	{ false, false, false, true, false, false },
	{ false, false, true, false, false, false },
	{ false, true, false, false, false, false },
	{ true, false, false, false, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region -
            mark8fontHashTable.Add(45, new bool[,] {
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ true, true, true, true, true, false },
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region 0
            mark8fontHashTable.Add(48, new bool[,] {
	{ false, true, true, true, false, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ false, true, true, true, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region 1
            mark8fontHashTable.Add(49, new bool[,] {
	{ false, false, true, false, false, false },
	{ false, true, true, false, false, false },
	{ false, false, true, false, false, false },
	{ false, false, true, false, false, false },
	{ false, false, true, false, false, false },
	{ false, false, true, false, false, false },
	{ false, true, true, true, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region 2
            mark8fontHashTable.Add(50, new bool[,] {
	{ false, true, true, true, false, false },
	{ true, false, false, false, true, false },
	{ false, false, false, true, false, false },
	{ false, false, true, false, false, false },
	{ false, true, false, false, false, false },
	{ true, false, false, false, true, false },
	{ true, true, true, true, true, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region 3
            mark8fontHashTable.Add(51, new bool[,] {
	{ false, true, true, true, false, false },
	{ true, false, false, false, true, false },
	{ false, false, false, false, true, false },
	{ false, true, true, true, false, false },
	{ false, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ false, true, true, true, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region 4
            mark8fontHashTable.Add(52, new bool[,] {
	{ false, false, false, true, false, false },
	{ false, false, true, true, false, false },
	{ false, true, false, true, false, false },
	{ true, false, false, true, false, false },
	{ true, true, true, true, true, false },
	{ false, false, false, true, false, false },
	{ false, false, true, true, true, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region 5
            mark8fontHashTable.Add(53, new bool[,] {
	{ true, true, true, true, true, false },
	{ true, false, false, false, false, false },
	{ true, true, true, true, false, false },
	{ true, false, false, false, true, false },
	{ false, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ false, true, true, true, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region 6
            mark8fontHashTable.Add(54, new bool[,] {
	{ false, true, true, true, false, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, false, false },
	{ true, true, true, true, false, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ false, true, true, true, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region 7
            mark8fontHashTable.Add(55, new bool[,] {
	{ true, true, true, true, true, false },
	{ true, false, false, false, true, false },
	{ false, false, false, false, true, false },
	{ false, false, false, true, false, false },
	{ false, false, false, true, false, false },
	{ false, false, true, false, false, false },
	{ false, false, true, false, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region 8
            mark8fontHashTable.Add(56, new bool[,] {
	{ false, true, true, true, false, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ false, true, true, true, false, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ false, true, true, true, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region 9
            mark8fontHashTable.Add(57, new bool[,] {
	{ false, true, true, true, false, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ false, true, true, true, true, false },
	{ false, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ false, true, true, true, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region A
            mark8fontHashTable.Add(65, new bool[,] {
	{ false, false, true, false, false, false },
	{ false, true, false, true, false, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ true, true, true, true, true, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region C
            mark8fontHashTable.Add(67, new bool[,] {
	{ false, true, true, true, false, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, false, false },
	{ true, false, false, false, false, false },
	{ true, false, false, false, false, false },
	{ true, false, false, false, true, false },
	{ false, true, true, true, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region D
            mark8fontHashTable.Add(68, new bool[,] {
	{ true, true, true, true, false, false },
	{ false, true, false, false, true, false },
	{ false, true, false, false, true, false },
	{ false, true, false, false, true, false },
	{ false, true, false, false, true, false },
	{ false, true, false, false, true, false },
	{ true, true, true, true, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region F
            mark8fontHashTable.Add(70, new bool[,] {
	{ true, true, true, true, true, false },
	{ false, true, false, false, false, false },
	{ false, true, false, false, false, false },
	{ false, true, true, true, false, false },
	{ false, true, false, false, false, false },
	{ false, true, false, false, false, false },
	{ true, true, true, false, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region J
            mark8fontHashTable.Add(74, new bool[,] {
	{ false, false, true, true, true, false },
	{ false, false, false, true, false, false },
	{ false, false, false, true, false, false },
	{ false, false, false, true, false, false },
	{ true, false, false, true, false, false },
	{ true, false, false, true, false, false },
	{ false, true, true, false, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region M
            mark8fontHashTable.Add(77, new bool[,] {
	{ true, false, false, false, true, false },
	{ true, true, false, true, true, false },
	{ true, false, true, false, true, false },
	{ true, false, true, false, true, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region N
            mark8fontHashTable.Add(78, new bool[,] {
	{ true, false, false, false, true, false },
	{ true, true, false, false, true, false },
	{ true, false, true, false, true, false },
	{ true, false, true, false, true, false },
	{ true, false, false, true, true, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region O
            mark8fontHashTable.Add(79, new bool[,] {
	{ false, true, true, true, false, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ false, true, true, true, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region S
            mark8fontHashTable.Add(83, new bool[,] {
	{ false, true, true, true, false, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, false, false },
	{ false, true, true, true, false, false },
	{ false, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ false, true, true, true, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region T
            mark8fontHashTable.Add(84, new bool[,] {
	{ true, true, true, true, true, false },
	{ true, false, true, false, true, false },
	{ false, false, true, false, false, false },
	{ false, false, true, false, false, false },
	{ false, false, true, false, false, false },
	{ false, false, true, false, false, false },
	{ false, true, true, true, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region W
            mark8fontHashTable.Add(87, new bool[,] {
	{ true, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ true, false, true, false, true, false },
	{ true, false, true, false, true, false },
	{ true, true, false, true, true, false },
	{ true, false, false, false, true, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region a
            mark8fontHashTable.Add(97, new bool[,] {
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ false, true, true, false, false, false },
	{ false, false, false, true, false, false },
	{ false, true, true, true, false, false },
	{ true, false, false, true, false, false },
	{ false, true, true, false, true, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region b
            mark8fontHashTable.Add(98, new bool[,] {
	{ true, true, false, false, false, false },
	{ false, true, false, false, false, false },
	{ false, true, true, true, false, false },
	{ false, true, false, false, true, false },
	{ false, true, false, false, true, false },
	{ false, true, false, false, true, false },
	{ true, false, true, true, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region c
            mark8fontHashTable.Add(99, new bool[,] {
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ false, true, true, true, false, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, false, false },
	{ true, false, false, false, true, false },
	{ false, true, true, true, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region d
            mark8fontHashTable.Add(100, new bool[,] {
	{ false, false, true, true, false, false },
	{ false, false, false, true, false, false },
	{ false, true, true, true, false, false },
	{ true, false, false, true, false, false },
	{ true, false, false, true, false, false },
	{ true, false, false, true, false, false },
	{ false, true, true, false, true, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region e
            mark8fontHashTable.Add(101, new bool[,] {
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ false, true, true, false, false, false },
	{ true, false, false, true, false, false },
	{ true, true, true, true, false, false },
	{ true, false, false, false, false, false },
	{ false, true, true, true, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region g
            mark8fontHashTable.Add(103, new bool[,] {
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ false, true, true, true, true, false },
	{ true, false, false, true, false, false },
	{ true, false, false, true, false, false },
	{ false, true, true, true, false, false },
	{ false, false, false, true, false, false },
	{ false, true, true, false, false, false }});
            #endregion
            #region h
            mark8fontHashTable.Add(104, new bool[,] {
	{ true, true, false, false, false, false },
	{ false, true, false, false, false, false },
	{ false, true, true, true, false, false },
	{ false, true, false, false, true, false },
	{ false, true, false, false, true, false },
	{ false, true, false, false, true, false },
	{ false, true, false, false, true, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region i
            mark8fontHashTable.Add(105, new bool[,] {
	{ false, false, true, false, false, false },
	{ false, false, false, false, false, false },
	{ false, true, true, false, false, false },
	{ false, false, true, false, false, false },
	{ false, false, true, false, false, false },
	{ false, false, true, false, false, false },
	{ false, true, true, true, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region l
            mark8fontHashTable.Add(108, new bool[,] {
	{ false, true, true, false, false, false },
	{ false, false, true, false, false, false },
	{ false, false, true, false, false, false },
	{ false, false, true, false, false, false },
	{ false, false, true, false, false, false },
	{ false, false, true, false, false, false },
	{ false, true, true, true, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region n
            mark8fontHashTable.Add(110, new bool[,] {
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ true, false, true, true, false, false },
	{ false, true, false, false, true, false },
	{ false, true, false, false, true, false },
	{ false, true, false, false, true, false },
	{ false, true, false, false, true, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region o
            mark8fontHashTable.Add(111, new bool[,] {
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ false, true, true, true, false, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ false, true, true, true, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region p
            mark8fontHashTable.Add(112, new bool[,] {
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ true, true, true, true, false, false },
	{ false, true, false, false, true, false },
	{ false, true, false, false, true, false },
	{ false, true, true, true, false, false },
	{ false, true, false, false, false, false },
	{ true, true, true, false, false, false }});
            #endregion
            #region r
            mark8fontHashTable.Add(114, new bool[,] {
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ true, false, true, true, false, false },
	{ false, true, false, false, true, false },
	{ false, true, false, false, false, false },
	{ false, true, false, false, false, false },
	{ true, true, true, false, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region t
            mark8fontHashTable.Add(116, new bool[,] {
	{ false, true, false, false, false, false },
	{ false, true, false, false, false, false },
	{ true, true, true, false, false, false },
	{ false, true, false, false, false, false },
	{ false, true, false, false, false, false },
	{ false, true, false, false, true, false },
	{ false, false, true, true, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region u
            mark8fontHashTable.Add(117, new bool[,] {
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ true, false, false, true, false, false },
	{ true, false, false, true, false, false },
	{ true, false, false, true, false, false },
	{ true, false, false, true, false, false },
	{ false, true, true, false, true, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region v
            mark8fontHashTable.Add(118, new bool[,] {
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ false, true, false, true, false, false },
	{ false, false, true, false, false, false },
	{ false, false, false, false, false, false }});
            #endregion
            #region y
            mark8fontHashTable.Add(121, new bool[,] {
	{ false, false, false, false, false, false },
	{ false, false, false, false, false, false },
	{ true, false, false, false, true, false },
	{ true, false, false, false, true, false },
	{ false, true, false, true, false, false },
	{ false, false, true, false, false, false },
	{ false, false, true, false, false, false },
	{ true, true, false, false, false, false }});
            #endregion



            #endregion

            #region Mark8x12font
            #region ' '
            mark12fontHashTable.Add(32, new bool[,]{ 

	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '!'
            mark12fontHashTable.Add(33, new bool[,]{ 
	{ false, false, true, false, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '"'
            mark12fontHashTable.Add(34, new bool[,]{ 

	{ false, false, true, false, false, true, false, false },
	{ false, false, true, false, false, true, false, false },
	{ false, false, true, false, false, true, false, false },
	{ false, false, true, false, false, true, false, false },
	{ false, false, true, false, false, true, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '#'
            mark12fontHashTable.Add(35, new bool[,]{ 
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, true, false, false, true, false, false },
	{ false, false, true, false, false, true, false, false },
	{ false, true, true, true, true, true, true, false },
	{ false, false, true, false, false, true, false, false },
	{ false, false, true, false, false, true, false, false },
	{ false, true, true, true, true, true, true, false },
	{ false, false, true, false, false, true, false, false },
	{ false, false, true, false, false, true, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '$'
            mark12fontHashTable.Add(36, new bool[,]{ 

	{ false, false, false, true, false, false, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, true, false, true, false, true, false, false },
	{ true, false, false, true, false, false, true, false },
	{ false, true, false, true, false, false, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, false, false, true, false, true, false, false },
	{ true, false, false, true, false, false, true, false },
	{ false, true, false, true, false, true, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '%'
            mark12fontHashTable.Add(37, new bool[,]{ 
	{ false, false, false, false, false, false, false, false },
	{ false, true, false, false, false, false, true, false },
	{ true, false, true, false, false, false, true, false },
	{ true, false, true, false, false, true, false, false },
	{ false, true, false, false, true, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, true, false, false, true, false, false },
	{ false, true, false, false, true, false, true, false },
	{ true, false, false, false, true, false, true, false },
	{ true, false, false, false, false, true, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '&'
            mark12fontHashTable.Add(38, new bool[,]{ 

	{ false, false, true, true, true, false, false, false },
	{ false, true, false, false, false, true, false, false },
	{ true, false, false, false, false, true, false, false },
	{ false, true, false, false, true, false, false, false },
	{ false, false, true, true, false, false, false, false },
	{ false, false, true, true, false, false, false, false },
	{ false, true, false, false, true, false, true, false },
	{ true, false, false, false, false, true, false, false },
	{ true, false, false, false, true, false, true, false },
	{ false, true, false, true, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '''
            mark12fontHashTable.Add(39, new bool[,]{ 
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '('
            mark12fontHashTable.Add(40, new bool[,]{ 

	{ false, false, false, false, true, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, false, true, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region ')'
            mark12fontHashTable.Add(41, new bool[,]{ 
	{ false, false, true, false, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, false, true, false, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, true, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '*'
            mark12fontHashTable.Add(42, new bool[,]{ 

	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, false, false },
	{ false, false, true, false, true, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ true, true, true, true, true, true, true, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, true, false, true, false, false, false },
	{ false, true, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '+'
            mark12fontHashTable.Add(43, new bool[,]{ 
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ true, true, true, true, true, true, true, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region ','
            mark12fontHashTable.Add(44, new bool[,]{ 

	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '-'
            mark12fontHashTable.Add(45, new bool[,]{ 
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ true, true, true, true, true, true, true, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '.'
            mark12fontHashTable.Add(46, new bool[,]{ 

	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false },
	{ false, false, true, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '/'
            mark12fontHashTable.Add(47, new bool[,]{ 
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, true, false, false, false },
	{ false, false, false, false, true, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '0'
            mark12fontHashTable.Add(48, new bool[,]{ 

	{ false, false, true, true, true, false, false, false },
	{ false, true, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, true, true, false },
	{ true, false, false, false, true, false, true, false },
	{ true, false, false, true, false, false, true, false },
	{ true, false, true, false, false, false, true, false },
	{ true, true, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '1'
            mark12fontHashTable.Add(49, new bool[,]{ 
	{ false, false, false, true, false, false, false, false },
	{ false, false, true, true, false, false, false, false },
	{ false, true, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, true, true, true, true, true, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '2'
            mark12fontHashTable.Add(50, new bool[,]{ 

	{ false, false, true, true, true, false, false, false },
	{ false, true, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ false, false, false, false, false, false, true, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, true, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, true, true, true, true, true, true, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '3'
            mark12fontHashTable.Add(51, new bool[,]{ 
	{ false, false, true, true, true, false, false, false },
	{ false, true, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ false, false, false, false, false, false, true, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, true, true, false, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '4'
            mark12fontHashTable.Add(52, new bool[,]{ 

	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, true, true, false, false },
	{ false, false, false, true, false, true, false, false },
	{ false, false, true, false, false, true, false, false },
	{ false, true, false, false, false, true, false, false },
	{ true, false, false, false, false, true, false, false },
	{ true, false, false, false, false, true, false, false },
	{ true, true, true, true, true, true, true, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, true, true, true, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '5'
            mark12fontHashTable.Add(53, new bool[,]{ 
	{ true, true, true, true, true, true, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, true, true, true, false, false, false },
	{ true, true, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ false, false, false, false, false, false, true, false },
	{ false, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '6'
            mark12fontHashTable.Add(54, new bool[,]{ 

	{ false, false, true, true, true, false, false, false },
	{ false, true, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, true, true, true, false, false, false },
	{ true, true, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '7'
            mark12fontHashTable.Add(55, new bool[,]{ 
	{ true, true, true, true, true, true, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, false, false, false, false, false, true, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, true, false, false, false },
	{ false, false, false, false, true, false, false, false },
	{ false, false, false, false, true, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '8'
            mark12fontHashTable.Add(56, new bool[,]{ 

	{ false, false, true, true, true, false, false, false },
	{ false, true, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, true, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '9'
            mark12fontHashTable.Add(57, new bool[,]{ 
	{ false, false, true, true, true, false, false, false },
	{ false, true, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, true, false },
	{ false, false, true, true, true, false, true, false },
	{ false, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region ':'
            mark12fontHashTable.Add(58, new bool[,]{ 

	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region ';'
            mark12fontHashTable.Add(59, new bool[,]{ 
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '<'
            mark12fontHashTable.Add(60, new bool[,]{ 

	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, true, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, false, true, false, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '='
            mark12fontHashTable.Add(61, new bool[,]{ 
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ true, true, true, true, true, true, true, false },
	{ false, false, false, false, false, false, false, false },
	{ true, true, true, true, true, true, true, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '>'
            mark12fontHashTable.Add(62, new bool[,]{ 

	{ true, false, false, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, false, true, false, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, true, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '?'
            mark12fontHashTable.Add(63, new bool[,]{ 
	{ false, false, true, true, true, false, false, false },
	{ false, true, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, true, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '@'
            mark12fontHashTable.Add(64, new bool[,]{ 

	{ false, false, true, true, true, false, false, false },
	{ false, true, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, true, true, false, true, false },
	{ true, false, true, false, false, false, true, false },
	{ true, false, true, false, false, true, false, false },
	{ true, false, false, true, true, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'A'
            mark12fontHashTable.Add(65, new bool[,]{ 
	{ false, false, false, true, false, false, false, false },
	{ false, false, true, false, true, false, false, false },
	{ false, true, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, true, true, true, true, true, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'B'
            mark12fontHashTable.Add(66, new bool[,]{ 

	{ true, true, true, true, true, false, false, false },
	{ true, false, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, true, false, false },
	{ true, true, true, true, true, false, false, false },
	{ true, false, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, true, false, false },
	{ true, true, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'C'
            mark12fontHashTable.Add(67, new bool[,]{ 
	{ false, false, true, true, true, false, false, false },
	{ false, true, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'D'
            mark12fontHashTable.Add(68, new bool[,]{ 

	{ true, true, true, true, true, false, false, false },
	{ true, false, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, true, false, false },
	{ true, true, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'E'
            mark12fontHashTable.Add(69, new bool[,]{ 
	{ true, true, true, true, true, true, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, true, false, false, false },
	{ true, true, true, true, true, false, false, false },
	{ true, false, false, false, true, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, true, true, true, true, true, true, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'F'
            mark12fontHashTable.Add(70, new bool[,]{ 

	{ true, true, true, true, true, true, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, true, false, false, false },
	{ true, true, true, true, true, false, false, false },
	{ true, false, false, false, true, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'G'
            mark12fontHashTable.Add(71, new bool[,]{ 
	{ false, false, true, true, true, false, false, false },
	{ false, true, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, true, true, true, true, false },
	{ true, false, false, true, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'H'
            mark12fontHashTable.Add(72, new bool[,]{ 

	{ true, false, false, false, false, true, false, false },
	{ true, false, false, false, false, true, false, false },
	{ true, false, false, false, false, true, false, false },
	{ true, false, false, false, false, true, false, false },
	{ true, false, false, false, false, true, false, false },
	{ true, true, true, true, true, true, false, false },
	{ true, false, false, false, false, true, false, false },
	{ true, false, false, false, false, true, false, false },
	{ true, false, false, false, false, true, false, false },
	{ true, false, false, false, false, true, false, false },
	{ true, false, false, false, false, true, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'I'
            mark12fontHashTable.Add(73, new bool[,]{ 
	{ false, false, true, true, true, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'J'
            mark12fontHashTable.Add(74, new bool[,]{ 

	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ true, false, false, false, false, true, false, false },
	{ false, true, false, false, true, false, false, false },
	{ false, false, true, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'K'
            mark12fontHashTable.Add(75, new bool[,]{ 
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, true, false, false },
	{ true, false, false, false, true, false, false, false },
	{ true, false, false, true, false, false, false, false },
	{ true, false, true, false, false, false, false, false },
	{ true, true, false, false, false, false, false, false },
	{ true, false, true, false, false, false, false, false },
	{ true, false, false, true, false, false, false, false },
	{ true, false, false, false, true, false, false, false },
	{ true, false, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'L'
            mark12fontHashTable.Add(76, new bool[,]{ 

	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, true, true, true, true, true, true, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'M'
            mark12fontHashTable.Add(77, new bool[,]{ 
	{ true, false, false, false, false, false, true, false },
	{ true, true, false, false, false, true, true, false },
	{ true, true, false, false, false, true, true, false },
	{ true, false, true, false, true, false, true, false },
	{ true, false, true, false, true, false, true, false },
	{ true, false, false, true, false, false, true, false },
	{ true, false, false, true, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'N'
            mark12fontHashTable.Add(78, new bool[,]{ 

	{ true, false, false, false, false, false, true, false },
	{ true, true, false, false, false, false, true, false },
	{ true, true, false, false, false, false, true, false },
	{ true, false, true, false, false, false, true, false },
	{ true, false, true, false, false, false, true, false },
	{ true, false, false, true, false, false, true, false },
	{ true, false, false, false, true, false, true, false },
	{ true, false, false, false, true, false, true, false },
	{ true, false, false, false, false, true, true, false },
	{ true, false, false, false, false, true, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'O'
            mark12fontHashTable.Add(79, new bool[,]{ 
	{ false, false, true, true, true, false, false, false },
	{ false, true, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'P'
            mark12fontHashTable.Add(80, new bool[,]{ 

	{ true, true, true, true, true, false, false, false },
	{ true, false, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, true, false, false },
	{ true, true, true, true, true, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'Q'
            mark12fontHashTable.Add(81, new bool[,]{ 
	{ false, false, true, true, true, false, false, false },
	{ false, true, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, true, false, false, true, false },
	{ true, false, false, false, true, false, true, false },
	{ false, true, false, false, false, true, false, false },
	{ false, false, true, true, true, false, true, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'R'
            mark12fontHashTable.Add(82, new bool[,]{ 

	{ true, true, true, true, true, false, false, false },
	{ true, false, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, true, false, false },
	{ true, true, true, true, true, false, false, false },
	{ true, false, false, true, false, false, false, false },
	{ true, false, false, false, true, false, false, false },
	{ true, false, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'S'
            mark12fontHashTable.Add(83, new bool[,]{ 
	{ false, false, true, true, true, false, false, false },
	{ false, true, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'T'
            mark12fontHashTable.Add(84, new bool[,]{ 

	{ true, true, true, true, true, true, true, false },
	{ true, false, false, true, false, false, true, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'U'
            mark12fontHashTable.Add(85, new bool[,]{ 
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'V'
            mark12fontHashTable.Add(86, new bool[,]{ 

	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, false, false },
	{ false, true, false, false, false, true, false, false },
	{ false, false, true, false, true, false, false, false },
	{ false, false, true, false, true, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'W'
            mark12fontHashTable.Add(87, new bool[,]{ 
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, true, false, false, true, false },
	{ true, false, false, true, false, false, true, false },
	{ true, false, true, false, true, false, true, false },
	{ true, false, true, false, true, false, true, false },
	{ true, true, false, false, false, true, true, false },
	{ true, true, false, false, false, true, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'X'
            mark12fontHashTable.Add(88, new bool[,]{ 

	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, false, false },
	{ false, true, false, false, false, true, false, false },
	{ false, false, true, false, true, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, true, false, true, false, false, false },
	{ false, true, false, false, false, true, false, false },
	{ false, true, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'Y'
            mark12fontHashTable.Add(89, new bool[,]{ 
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, false, false },
	{ false, false, true, false, true, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'Z'
            mark12fontHashTable.Add(90, new bool[,]{ 

	{ true, true, true, true, true, true, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, false, false, false, false, false, true, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, true, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, true, true, true, true, true, true, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '['
            mark12fontHashTable.Add(91, new bool[,]{ 
	{ false, true, true, true, true, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, true, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '\'
            mark12fontHashTable.Add(92, new bool[,]{ 

	{ true, false, false, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, false, true, false, false, false },
	{ false, false, false, false, true, false, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region ']'
            mark12fontHashTable.Add(93, new bool[,]{ 
	{ false, false, true, true, true, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, true, true, true, true, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '^'
            mark12fontHashTable.Add(94, new bool[,]{ 

	{ false, false, false, false, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, true, false, true, false, false, false },
	{ false, true, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '_'
            mark12fontHashTable.Add(95, new bool[,]{ 
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ true, true, true, true, true, true, true, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '`'
            mark12fontHashTable.Add(96, new bool[,]{ 

	{ true, false, false, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'a'
            mark12fontHashTable.Add(97, new bool[,]{ 
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, true, true, true, false, false, false, false },
	{ true, false, false, false, true, false, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, true, true, true, true, false, false },
	{ false, true, false, false, false, true, false, false },
	{ true, false, false, false, false, true, false, false },
	{ true, false, false, false, false, true, false, false },
	{ false, true, true, true, true, false, true, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'b'
            mark12fontHashTable.Add(98, new bool[,]{ 

	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, true, true, true, false, false, false },
	{ true, true, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, true, false, false, false, true, false, false },
	{ true, false, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'c'
            mark12fontHashTable.Add(99, new bool[,]{ 
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, true, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'd'
            mark12fontHashTable.Add(100, new bool[,]{ 

	{ false, false, false, false, false, false, true, false },
	{ false, false, false, false, false, false, true, false },
	{ false, false, true, true, true, false, true, false },
	{ false, true, false, false, false, true, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, true, false },
	{ false, false, true, true, true, false, true, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'e'
            mark12fontHashTable.Add(101, new bool[,]{ 
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, true, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, true, true, true, true, true, true, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ false, true, false, false, false, false, true, false },
	{ false, false, true, true, true, true, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'f'
            mark12fontHashTable.Add(102, new bool[,]{ 

	{ false, false, false, false, true, false, false, false },
	{ false, false, false, true, false, true, false, false },
	{ false, false, true, false, false, false, true, false },
	{ false, false, true, false, false, false, true, false },
	{ false, false, true, false, false, false, false, false },
	{ false, true, true, true, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, true, true, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'g'
            mark12fontHashTable.Add(103, new bool[,]{ 
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, true, true, true, true, true, false },
	{ false, true, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, true, false },
	{ false, false, true, true, true, false, true, false },
	{ false, false, false, false, false, false, true, false },
	{ false, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, false, false },
	{ false, false, true, true, true, false, false, false }});
            #endregion
            #region 'h'
            mark12fontHashTable.Add(104, new bool[,]{ 

	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, true, true, true, false, false, false },
	{ true, true, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'i'
            mark12fontHashTable.Add(105, new bool[,]{ 
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'j'
            mark12fontHashTable.Add(106, new bool[,]{ 

	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, false, true, false, false },
	{ true, false, false, false, false, true, false, false },
	{ false, true, false, false, true, false, false, false },
	{ false, false, true, true, false, false, false, false }});
            #endregion
            #region 'k'
            mark12fontHashTable.Add(107, new bool[,]{ 
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, true, false, false },
	{ true, false, false, false, true, false, false, false },
	{ true, false, false, true, false, false, false, false },
	{ true, false, true, false, false, false, false, false },
	{ true, true, false, false, false, false, false, false },
	{ true, false, true, false, false, false, false, false },
	{ true, false, false, true, false, false, false, false },
	{ true, false, false, false, true, false, false, false },
	{ true, false, false, false, false, true, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'l'
            mark12fontHashTable.Add(108, new bool[,]{ 

	{ false, false, true, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'm'
            mark12fontHashTable.Add(109, new bool[,]{ 
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ true, true, false, false, false, true, false, false },
	{ true, false, true, false, true, false, true, false },
	{ true, false, false, true, false, false, true, false },
	{ true, false, false, true, false, false, true, false },
	{ true, false, false, true, false, false, true, false },
	{ true, false, false, true, false, false, true, false },
	{ true, false, false, true, false, false, true, false },
	{ true, false, false, true, false, false, true, false },
	{ true, false, false, true, false, false, true, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'n'
            mark12fontHashTable.Add(110, new bool[,]{ 

	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ true, false, true, true, true, false, false, false },
	{ false, true, false, false, false, true, false, false },
	{ false, true, false, false, false, false, true, false },
	{ false, true, false, false, false, false, true, false },
	{ false, true, false, false, false, false, true, false },
	{ false, true, false, false, false, false, true, false },
	{ false, true, false, false, false, false, true, false },
	{ false, true, false, false, false, false, true, false },
	{ false, true, false, false, false, false, true, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'o'
            mark12fontHashTable.Add(111, new bool[,]{ 
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, true, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'p'
            mark12fontHashTable.Add(112, new bool[,]{ 

	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, true, true, true, false, false, false },
	{ true, true, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, true, false, false, false, true, false, false },
	{ true, false, true, true, true, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, false, false }});
            #endregion
            #region 'q'
            mark12fontHashTable.Add(113, new bool[,]{ 
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, true, true, true, true, true, false },
	{ false, true, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, false, true, false },
	{ false, false, true, true, true, true, true, false },
	{ false, false, false, false, false, false, true, false },
	{ false, false, false, false, false, false, true, false },
	{ false, false, false, false, false, false, true, false }});
            #endregion
            #region 'r'
            mark12fontHashTable.Add(114, new bool[,]{ 

	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ true, false, true, true, true, false, false, false },
	{ false, true, false, false, false, true, false, false },
	{ false, true, false, false, false, false, true, false },
	{ false, true, false, false, false, false, true, false },
	{ false, true, false, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 's'
            mark12fontHashTable.Add(115, new bool[,]{ 
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, true, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, false, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 't'
            mark12fontHashTable.Add(116, new bool[,]{ 

	{ false, true, false, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ true, true, true, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, true, false, false, false, false, true, false },
	{ false, false, true, false, false, true, false, false },
	{ false, false, false, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'u'
            mark12fontHashTable.Add(117, new bool[,]{ 
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'v'
            mark12fontHashTable.Add(118, new bool[,]{ 

	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, false, false },
	{ false, true, false, false, false, true, false, false },
	{ false, false, true, false, true, false, false, false },
	{ false, false, true, false, true, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'w'
            mark12fontHashTable.Add(119, new bool[,]{ 
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, true, false, false, true, false },
	{ true, false, true, false, true, false, true, false },
	{ true, false, true, false, true, false, true, false },
	{ true, true, false, false, false, true, true, false },
	{ true, true, false, false, false, true, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'x'
            mark12fontHashTable.Add(120, new bool[,]{ 

	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, false, false },
	{ false, false, true, false, true, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, true, false, true, false, false, false },
	{ false, true, false, false, false, true, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region 'y'
            mark12fontHashTable.Add(121, new bool[,]{ 
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, true, false },
	{ false, false, true, true, true, false, true, false },
	{ false, false, false, false, false, false, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, true, false, false, false, true, false, false },
	{ false, false, true, true, true, false, false, false }});
            #endregion
            #region 'z'
            mark12fontHashTable.Add(122, new bool[,]{ 

	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ true, true, true, true, true, true, true, false },
	{ true, false, false, false, false, false, true, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, true, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ true, false, false, false, false, false, true, false },
	{ true, true, true, true, true, true, true, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '{'
            mark12fontHashTable.Add(123, new bool[,]{ 
	{ false, false, false, false, true, true, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, true, false, false, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, false, true, false, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, false, true, true, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '|'
            mark12fontHashTable.Add(124, new bool[,]{ 

	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '}'
            mark12fontHashTable.Add(125, new bool[,]{ 
	{ false, true, true, false, false, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, false, false, false, true, false, false, false },
	{ false, false, false, false, true, false, false, false },
	{ false, false, false, false, true, false, false, false },
	{ false, false, false, false, false, true, false, false },
	{ false, false, false, false, true, false, false, false },
	{ false, false, false, false, true, false, false, false },
	{ false, false, false, false, true, false, false, false },
	{ false, false, false, true, false, false, false, false },
	{ false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '~'
            mark12fontHashTable.Add(126, new bool[,]{ 

	{ false, true, true, false, false, false, true, false },
	{ true, false, false, true, false, false, true, false },
	{ true, false, false, false, true, true, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #region '°'
            mark12fontHashTable.Add(176, new bool[,]{ 
	            { false, false, true, true, true, false, false, false },
	            { false, true, false, false, false, true, false, false },
	            { true, false, false, false, false, false, true, false },
	            { true, false, false, false, false, false, true, false },
	            { true, false, false, false, false, false, true, false },
	            { false, true, false, false, false, true, false, false },
	            { false, false, true, true, true, false, false, false },
	            { false, false, false, false, false, false, false, false },
	            { false, false, false, false, false, false, false, false },
	            { false, false, false, false, false, false, false, false },
	            { false, false, false, false, false, false, false, false },
	            { false, false, false, false, false, false, false, false }});
            #endregion
            #endregion

            #region MarkBoldfont
            #region ' '
            markBoldfontHashTable.Add(32, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '!'
            markBoldfontHashTable.Add(33, new bool[,]{ 

	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '"'
            markBoldfontHashTable.Add(34, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, true, true, false, false, false },
	{ false, false, true, true, false, false, false, true, true, false, false, false },
	{ false, false, true, true, false, false, false, true, true, false, false, false },
	{ false, false, true, true, false, false, false, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '#'
            markBoldfontHashTable.Add(35, new bool[,]{ 

	{ false, false, false, false, false, false, true, true, false, false, true, true },
	{ false, false, false, false, false, false, true, true, false, false, true, true },
	{ false, false, false, false, false, true, true, false, false, true, true, false },
	{ false, false, true, true, true, true, true, true, true, true, true, true },
	{ false, false, false, false, true, true, false, false, true, true, false, false },
	{ false, false, false, false, true, true, false, false, true, true, false, false },
	{ false, false, false, true, true, false, false, true, true, false, false, false },
	{ false, false, true, true, false, false, true, true, false, false, false, false },
	{ false, false, true, true, false, false, true, true, false, false, false, false },
	{ true, true, true, true, true, true, true, true, true, true, false, false },
	{ false, true, true, false, false, true, true, false, false, false, false, false },
	{ true, true, false, false, true, true, false, false, false, false, false, false },
	{ true, true, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '$'
            markBoldfontHashTable.Add(36, new bool[,]{ 

	{ false, false, false, true, false, false, true, false, false, false, false, false },
	{ false, false, false, true, true, true, true, false, false, false, false, false },
	{ false, true, true, true, false, false, true, true, true, false, false, false },
	{ true, true, false, true, false, false, true, false, true, true, false, false },
	{ true, true, false, true, false, false, true, false, true, true, false, false },
	{ true, true, false, true, false, false, true, false, false, false, false, false },
	{ false, true, true, true, false, false, true, false, false, false, false, false },
	{ false, false, false, true, true, true, true, false, false, false, false, false },
	{ false, false, false, true, false, false, true, true, true, false, false, false },
	{ false, false, false, true, false, false, true, false, true, true, false, false },
	{ true, true, false, true, false, false, true, false, true, true, false, false },
	{ true, true, false, true, false, false, true, false, true, true, false, false },
	{ false, true, true, true, false, false, true, true, true, false, false, false },
	{ false, false, false, true, true, true, true, false, false, false, false, false },
	{ false, false, false, true, false, false, true, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '%'
            markBoldfontHashTable.Add(37, new bool[,]{ 

	{ false, true, true, true, false, false, false, false, false, false, false, false },
	{ true, true, false, true, true, false, false, false, false, true, true, false },
	{ true, true, false, true, true, false, false, false, true, true, true, false },
	{ true, true, false, true, true, false, false, true, true, true, false, false },
	{ false, true, true, true, false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, true, true, true, false, false, false, false },
	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ false, false, false, true, true, true, false, false, false, false, false, false },
	{ false, false, true, true, true, false, false, true, true, true, false, false },
	{ false, true, true, true, false, false, true, true, false, true, true, false },
	{ true, true, true, false, false, false, true, true, false, true, true, false },
	{ true, true, false, false, false, false, true, true, false, true, true, false },
	{ false, false, false, false, false, false, false, true, true, true, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '&'
            markBoldfontHashTable.Add(38, new bool[,]{ 

	{ false, false, false, true, true, true, false, false, false, false, false, false },
	{ false, false, true, true, false, true, true, false, false, false, false, false },
	{ false, true, true, false, false, false, true, true, false, false, false, false },
	{ false, true, true, false, false, false, true, true, false, false, false, false },
	{ false, true, true, false, false, true, true, false, false, false, false, false },
	{ false, false, true, true, true, true, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, true, true, true, true, false, false, true, true, true, false },
	{ false, true, true, false, false, true, true, false, true, true, false, false },
	{ true, true, false, false, false, false, true, true, true, false, false, false },
	{ true, true, false, false, false, false, false, true, true, false, false, false },
	{ false, true, true, false, false, true, true, false, true, true, false, false },
	{ false, false, false, true, true, true, false, false, false, true, true, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '''
            markBoldfontHashTable.Add(39, new bool[,]{ 

	{ false, false, true, true, true, false, false, false, false, false, false, false },
	{ false, false, true, true, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '('
            markBoldfontHashTable.Add(40, new bool[,]{ 

	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region ')'
            markBoldfontHashTable.Add(41, new bool[,]{ 

	{ true, true, true, false, false, false, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ true, true, true, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '*'
            markBoldfontHashTable.Add(42, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, true, false, false, false, false, false, false },
	{ false, false, false, false, false, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ true, true, true, true, true, true, true, true, true, true, true, false },
	{ false, true, true, true, true, true, true, true, true, true, false, false },
	{ false, false, true, true, true, true, true, true, true, false, false, false },
	{ false, false, true, true, true, true, true, true, true, false, false, false },
	{ false, true, true, true, true, true, true, true, true, true, false, false },
	{ true, true, true, true, true, true, true, true, true, true, true, false },
	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, false, false, false, false, false, false },
	{ false, false, false, false, false, true, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '+'
            markBoldfontHashTable.Add(43, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, true, true, true, true, true, true, true, true, true, true, false },
	{ false, true, true, true, true, true, true, true, true, true, true, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region ','
            markBoldfontHashTable.Add(44, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, true, true, true, false, false, false, false, false, false, false, false },
	{ false, true, true, true, true, false, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '-'
            markBoldfontHashTable.Add(45, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, true, true, true, true, true, true, true, true, true, false, false },
	{ false, true, true, true, true, true, true, true, true, true, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '.'
            markBoldfontHashTable.Add(46, new bool[,]{ 

	{ false, false, false, false, false, false, false, false},
	{ false, false, false, false, false, false, false, false},
	{ false, false, false, false, false, false, false, false},
	{ false, false, false, false, false, false, false, false},
	{ false, false, false, false, false, false, false, false},
	{ false, false, false, false, false, false, false, false},
	{ false, false, false, false, false, false, false, false},
	{ false, false, false, false, false, false, false, false},
	{ false, false, false, false, false, false, false, false},
	{ false, false, true, true, false, false, false, false},
	{ false, true, true, true, true, false, false, false},
	{ false, true, true, true, true, false, false, false},
	{ false, false, true, true, false, false, false, false},
	{ false, false, false, false, false, false, false, false},
	{ false, false, false, false, false, false, false, false},
	{ false, false, false, false, false, false, false, false}});
            #endregion
            #region '/'
            markBoldfontHashTable.Add(47, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, true, true, false },
	{ false, false, false, false, false, false, false, false, true, true, true, false },
	{ false, false, false, false, false, false, false, true, true, true, false, false },
	{ false, false, false, false, false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, true, true, true, false, false, false, false },
	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ false, false, false, true, true, true, false, false, false, false, false, false },
	{ false, false, true, true, true, false, false, false, false, false, false, false },
	{ false, true, true, true, false, false, false, false, false, false, false, false },
	{ true, true, true, false, false, false, false, false, false, false, false, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '0'
            markBoldfontHashTable.Add(48, new bool[,]{ 

	{ false, false, false, true, true, true, true, true, false, false, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ true, true, false, false, false, false, false, false, true, true, true, false },
	{ true, true, false, false, false, false, false, true, false, true, true, false },
	{ true, true, false, false, false, false, true, false, false, true, true, false },
	{ true, true, false, false, false, true, false, false, false, true, true, false },
	{ true, true, false, false, true, false, false, false, false, true, true, false },
	{ true, true, false, true, false, false, false, false, false, true, true, false },
	{ true, true, true, false, false, false, false, false, false, true, true, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, false, false, true, true, true, true, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '1'
            markBoldfontHashTable.Add(49, new bool[,]{ 

	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ false, false, false, true, true, true, true, false, false, false, false, false },
	{ false, false, true, true, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, true, true, true, true, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '2'
            markBoldfontHashTable.Add(50, new bool[,]{ 

	{ false, false, false, true, true, true, true, true, false, false, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ true, false, false, false, false, false, false, false, false, true, true, false },
	{ false, false, false, false, false, false, false, false, false, true, true, false },
	{ false, false, false, false, false, false, false, false, false, true, true, false },
	{ false, false, false, false, false, false, false, true, true, true, false, false },
	{ false, false, false, false, false, true, true, true, false, false, false, false },
	{ false, false, false, true, true, true, false, false, false, false, false, false },
	{ false, true, true, true, false, false, false, false, false, false, false, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ true, true, false, false, false, false, false, false, false, false, true, false },
	{ true, true, false, false, false, false, false, false, false, false, true, false },
	{ true, true, true, true, true, true, true, true, true, true, true, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '3'
            markBoldfontHashTable.Add(51, new bool[,]{ 

	{ false, false, false, true, true, true, true, true, false, false, false, false },
	{ false, true, true, false, false, false, false, true, true, true, false, false },
	{ true, false, false, false, false, false, false, false, false, true, true, false },
	{ false, false, false, false, false, false, false, false, false, true, true, false },
	{ false, false, false, false, false, false, false, false, false, true, true, false },
	{ false, false, false, false, false, false, false, true, true, true, false, false },
	{ false, false, false, true, true, true, true, true, false, false, false, false },
	{ false, false, false, false, false, false, false, true, true, true, false, false },
	{ false, false, false, false, false, false, false, false, false, true, true, false },
	{ false, false, false, false, false, false, false, false, false, true, true, false },
	{ true, false, false, false, false, false, false, false, false, true, true, false },
	{ false, true, true, false, false, false, false, true, true, true, false, false },
	{ false, false, false, true, true, true, true, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '4'
            markBoldfontHashTable.Add(52, new bool[,]{ 

	{ false, false, false, false, false, false, true, true, false, false, false, false },
	{ false, false, false, false, false, true, true, true, false, false, false, false },
	{ false, false, false, false, false, true, true, true, false, false, false, false },
	{ false, false, false, false, true, true, true, true, false, false, false, false },
	{ false, false, false, true, true, false, true, true, false, false, false, false },
	{ false, false, false, true, true, false, true, true, false, false, false, false },
	{ false, false, true, true, false, false, true, true, false, false, false, false },
	{ false, true, true, false, false, false, true, true, false, false, false, false },
	{ false, true, true, false, false, false, true, true, false, false, false, false },
	{ true, true, true, true, true, true, true, true, true, true, true, false },
	{ false, false, false, false, false, false, true, true, false, false, false, false },
	{ false, false, false, false, false, false, true, true, false, false, false, false },
	{ false, false, false, false, true, true, true, true, true, true, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '5'
            markBoldfontHashTable.Add(53, new bool[,]{ 

	{ true, true, true, true, true, true, true, true, true, true, true, false },
	{ true, true, false, false, false, false, false, false, false, false, true, false },
	{ true, true, false, false, false, false, false, false, false, false, true, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ true, true, false, true, true, true, true, true, false, false, false, false },
	{ true, true, true, false, false, false, false, true, true, true, false, false },
	{ true, false, false, false, false, false, false, false, false, true, true, false },
	{ false, false, false, false, false, false, false, false, false, true, true, false },
	{ false, false, false, false, false, false, false, false, false, true, true, false },
	{ false, false, false, false, false, false, false, false, false, true, true, false },
	{ true, false, false, false, false, false, false, false, false, true, true, false },
	{ false, true, true, false, false, false, false, true, true, true, false, false },
	{ false, false, false, true, true, true, true, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '6'
            markBoldfontHashTable.Add(54, new bool[,]{ 

	{ false, false, false, true, true, true, true, true, false, false, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ true, true, false, false, false, false, false, false, false, false, true, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ true, true, false, true, true, true, true, true, false, false, false, false },
	{ true, true, true, false, false, false, false, false, true, true, false, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, false, false, true, true, true, true, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '7'
            markBoldfontHashTable.Add(55, new bool[,]{ 

	{ true, true, true, true, true, true, true, true, true, true, true, false },
	{ true, false, false, false, false, false, false, false, false, true, true, false },
	{ true, false, false, false, false, false, false, false, true, true, false, false },
	{ false, false, false, false, false, false, false, true, true, false, false, false },
	{ false, false, false, false, false, false, true, true, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '8'
            markBoldfontHashTable.Add(56, new bool[,]{ 

	{ false, false, false, true, true, true, true, true, false, false, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, false, false, true, true, true, true, true, false, false, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, false, false, true, true, true, true, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '9'
            markBoldfontHashTable.Add(57, new bool[,]{ 

	{ false, false, false, true, true, true, true, true, false, false, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ false, true, true, false, false, false, false, false, true, true, true, false },
	{ false, false, false, true, true, true, true, true, false, true, true, false },
	{ false, false, false, false, false, false, false, false, false, true, true, false },
	{ true, false, false, false, false, false, false, false, false, true, true, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, false, false, true, true, true, true, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region ':'
            markBoldfontHashTable.Add(58, new bool[,]{ 

	{ false, false, false, false, false, false, false, false},
	{ false, false, false, false, false, false, false, false},
	{ false, false, true, true, false, false, false, false},
	{ false, true, true, true, true, false, false, false},
	{ false, true, true, true, true, false, false, false},
	{ false, false, true, true, false, false, false, false},
	{ false, false, false, false, false, false, false, false},
	{ false, false, false, false, false, false, false, false},
	{ false, false, true, true, false, false, false, false},
	{ false, true, true, true, true, false, false, false},
	{ false, true, true, true, true, false, false, false},
	{ false, false, true, true, false, false, false, false},
	{ false, false, false, false, false, false, false, false},
	{ false, false, false, false, false, false, false, false},
	{ false, false, false, false, false, false, false, false},
	{ false, false, false, false, false, false, false, false}});
            #endregion
            #region ';'
            markBoldfontHashTable.Add(59, new bool[,]{ 

	{ false, false, false, false, false, false, false, false},
	{ false, false, false, false, false, false, false, false},
	{ false, false, false, false, false, false, false, false},
	{ false, false, true, true, false, false, false, false},
	{ false, true, true, true, true, false, false, false},
	{ false, true, true, true, true, false, false, false},
	{ false, false, true, true, false, false, false, false},
	{ false, false, false, false, false, false, false, false},
	{ false, false, false, false, false, false, false, false},
	{ false, false, false, false, false, false, false, false},
	{ false, true, true, true, false, false, false, false},
	{ false, true, true, true, true, false, false, false},
	{ false, false, false, true, true, false, false, false},
	{ false, false, true, true, false, false, false, false},
	{ false, true, true, false, false, false, false, false},
	{ true, true, false, false, false, false, false, false}});
            #endregion
            #region '<'
            markBoldfontHashTable.Add(60, new bool[,]{ 

	{ false, false, false, false, false, false, true, true, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, false, true, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '='
            markBoldfontHashTable.Add(61, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, true, true, true, true, true, true, true, true, true, false, false },
	{ false, true, true, true, true, true, true, true, true, true, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, true, true, true, true, true, true, true, true, true, false, false },
	{ false, true, true, true, true, true, true, true, true, true, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '>'
            markBoldfontHashTable.Add(62, new bool[,]{ 

	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, false, true, true, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '?'
            markBoldfontHashTable.Add(63, new bool[,]{ 

	{ false, false, true, true, true, true, true, true, false, false, false, false },
	{ false, true, true, false, false, false, false, true, true, false, false, false },
	{ true, true, false, false, false, false, false, false, true, true, false, false },
	{ true, true, false, false, false, false, false, false, true, true, false, false },
	{ false, false, false, false, false, false, false, true, true, false, false, false },
	{ false, false, false, false, false, true, true, true, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '@'
            markBoldfontHashTable.Add(64, new bool[,]{ 

	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ false, false, true, true, false, false, false, true, true, false, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ true, true, false, false, true, true, true, false, false, true, true, false },
	{ true, true, false, true, true, false, true, true, false, true, true, false },
	{ true, true, false, true, true, false, true, true, false, true, true, false },
	{ true, true, false, true, true, false, true, true, false, true, true, false },
	{ true, true, false, false, true, true, true, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, true, false, false, false },
	{ false, false, false, false, true, true, true, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'A'
            markBoldfontHashTable.Add(65, new bool[,]{ 

	{ false, false, false, false, false, true, false, false, false, false, false, false },
	{ false, false, false, false, false, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ false, false, false, true, true, false, true, true, false, false, false, false },
	{ false, false, false, true, true, false, true, true, false, false, false, false },
	{ false, false, true, true, false, false, false, true, true, false, false, false },
	{ false, false, true, true, false, false, false, true, true, false, false, false },
	{ false, false, true, true, true, true, true, true, true, false, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ true, true, true, false, false, false, false, false, true, true, true, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'B'
            markBoldfontHashTable.Add(66, new bool[,]{ 

	{ true, true, true, true, true, true, true, false, false, false, false, false },
	{ false, true, true, false, false, false, false, true, true, false, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, true, true, false, false, false },
	{ false, true, true, true, true, true, true, false, false, false, false, false },
	{ false, true, true, false, false, false, false, true, true, false, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, true, true, false, false, false },
	{ true, true, true, true, true, true, true, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'C'
            markBoldfontHashTable.Add(67, new bool[,]{ 

	{ false, false, false, false, true, true, true, false, false, true, false, false },
	{ false, false, true, true, false, false, false, true, true, true, false, false },
	{ false, true, true, false, false, false, false, false, false, true, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, true, false, false },
	{ false, false, true, true, false, false, false, true, true, true, false, false },
	{ false, false, false, false, true, true, true, false, false, true, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'D'
            markBoldfontHashTable.Add(68, new bool[,]{ 

	{ true, true, true, true, true, true, true, false, false, false, false, false },
	{ false, true, true, false, false, false, false, true, true, false, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, false, true, true, false },
	{ false, true, true, false, false, false, false, false, false, true, true, false },
	{ false, true, true, false, false, false, false, false, false, true, true, false },
	{ false, true, true, false, false, false, false, false, false, true, true, false },
	{ false, true, true, false, false, false, false, false, false, true, true, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, true, true, false, false, false },
	{ true, true, true, true, true, true, true, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'E'
            markBoldfontHashTable.Add(69, new bool[,]{ 

	{ true, true, true, true, true, true, true, true, true, true, false, false },
	{ false, true, true, false, false, false, false, false, false, true, false, false },
	{ false, true, true, false, false, false, false, false, false, true, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, true, false, false, false, false, false },
	{ false, true, true, true, true, true, true, false, false, false, false, false },
	{ false, true, true, false, false, false, true, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, true, false, false },
	{ false, true, true, false, false, false, false, false, false, true, false, false },
	{ true, true, true, true, true, true, true, true, true, true, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'F'
            markBoldfontHashTable.Add(70, new bool[,]{ 

	{ true, true, true, true, true, true, true, true, true, true, false, false },
	{ false, true, true, false, false, false, false, false, false, true, false, false },
	{ false, true, true, false, false, false, false, false, false, true, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, true, false, false, false, false, false },
	{ false, true, true, true, true, true, true, false, false, false, false, false },
	{ false, true, true, false, false, false, true, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ true, true, true, true, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'G'
            markBoldfontHashTable.Add(71, new bool[,]{ 

	{ false, false, false, false, true, true, true, false, false, true, false, false },
	{ false, false, true, true, false, false, false, true, true, true, false, false },
	{ false, true, true, false, false, false, false, false, false, true, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ true, true, false, false, false, true, true, true, true, true, true, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, false, true, true, false, false, false, true, true, false, false, false },
	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'H'
            markBoldfontHashTable.Add(72, new bool[,]{ 

	{ true, true, true, true, false, false, false, true, true, true, true, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, true, true, true, true, true, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ true, true, true, true, false, false, false, true, true, true, true, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'I'
            markBoldfontHashTable.Add(73, new bool[,]{ 

	{ false, true, true, true, true, true, true, true, true, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, true, true, true, true, true, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'J'
            markBoldfontHashTable.Add(74, new bool[,]{ 

	{ false, false, false, true, true, true, true, true, true, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ true, false, false, false, true, true, false, false, false, false, false, false },
	{ false, true, true, true, true, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'K'
            markBoldfontHashTable.Add(75, new bool[,]{ 

	{ true, true, true, false, false, false, false, false, true, true, true, false },
	{ false, true, true, false, false, false, false, true, true, false, false, false },
	{ false, true, true, false, false, false, true, true, false, false, false, false },
	{ false, true, true, false, false, true, true, false, false, false, false, false },
	{ false, true, true, false, true, true, false, false, false, false, false, false },
	{ false, true, true, true, true, false, false, false, false, false, false, false },
	{ false, true, true, true, false, false, false, false, false, false, false, false },
	{ false, true, true, true, true, false, false, false, false, false, false, false },
	{ false, true, true, false, true, true, false, false, false, false, false, false },
	{ false, true, true, false, false, true, true, false, false, false, false, false },
	{ false, true, true, false, false, false, true, true, false, false, false, false },
	{ false, true, true, false, false, false, false, true, true, false, false, false },
	{ true, true, true, false, false, false, false, false, true, true, true, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'L'
            markBoldfontHashTable.Add(76, new bool[,]{ 

	{ true, true, true, true, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, true, false, false },
	{ false, true, true, false, false, false, false, false, false, true, false, false },
	{ true, true, true, true, true, true, true, true, true, true, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'M'
            markBoldfontHashTable.Add(77, new bool[,]{ 

	{ true, true, true, false, false, false, false, false, true, true, true, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, true, false, false, false, true, true, true, false, false },
	{ false, true, true, true, false, false, false, true, true, true, false, false },
	{ false, true, true, true, true, false, true, true, true, true, false, false },
	{ false, true, true, true, true, false, true, true, true, true, false, false },
	{ false, true, true, false, true, true, true, false, true, true, false, false },
	{ false, true, true, false, true, true, true, false, true, true, false, false },
	{ false, true, true, false, false, true, false, false, true, true, false, false },
	{ false, true, true, false, false, true, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ true, true, true, false, false, false, false, false, true, true, true, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'N'
            markBoldfontHashTable.Add(78, new bool[,]{ 

	{ true, true, true, false, false, false, false, false, true, true, true, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, true, false, false, false, false, true, true, false, false },
	{ false, true, true, true, false, false, false, false, true, true, false, false },
	{ false, true, true, true, true, false, false, false, true, true, false, false },
	{ false, true, true, false, true, true, false, false, true, true, false, false },
	{ false, true, true, false, true, true, false, false, true, true, false, false },
	{ false, true, true, false, false, true, true, false, true, true, false, false },
	{ false, true, true, false, false, false, true, true, true, true, false, false },
	{ false, true, true, false, false, false, true, true, true, true, false, false },
	{ false, true, true, false, false, false, false, true, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ true, true, true, false, false, false, false, false, true, true, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'O'
            markBoldfontHashTable.Add(79, new bool[,]{ 

	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ false, false, true, true, false, false, false, true, true, false, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, false, true, true, false, false, false, true, true, false, false, false },
	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'P'
            markBoldfontHashTable.Add(80, new bool[,]{ 

	{ true, true, true, true, true, true, true, true, false, false, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, false, true, true, false },
	{ false, true, true, false, false, false, false, false, false, true, true, false },
	{ false, true, true, false, false, false, false, false, false, true, true, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, true, true, true, true, true, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ true, true, true, true, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'Q'
            markBoldfontHashTable.Add(81, new bool[,]{ 

	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ false, false, true, true, false, false, false, true, true, false, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ true, true, false, false, false, false, false, false, false, true, true, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, false, true, true, false, false, false, true, true, false, false, false },
	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ false, false, true, true, false, false, true, true, false, false, true, false },
	{ false, false, false, false, false, false, false, false, true, true, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'R'
            markBoldfontHashTable.Add(82, new bool[,]{ 

	{ true, true, true, true, true, true, true, false, false, false, false, false },
	{ false, true, true, false, false, false, false, true, true, false, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, true, true, false, false, false },
	{ false, true, true, true, true, true, true, false, false, false, false, false },
	{ false, true, true, true, true, false, false, false, false, false, false, false },
	{ false, true, true, false, true, true, false, false, false, false, false, false },
	{ false, true, true, false, false, true, true, false, false, false, false, false },
	{ false, true, true, false, false, false, true, true, false, false, false, false },
	{ false, true, true, false, false, false, false, true, true, false, false, false },
	{ true, true, true, true, false, false, false, false, true, true, true, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'S'
            markBoldfontHashTable.Add(83, new bool[,]{ 

	{ false, false, false, true, true, true, true, true, true, false, true, false },
	{ false, true, true, false, false, false, false, false, false, true, true, false },
	{ true, true, false, false, false, false, false, false, false, false, true, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, false, true, true, true, false, false, false, false, false, false, false },
	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ false, false, false, false, false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false, true, true, false, false },
	{ false, false, false, false, false, false, false, false, false, true, true, false },
	{ true, false, false, false, false, false, false, false, false, true, true, false },
	{ true, true, false, false, false, false, false, false, true, true, false, false },
	{ true, false, true, true, true, true, true, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'T'
            markBoldfontHashTable.Add(84, new bool[,]{ 

	{ true, true, true, true, true, true, true, true, true, true, false, false },
	{ true, false, false, false, true, true, false, false, false, true, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, true, true, true, true, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'U'
            markBoldfontHashTable.Add(85, new bool[,]{ 

	{ true, true, true, false, false, false, false, false, true, true, true, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, false, true, true, false, false, false, true, true, false, false, false },
	{ false, false, true, true, false, false, false, true, true, false, false, false },
	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'V'
            markBoldfontHashTable.Add(86, new bool[,]{ 

	{ true, true, true, false, false, false, false, false, true, true, true, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, false, true, true, false, false, false, true, true, false, false, false },
	{ false, false, true, true, false, false, false, true, true, false, false, false },
	{ false, false, true, true, false, false, false, true, true, false, false, false },
	{ false, false, false, true, true, false, true, true, false, false, false, false },
	{ false, false, false, true, true, false, true, true, false, false, false, false },
	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, false, false, false, false, false, false },
	{ false, false, false, false, false, true, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'W'
            markBoldfontHashTable.Add(87, new bool[,]{ 

	{ true, true, true, false, false, true, false, false, true, true, true, false },
	{ false, true, true, false, false, true, false, false, true, true, false, false },
	{ false, true, true, false, false, true, false, false, true, true, false, false },
	{ false, true, true, false, true, true, true, false, true, true, false, false },
	{ false, true, true, false, true, true, true, false, true, true, false, false },
	{ false, true, true, false, true, true, true, false, true, true, false, false },
	{ false, false, true, true, true, false, true, true, true, false, false, false },
	{ false, false, true, true, true, false, true, true, true, false, false, false },
	{ false, false, true, true, true, false, true, true, true, false, false, false },
	{ false, false, true, true, true, false, true, true, true, false, false, false },
	{ false, false, false, true, false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'X'
            markBoldfontHashTable.Add(88, new bool[,]{ 

	{ true, true, true, false, false, false, false, true, true, true, false, false },
	{ false, true, true, false, false, false, false, true, true, false, false, false },
	{ false, false, true, true, false, false, true, true, false, false, false, false },
	{ false, false, true, true, false, false, true, true, false, false, false, false },
	{ false, false, false, true, true, true, true, false, false, false, false, false },
	{ false, false, false, true, true, true, true, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, true, true, true, true, false, false, false, false, false },
	{ false, false, false, true, true, true, true, false, false, false, false, false },
	{ false, false, true, true, false, false, true, true, false, false, false, false },
	{ false, false, true, true, false, false, true, true, false, false, false, false },
	{ false, true, true, false, false, false, false, true, true, false, false, false },
	{ true, true, true, false, false, false, false, true, true, true, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'Y'
            markBoldfontHashTable.Add(89, new bool[,]{ 

	{ true, true, true, false, false, false, false, true, true, true, false, false },
	{ false, true, true, false, false, false, false, true, true, false, false, false },
	{ false, true, true, false, false, false, false, true, true, false, false, false },
	{ false, false, true, true, false, false, true, true, false, false, false, false },
	{ false, false, true, true, false, false, true, true, false, false, false, false },
	{ false, false, false, true, true, true, true, false, false, false, false, false },
	{ false, false, false, true, true, true, true, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, true, true, true, true, true, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'Z'
            markBoldfontHashTable.Add(90, new bool[,]{ 

	{ true, true, true, true, true, true, true, true, true, true, true, false },
	{ true, false, false, false, false, false, false, false, false, true, true, false },
	{ false, false, false, false, false, false, false, false, true, true, true, false },
	{ false, false, false, false, false, false, false, true, true, true, false, false },
	{ false, false, false, false, false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, true, true, true, false, false, false, false },
	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ false, false, false, true, true, true, false, false, false, false, false, false },
	{ false, false, true, true, true, false, false, false, false, false, false, false },
	{ false, true, true, true, false, false, false, false, false, false, false, false },
	{ true, true, true, false, false, false, false, false, false, false, true, false },
	{ true, true, false, false, false, false, false, false, false, false, true, false },
	{ true, true, true, true, true, true, true, true, true, true, true, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '['
            markBoldfontHashTable.Add(91, new bool[,]{ 

	{ false, false, true, true, true, true, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, true, true, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '\'
            markBoldfontHashTable.Add(92, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ true, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, true, false, false, false, false, false, false, false },
	{ false, false, false, true, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, true, true, false, false, false, false },
	{ false, false, false, false, false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, true, true, true, false, false },
	{ false, false, false, false, false, false, false, false, true, true, true, false },
	{ false, false, false, false, false, false, false, false, false, true, true, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region ']'
            markBoldfontHashTable.Add(93, new bool[,]{ 

	{ false, false, true, true, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, true, true, true, true, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '^'
            markBoldfontHashTable.Add(94, new bool[,]{ 

	{ false, false, false, false, true, false, false, false, false, false, false, false },
	{ false, false, false, true, true, true, false, false, false, false, false, false },
	{ false, false, true, true, false, true, true, false, false, false, false, false },
	{ false, true, true, false, false, false, true, true, false, false, false, false },
	{ true, true, false, false, false, false, false, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '_'
            markBoldfontHashTable.Add(95, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ true, true, true, true, true, true, true, true, true, true, true, true }});
            #endregion
            #region '`'
            markBoldfontHashTable.Add(96, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, true, true, true, false, false, false, false, false, false },
	{ false, false, true, true, true, true, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'a'
            markBoldfontHashTable.Add(97, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, true, true, true, true, true, false, false, false, false, false },
	{ false, true, true, false, false, false, true, true, false, false, false, false },
	{ false, false, false, false, false, false, false, true, true, false, false, false },
	{ false, false, false, false, false, false, false, true, true, false, false, false },
	{ false, false, false, true, true, true, true, true, true, false, false, false },
	{ false, true, true, true, false, false, false, true, true, false, false, false },
	{ true, true, false, false, false, false, false, true, true, false, false, false },
	{ true, true, false, false, false, false, true, true, true, false, false, false },
	{ false, true, true, true, true, true, true, false, true, true, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'b'
            markBoldfontHashTable.Add(98, new bool[,]{ 

	{ true, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, true, true, true, false, false, false, false, false },
	{ false, true, true, true, false, false, false, true, true, false, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, true, false, false, false, true, true, false, false, false },
	{ true, true, true, false, true, true, true, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'c'
            markBoldfontHashTable.Add(99, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, true, true, true, true, false, false, false, false, false },
	{ false, true, true, false, false, false, false, true, true, false, false, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, true, true, false, false, false },
	{ false, false, false, true, true, true, true, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'd'
            markBoldfontHashTable.Add(100, new bool[,]{ 

	{ false, false, false, false, false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, true, true, false, false, false },
	{ false, false, false, false, false, false, false, true, true, false, false, false },
	{ false, false, false, false, false, false, false, true, true, false, false, false },
	{ false, false, false, true, true, true, false, true, true, false, false, false },
	{ false, true, true, false, false, false, true, true, true, false, false, false },
	{ true, true, false, false, false, false, false, true, true, false, false, false },
	{ true, true, false, false, false, false, false, true, true, false, false, false },
	{ true, true, false, false, false, false, false, true, true, false, false, false },
	{ true, true, false, false, false, false, false, true, true, false, false, false },
	{ true, true, false, false, false, false, false, true, true, false, false, false },
	{ false, true, true, false, false, false, true, true, true, false, false, false },
	{ false, false, false, true, true, true, false, true, true, true, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'e'
            markBoldfontHashTable.Add(101, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, true, true, true, true, false, false, false, false, false },
	{ false, true, true, false, false, false, false, true, true, false, false, false },
	{ true, true, false, false, false, false, false, false, true, true, false, false },
	{ true, true, false, false, false, false, false, false, true, true, false, false },
	{ true, true, true, true, true, true, true, true, true, true, false, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, true, true, false, false, false },
	{ false, false, false, true, true, true, true, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'f'
            markBoldfontHashTable.Add(102, new bool[,]{ 

	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ false, false, false, true, false, false, false, true, false, false, false, false },
	{ false, false, true, true, false, false, false, false, true, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ true, true, true, true, true, true, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ true, true, true, true, true, true, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'g'
            markBoldfontHashTable.Add(103, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, true, true, true, true, false, true, true, false, false },
	{ false, true, true, false, false, false, false, true, true, false, false, false },
	{ true, true, false, false, false, false, false, true, true, false, false, false },
	{ true, true, false, false, false, false, false, true, true, false, false, false },
	{ true, true, false, false, false, false, false, true, true, false, false, false },
	{ true, true, false, false, false, false, false, true, true, false, false, false },
	{ false, true, true, false, false, false, true, true, true, false, false, false },
	{ false, false, false, true, true, true, false, true, true, false, false, false },
	{ false, false, false, false, false, false, false, true, true, false, false, false },
	{ false, false, false, false, false, false, false, true, true, false, false, false },
	{ false, true, true, false, false, false, false, true, false, false, false, false },
	{ false, false, true, true, true, true, true, false, false, false, false, false }});
            #endregion
            #region 'h'
            markBoldfontHashTable.Add(104, new bool[,]{ 

	{ true, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, true, true, true, false, false, false, false, false },
	{ false, true, true, true, false, false, false, true, true, false, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ true, true, true, true, false, false, false, true, true, true, true, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'i'
            markBoldfontHashTable.Add(105, new bool[,]{ 

	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, true, true, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, true, true, true, true, true, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'j'
            markBoldfontHashTable.Add(106, new bool[,]{ 

	{ false, false, false, false, false, false, true, true, false, false, false, false },
	{ false, false, false, false, false, false, true, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, true, true, true, true, false, false, false, false },
	{ false, false, false, false, false, false, true, true, false, false, false, false },
	{ false, false, false, false, false, false, true, true, false, false, false, false },
	{ false, false, false, false, false, false, true, true, false, false, false, false },
	{ false, false, false, false, false, false, true, true, false, false, false, false },
	{ false, false, false, false, false, false, true, true, false, false, false, false },
	{ false, false, false, false, false, false, true, true, false, false, false, false },
	{ false, false, false, false, false, false, true, true, false, false, false, false },
	{ false, false, false, false, false, false, true, true, false, false, false, false },
	{ true, false, false, false, false, false, true, true, false, false, false, false },
	{ false, true, false, false, false, true, true, false, false, false, false, false },
	{ false, false, true, true, true, true, false, false, false, false, false, false }});
            #endregion
            #region 'k'
            markBoldfontHashTable.Add(107, new bool[,]{ 

	{ true, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, true, true, true, false, false, false },
	{ false, true, true, false, false, true, true, false, false, false, false, false },
	{ false, true, true, false, true, true, false, false, false, false, false, false },
	{ false, true, true, true, true, false, false, false, false, false, false, false },
	{ false, true, true, true, false, false, false, false, false, false, false, false },
	{ false, true, true, true, true, false, false, false, false, false, false, false },
	{ false, true, true, false, true, true, false, false, false, false, false, false },
	{ false, true, true, false, false, true, true, false, false, false, false, false },
	{ true, true, true, false, false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'l'
            markBoldfontHashTable.Add(108, new bool[,]{ 

	{ false, false, true, true, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, true, true, true, true, true, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'm'
            markBoldfontHashTable.Add(109, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ true, true, false, true, true, true, false, true, true, true, false, false },
	{ false, true, true, false, false, true, true, false, false, true, true, false },
	{ false, true, true, false, false, true, true, false, false, true, true, false },
	{ false, true, true, false, false, true, true, false, false, true, true, false },
	{ false, true, true, false, false, true, true, false, false, true, true, false },
	{ false, true, true, false, false, true, true, false, false, true, true, false },
	{ false, true, true, false, false, true, true, false, false, true, true, false },
	{ false, true, true, false, false, true, true, false, false, true, true, false },
	{ true, true, true, false, false, true, true, false, false, true, true, true },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'n'
            markBoldfontHashTable.Add(110, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ true, true, true, false, true, true, true, false, false, false, false, false },
	{ false, true, true, true, false, false, false, true, true, false, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ true, true, true, true, false, false, false, true, true, true, true, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'o'
            markBoldfontHashTable.Add(111, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, true, true, true, true, false, false, false, false, false },
	{ false, true, true, false, false, false, false, true, true, false, false, false },
	{ true, true, false, false, false, false, false, false, true, true, false, false },
	{ true, true, false, false, false, false, false, false, true, true, false, false },
	{ true, true, false, false, false, false, false, false, true, true, false, false },
	{ true, true, false, false, false, false, false, false, true, true, false, false },
	{ true, true, false, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, true, true, false, false, false },
	{ false, false, false, true, true, true, true, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'p'
            markBoldfontHashTable.Add(112, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ true, true, true, false, true, true, true, false, false, false, false, false },
	{ false, true, true, true, false, false, false, true, true, false, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, true, false, false, false, true, true, false, false, false },
	{ false, true, true, false, true, true, true, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ true, true, true, true, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'q'
            markBoldfontHashTable.Add(113, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, true, true, true, false, true, true, true, false, false },
	{ false, true, true, false, false, false, true, true, true, false, false, false },
	{ true, true, false, false, false, false, false, true, true, false, false, false },
	{ true, true, false, false, false, false, false, true, true, false, false, false },
	{ true, true, false, false, false, false, false, true, true, false, false, false },
	{ true, true, false, false, false, false, false, true, true, false, false, false },
	{ true, true, false, false, false, false, false, true, true, false, false, false },
	{ false, true, true, false, false, false, true, true, true, false, false, false },
	{ false, false, false, true, true, true, false, true, true, false, false, false },
	{ false, false, false, false, false, false, false, true, true, false, false, false },
	{ false, false, false, false, false, false, false, true, true, false, false, false },
	{ false, false, false, false, false, false, true, true, true, true, false, false }});
            #endregion
            #region 'r'
            markBoldfontHashTable.Add(114, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ true, true, true, false, true, true, true, false, false, false, false, false },
	{ false, true, true, true, false, false, false, true, true, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, false, false, false },
	{ true, true, true, true, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 's'
            markBoldfontHashTable.Add(115, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, true, true, true, true, true, true, true, false, false, false, false },
	{ true, true, false, false, false, false, false, false, true, false, false, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ false, true, true, true, false, false, false, false, false, false, false, false },
	{ false, false, false, true, true, true, false, false, false, false, false, false },
	{ false, false, false, false, false, true, true, true, false, false, false, false },
	{ false, false, false, false, false, false, false, true, true, false, false, false },
	{ true, false, false, false, false, false, false, true, true, false, false, false },
	{ false, true, true, true, true, true, true, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 't'
            markBoldfontHashTable.Add(116, new bool[,]{ 

	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ true, true, true, true, true, true, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, true, false, false, false },
	{ false, false, false, true, false, false, false, true, false, false, false, false },
	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'u'
            markBoldfontHashTable.Add(117, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ true, true, true, true, false, false, false, true, true, true, true, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, true, true, false, false, false, false, false, true, true, false, false },
	{ false, false, true, true, false, false, false, true, true, true, false, false },
	{ false, false, false, false, true, true, true, false, true, true, true, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'v'
            markBoldfontHashTable.Add(118, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ true, true, true, true, false, false, false, true, true, true, true, false },
	{ false, false, true, true, false, false, false, true, true, false, false, false },
	{ false, false, true, true, false, false, false, true, true, false, false, false },
	{ false, false, false, true, true, false, true, true, false, false, false, false },
	{ false, false, false, true, true, false, true, true, false, false, false, false },
	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ false, false, false, false, false, true, false, false, false, false, false, false },
	{ false, false, false, false, false, true, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'w'
            markBoldfontHashTable.Add(119, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ true, true, true, false, false, true, false, false, true, true, true, false },
	{ false, true, true, false, false, true, false, false, true, true, false, false },
	{ false, true, true, false, true, true, true, false, true, true, false, false },
	{ false, true, true, false, true, true, true, false, true, true, false, false },
	{ false, false, true, true, true, false, true, true, true, false, false, false },
	{ false, false, true, true, true, false, true, true, true, false, false, false },
	{ false, false, true, true, true, false, true, true, true, false, false, false },
	{ false, false, false, true, false, false, false, true, false, false, false, false },
	{ false, false, false, true, false, false, false, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'x'
            markBoldfontHashTable.Add(120, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ true, true, true, false, false, false, false, true, true, true, false, false },
	{ false, true, true, false, false, false, false, true, true, false, false, false },
	{ false, false, true, true, false, false, true, true, false, false, false, false },
	{ false, false, false, true, true, true, true, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, true, true, true, true, false, false, false, false, false },
	{ false, false, true, true, false, false, true, true, false, false, false, false },
	{ false, true, true, false, false, false, false, true, true, false, false, false },
	{ true, true, true, false, false, false, false, true, true, true, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'y'
            markBoldfontHashTable.Add(121, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ true, true, true, true, false, false, false, true, true, true, true, false },
	{ false, false, true, true, false, false, false, true, true, false, false, false },
	{ false, false, true, true, false, false, false, true, true, false, false, false },
	{ false, false, false, true, true, false, true, true, false, false, false, false },
	{ false, false, false, true, true, false, true, true, false, false, false, false },
	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ false, false, false, false, true, true, true, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false }});
            #endregion
            #region 'z'
            markBoldfontHashTable.Add(122, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ true, true, true, true, true, true, true, true, true, true, false, false },
	{ true, false, false, false, false, false, false, true, true, false, false, false },
	{ false, false, false, false, false, false, true, true, false, false, false, false },
	{ false, false, false, false, false, true, true, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, true, true, false, false, false, false, false, false, true, false, false },
	{ true, true, true, true, true, true, true, true, true, true, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '{'
            markBoldfontHashTable.Add(123, new bool[,]{ 

	{ false, false, false, false, false, false, true, true, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ true, true, true, false, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, false, false, true, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '|'
            markBoldfontHashTable.Add(124, new bool[,]{ 

	{ false, false, true, true, true, false, false, false, false, false, false, false },
	{ false, false, true, true, true, false, false, false, false, false, false, false },
	{ false, false, true, true, true, false, false, false, false, false, false, false },
	{ false, false, true, true, true, false, false, false, false, false, false, false },
	{ false, false, true, true, true, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, true, true, true, false, false, false, false, false, false, false },
	{ false, false, true, true, true, false, false, false, false, false, false, false },
	{ false, false, true, true, true, false, false, false, false, false, false, false },
	{ false, false, true, true, true, false, false, false, false, false, false, false },
	{ false, false, true, true, true, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '}'
            markBoldfontHashTable.Add(125, new bool[,]{ 

	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, false, false, true, true, true, false, false, false, false },
	{ false, false, false, false, true, true, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, false, true, true, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ true, true, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '~'
            markBoldfontHashTable.Add(126, new bool[,]{ 

	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, true, true, false, false, false, false, false, false, false, false },
	{ false, true, true, true, true, false, false, false, true, true, false, false },
	{ true, true, false, false, true, true, false, false, true, true, false, false },
	{ true, true, false, false, false, true, true, true, true, false, false, false },
	{ false, false, false, false, false, false, true, true, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false, false, false, false, false }});
            #endregion
            #region '°'
            markBoldfontHashTable.Add(176, new bool[,]{ 
	{ false, false, false, false, false, false, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, true, true, false, true, true, false, false },
	{ true, true, false, false, false, true, true, false },
	{ true, true, false, false, false, true, true, false },
	{ true, true, false, false, false, true, true, false },
	{ false, true, true, false, true, true, false, false },
	{ false, false, true, true, true, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false },
	{ false, false, false, false, false, false, false, false }});
            #endregion
            #endregion


        }

        public void GenerateLocalizationHashTables()
        {
            //log messages
            logMessagesHashTable.Add("logMessage_searchingBySerialStarted", "Searching by serial connection has started...");
            logMessagesHashTable.Add("logMessage_searchingBySerialEndedNotFound", "Searching by serial connection has ended. Screen was not found.");
            logMessagesHashTable.Add("logMessage_searchingByIpDefaultStarted", "Searching on default ip address...");
            logMessagesHashTable.Add("logMessage_searchingByIpFound", "Screen was found via network connection.");
            logMessagesHashTable.Add("logMessage_searchingByIpFoundIpIs", "The screen was found via network connection. IP is");
            logMessagesHashTable.Add("logMessage_searchingByIpFoundIpIsDefault", "The screen was found via network connection. IP is default ");
            logMessagesHashTable.Add("logMessage_searchingByIpInSelectedNetwork", "Searching in selected network...");
            logMessagesHashTable.Add("logMessage_searchingByIpNotFound", "Screen was not found via network connection.");
            logMessagesHashTable.Add("logMessage_searchingByIpDefaultNotFound", "The screen was not found via network default IP address.");
            logMessagesHashTable.Add("logMessage_searchingByIpCanceled", "Searching via network connection was canceled.");
            logMessagesHashTable.Add("logMessage_searchingByIpAttemptingToConnect", "Attempting to connect by given IP...");
            logMessagesHashTable.Add("logMessage_searchingByIpUnableToConnect", "Could not connect to the screen.\nProbably given IP was misstyped.");
            logMessagesHashTable.Add("logMessage_initNetworkSearchWindow", "Initializing network search window...");
            logMessagesHashTable.Add("logMessage_networkSearchWindowInited", "Network search window was initialized.");
            logMessagesHashTable.Add("logMessage_searchingBySerialEndedFound", "A screen was found via serial connection.");
            logMessagesHashTable.Add("logMessage_selectNetwork", "Please select network to search from.");
            logMessagesHashTable.Add("logMessage_searchingByCOM", "Searching at {0} of {1} ports - {2} with baudrate {3}...");
            logMessagesHashTable.Add("logMessage_deletingOldFiles", "Deleting old files...");
            logMessagesHashTable.Add("logMessage_savingFiles", "Saving {0} files...");
            logMessagesHashTable.Add("logMessage_uploadingFile", "Uploading {0} of {1} files...");
            logMessagesHashTable.Add("logMessage_resetingControllerApp", "Reseting controller application...");

            //messageboxes
            messageBoxesHashTable.Add("messageBoxTitle_connectViaNetwork", "LedConfig - Search Failure");
            messageBoxesHashTable.Add("messageBoxMessage_connectViaNetwork", "Do you want to connect via network?");
            messageBoxesHashTable.Add("messageBoxMessage_screenWasFound", "The screen was found");
            messageBoxesHashTable.Add("messageBoxTitle_screenWasFound", "LedConfig - Search Success");
            messageBoxesHashTable.Add("messageBoxMessage_couldNotOpenFile", "Could not create specified file.");
            messageBoxesHashTable.Add("messageBoxMessage_changeType", "Change type?");
            messageBoxesHashTable.Add("messageBoxTitle_changeType", "Changing item's type");
            messageBoxesHashTable.Add("messageBoxMessage_unableToChangeFont", "Unable to change font");
            messageBoxesHashTable.Add("messageBoxMessage_deleteItem", "Are you sure?");
            messageBoxesHashTable.Add("messageBoxTitle_deleteItem", "Deleting element");
            messageBoxesHashTable.Add("messageBoxMessage_changeLevelBeginner", "Beginner has less option, your work will be erased. Proceed?");
            messageBoxesHashTable.Add("messageBoxTitle_changeLevelBeginner", "Warning");
            messageBoxesHashTable.Add("messageBoxMessage_changeLevelExpert", "Expert has access to screen settings, your screen will not work if you do something wrong there. Proceed?");
            messageBoxesHashTable.Add("messageBoxTitle_changeLevelExpert", "Warning");
            messageBoxesHashTable.Add("messageBoxMessage_passwordAccessDenied", "Access Denied!");
            messageBoxesHashTable.Add("messageBoxTitle_passwordAccessDenied", "LedConfig - Authentication Failure");
            messageBoxesHashTable.Add("messageBoxMessage_only10Windows", "Only 10 windows in program allowed.");
            //messageBoxesHashTable.Add("", "");

            #region Tooltips
            tooltipsHashTable.Add("tooltipTitle_Information", "Information");

            tooltipsHashTable.Add("tooltipMessage_progSavButton", "Press this button to save your programs to file, so you will be able to load them in the future.");
            tooltipsHashTable.Add("tooltipMessage_progLoadButton", "Press this button to load one of previously saved programs.");
            tooltipsHashTable.Add("tooltipMessage_playbillSendButton", "Send everything to the screen.");
            tooltipsHashTable.Add("tooltipMessage_progressBar1", "Progress bar that informs you if there is anything happenning at the moment.");

            tooltipsHashTable.Add("tooltipMessage_beginnerLevelCheckBox", "Change application user level to the easiest - beginner.");
            tooltipsHashTable.Add("tooltipMessage_advancedLevelCheckBox", "Change application user level to the one with most functions - advanced.");
            tooltipsHashTable.Add("tooltipMessage_expertLevelCheckBox", "Change application user level to the one with access to all settings - expert.");

            tooltipsHashTable.Add("tooltipMessage_languageComboBox", "This drop-down list allows to change language used by program \r\nsimply click on that down arrow and select best option.");
            tooltipsHashTable.Add("tooltipMessage_showPreviewButton", "This button shows a preview window,\r\nwhich shows how the program would look like on the LED screen.");
            tooltipsHashTable.Add("tooltipMessage_showSettingsButton", "Press this button to open an advanced network settings window.");

            tooltipsHashTable.Add("tooltipMessage_addButton", "Press this button to add program, window or item to your playbill\r\nNew item'll be added depending on selected piece of a tree.");
            tooltipsHashTable.Add("tooltipMessage_deleteButton", "Press this button to delete selected program, window or item from playbill.");
            tooltipsHashTable.Add("tooltipMessage_upButton", "Press this button to move up selected program, window or item in playbill hierarchy.");
            tooltipsHashTable.Add("tooltipMessage_downButton", "Press this button to move down selected program, window or item in playbill hierarchy.");

            tooltipsHashTable.Add("tooltipMessage_playbillWidthTextBox", "The width of LED screen, that will display programs");
            tooltipsHashTable.Add("tooltipMessage_playbillHeightTextBox", "The height of LED screen, that will display programs");
            tooltipsHashTable.Add("tooltipMessage_playbillColorComboBox", "The available colors used by LED screen, that will display programs\r\ndifferent screens could use different pallets of colours.");
            tooltipsHashTable.Add("tooltipMessage_useDeaultsForPlaybillCheckBox", "If this item is checked width, height and colors will return to default\r\nadjusted to screen which with you recieved this program.");

            tooltipsHashTable.Add("tooltipMessage_programNameTextBox", "The name of currently selected program.");
            tooltipsHashTable.Add("tooltipMessage_programWidthTextBox", "The width of currently selected program\r\nmust be the same as screen width.");
            tooltipsHashTable.Add("tooltipMessage_programHeightTextBox", "The height of currently selected program\r\nmust be the same as screen height.");
            tooltipsHashTable.Add("tooltipMessage_runningCheckBox", "If this item is checked, program will be played only at specified days and hours of the week.");
            tooltipsHashTable.Add("tooltipMessage_runningDateTimeBegin", "The hour and minute when screen will start displaying selected program.");
            tooltipsHashTable.Add("tooltipMessage_runningDateTimeEnd", "The hour and minute when screen will stop playing selected program.");
            tooltipsHashTable.Add("tooltipMessage_runningDayscheckedListBox", "Program'll be played only at checked days of the week.");

            tooltipsHashTable.Add("tooltipMessage_windowXTextBox", "The x position of left edge of the window in program \r\nIt should be divisible by 8.");
            tooltipsHashTable.Add("tooltipMessage_windowYTextBox", "The y position of left edge of the window in program.");
            tooltipsHashTable.Add("tooltipMessage_windowWidthTextBox", "The width of the window in program \r\nIt should be divisible by 8.");
            tooltipsHashTable.Add("tooltipMessage_windowHeightTextBox", "The height of the window in program.");
            tooltipsHashTable.Add("tooltipMessage_windowDivideHorizontalButton", "This button divides selected window horizontally \r\ninto two halves or in other possible proportions.");
            tooltipsHashTable.Add("tooltipMessage_windowDivideVerticalButton", "This button divides selected window vertically \r\ninto two halves or in other possible proportions.");
            tooltipsHashTable.Add("tooltipMessage_windowNameTextBox", "The name of currently selected window.");

            tooltipsHashTable.Add("tooltipMessage_bitmaptextBoldButton", "Press this button to change font style to bold.");
            tooltipsHashTable.Add("tooltipMessage_bitmaptextItalicButton", "Press this button to change font style to italic.");
            tooltipsHashTable.Add("tooltipMessage_bitmaptextUnderlineButton", "Press this button to change font style to underline.");
            tooltipsHashTable.Add("tooltipMessage_bitmaptextLeftButton", "This button change text align to left.");
            tooltipsHashTable.Add("tooltipMessage_bitmaptextRightButton", "This button change text align to right.");
            tooltipsHashTable.Add("tooltipMessage_bitmaptextCenterButton", "This button change text align to center.");

            tooltipsHashTable.Add("tooltipMessage_animatorRenderComboBox", "This drop-down list allow to set drawing mode for the screen,\r\nif it should draw at the center, stretch to fit window, zoom, or create mosaic from small images.");
            tooltipsHashTable.Add("tooltipMessage_bitmapRenderComboBox", "This drop-down list allow to set drawing mode for the screen,\r\nif it should draw at the center, stretch to fit window, zoom, or create mosaic from small images.");

            #endregion
        }

        public void SearchForScreen()
        {
            try
            {
                logWindow.Clearlog();
                logWindow.Show();


                if (Communication.Type == CommunicationType.Serial || Communication.Type == CommunicationType.None)
                {
                    logWindow.Message = logMessagesHashTable["logMessage_searchingBySerialStarted"].ToString();
                    Communication.Type = CommunicationType.Serial;
                    if (SetupTestedPorts(false) > 0) // check serial communication
                    {
                        Communication.Type = CommunicationType.Serial;
                    }
                    else // check network communication
                    {
                        logWindow.Message = logMessagesHashTable["logMessage_searchingBySerialEndedNotFound"].ToString();

                        logWindow.Message = logMessagesHashTable["logMessage_searchingByIpDefaultStarted"].ToString();
                        Communication.Type = CommunicationType.Network;
                        if (netSearchWindow.TestDefaultIP() == false)
                        {
                            var result = MessageBox.Show(messageBoxesHashTable["messageBoxMessage_connectViaNetwork"].ToString(),//"Do you want to connect via network?",
                                messageBoxesHashTable["messageBoxTitle_connectViaNetwork"].ToString(),//"LedConfig - Search Failure", 
                                MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                            if (result == DialogResult.Yes)
                            {
                                Communication.Type = CommunicationType.Network;

                                logWindow.Message = logMessagesHashTable["logMessage_initNetworkSearchWindow"].ToString();
                                if (netSearchWindow.ShowDialog() == System.Windows.Forms.DialogResult.No)
                                {
                                    Communication.Type = CommunicationType.None;
                                    network = false;


                                    screenFoundInfoLabel.Text = "NOT FOUND";
                                    screenFoundInfoLabel.ForeColor = Color.Red;
                                }
                            }
                        }
                    }
                }
                else if (Communication.Type == CommunicationType.Network)
                {
                    logWindow.Message = logMessagesHashTable["logMessage_searchingByIpDefaultStarted"].ToString();
                    Communication.Type = CommunicationType.Network;
                    if (netSearchWindow.TestDefaultIP() == false)
                    {

                        logWindow.Message = logMessagesHashTable["logMessage_initNetworkSearchWindow"].ToString();
                        if (netSearchWindow.ShowDialog() == System.Windows.Forms.DialogResult.No)
                        {
                            Communication.Type = CommunicationType.None;
                            network = false;

                            screenFoundInfoLabel.Text = "NOT FOUND";
                            screenFoundInfoLabel.ForeColor = Color.Red;

                            //logWindow.Hide();
                            //return;
                            //goto ret;
                        }
                    }

                    if (Communication.Type == CommunicationType.None)
                    {
                        //Communication.Type = CommunicationType.Serial;

                        logWindow.Message = logMessagesHashTable["logMessage_searchingBySerialStarted"].ToString();
                        SetupTestedPorts(false); // check serial communication
                    }
                }


                if (Communication.Type != CommunicationType.None)
                {
                    bool sendTime = true;
                    bool.TryParse(ConfigHashtable["AutoSendTime"].ToString(), out sendTime);

                    if (sendTime)
                        Communication.SetTime(DateTime.Now);

                    screenFoundInfoLabel.Text = "FOUND";
                    screenFoundInfoLabel.ForeColor = Color.Green;
                }

                if (Communication.Type == CommunicationType.Serial)
                {
                    logWindow.Message = logMessagesHashTable["logMessage_searchingBySerialEndedFound"].ToString();
                    UpdateSerialCommunication();
                }
                else if (Communication.Type == CommunicationType.Network)
                {
                    logWindow.Message = logMessagesHashTable["logMessage_searchingByIpFound"].ToString();
                    UpdateNetworkCommunication();
                }
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message, "Error");
                Communication.Type = CommunicationType.None;
                screenFoundInfoLabel.Text = "NOT FOUND";
                screenFoundInfoLabel.ForeColor = Color.Red;
            }
        }


        public void SearchForScreen(bool showSuccessMsg)
        {
            logWindow.Clearlog();
            logWindow.Show();

            screenFoundInfoLabel.Text = "NOT FOUND";
            screenFoundInfoLabel.ForeColor = Color.Red;

            logWindow.Message = logMessagesHashTable["logMessage_searchingBySerialStarted"].ToString();

            //  SetupPortsList(ref portsCheckedListBox);
            SetupTestedPorts(showSuccessMsg);
            //if (SetupTestedPorts(showSuccessMsg) == 0)
            if (Communication.Type == CommunicationType.None)
            {
                logWindow.Message = logMessagesHashTable["logMessage_searchingBySerialEndedNotFound"].ToString();
                logWindow.Message = logMessagesHashTable["logMessage_searchingByIpDefaultStarted"].ToString();

                netSearchWindow.TestDefaultIP();

                //if (netSearchWindow.TestDefaultIP() == true)
                if (Communication.Type == CommunicationType.Network)
                {
                    network = true;
                    logWindow.Message = logMessagesHashTable["logMessage_searchingByIpFound"].ToString();
                    //NetworkCommunication.SetTime();

                    screenFoundInfoLabel.Text = "FOUND";
                    screenFoundInfoLabel.ForeColor = Color.Green;
                }
                //else if (network == true)
                else if (Communication.Type == CommunicationType.None)
                {
                    //Thread.Sleep(4000);
                    var result = MessageBox.Show(messageBoxesHashTable["messageBoxMessage_connectViaNetwork"].ToString(),//"Do you want to connect via network?",
                            messageBoxesHashTable["messageBoxTitle_connectViaNetwork"].ToString(),//"LedConfig - Search Failure", 
                            MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                    if (result == DialogResult.Yes)
                    {
                        Communication.Type = CommunicationType.Network;

                        logWindow.Message = logMessagesHashTable["logMessage_initNetworkSearchWindow"].ToString();
                        if (netSearchWindow.ShowDialog() == System.Windows.Forms.DialogResult.No)
                        {
                            Communication.Type = CommunicationType.None;
                            network = false;
                        }
                    }
                }
            }
            else
            {
                logWindow.Message = logMessagesHashTable["logMessage_searchingBySerialEndedFound"].ToString();

                bool sendTime = true;
                bool.TryParse(ConfigHashtable["AutoSendTime"].ToString(), out sendTime);

                if (sendTime)
                    Communication.SetTime(DateTime.Now);

                screenFoundInfoLabel.Text = "FOUND";
                screenFoundInfoLabel.ForeColor = Color.Green;

                UpdateSerialCommunication();
            }
        }

        private void MakeTransparentControls(Control parent)
        {
            if (parent.Controls != null && parent.Controls.Count > 0)
            {
                foreach (Control control in parent.Controls)
                {
                    if ((control is PictureBox) || (control is Label) || (control is GroupBox) || (control is CheckBox))
                        control.BackColor = Color.Transparent;

                    if (control.Controls != null && control.Controls.Count > 0)
                        MakeTransparentControls(control);

                    if (control is TabControl)
                    {
                        foreach (TabPage tab in ((TabControl)control).TabPages)
                        {
                            tab.BackColor = Color.FromArgb(0xE1, 0xE5, 0xEE);
                        }

                        /*
                        control.BackgroundImage = this.BackgroundImage;
                        foreach (TabPage tab in ((TabControl)control).TabPages)
                        {
                            tab.BackColor = Color.Transparent;
                            int left = 0, top = 0;
                            Bitmap tabBackground = null;
                            Graphics tabGraphics = null;

                            if (tab.Parent.Parent != this)
                            {
                                left = tab.Left + tab.Parent.Left + tab.Parent.Parent.Left + tab.Parent.Parent.Parent.Left;
                                top = tab.Top + tab.Parent.Top + tab.Parent.Parent.Top + tab.Parent.Parent.Parent.Top;
                            }
                            else
                            {
                                left = tab.Left + tab.Parent.Left;
                                top = tab.Top + tab.Parent.Top;
                            }


                            tabBackground = new Bitmap(tab.Width, tab.Height);
                            tabGraphics = Graphics.FromImage(tabBackground);
                            tabGraphics.DrawImageUnscaledAndClipped(this.BackgroundImage, new Rectangle(-left, -top, tab.Width + left, tab.Height + top));

                            tab.BackgroundImage = tabBackground;
                            //tabBackground.Dispose();
                            //tabGraphics.Dispose();
                            
                            if(tab.Controls != null && tab.Controls.Count > 0)
                                MakeTransparentControls(tab);
                        }
                         * */
                    }
                }
            }
        }

        #region Configuration methods
        private void LoadConfiguration()
        {
            if (ConfigHashtable != null)
                ConfigHashtable.Clear();
            else
                ConfigHashtable = new System.Collections.Hashtable();


            if (!File.Exists(configFileName))
            {
                string configFileContent = BengiLED_for_C_Power.Properties.Resources.ledconfig;

                FileStream fs = File.Create(configFileName);

                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(configFileContent);
                }
            }

            XmlTextReader configFile = new XmlTextReader(configFileName);

            string elementName = "";
            string parentName = "";

            while (configFile.Read())
            {
                switch (configFile.NodeType)
                {
                    case XmlNodeType.Element:
                        if (configFile.Name == "Default" || configFile.Name == "Current"
                            || configFile.Name == "BitmapText" || configFile.Name == "Bitmap" || configFile.Name == "Animation"
                            || configFile.Name == "Clock" || configFile.Name == "Temperature" || configFile.Name == "Paths")
                            parentName = configFile.Name;
                        else if (configFile.Name == "Language")
                        {
                            string langName = configFile.GetAttribute("name");
                            if (!string.IsNullOrEmpty(langName))
                            {
                                if (langName == "American_english")
                                    langName = "English (US)";
                                else if (langName == "British_english")
                                    langName = "English (UK)";
                                ConfigHashtable.Add("Language", langName);
                                languageComboBox.SelectedItem = langName;
                                languageComboBox.SelectedIndex = languageComboBox.Items.IndexOf(langName);
                            }
                        }
                        else if (configFile.Name == "Tester")
                        {
                            string pass = configFile.GetAttribute("password");
                            if (!string.IsNullOrEmpty(pass))
                                ConfigHashtable.Add("TesterPassword", pass);
                        }
                        else if (configFile.Name == "Configurator")
                        {
                            string pass = configFile.GetAttribute("password");
                            if (!string.IsNullOrEmpty(pass))
                                ConfigHashtable.Add("ConfiguratorPassword", pass);
                        }
                        else if (configFile.Name == "User")
                        {
                            string level = configFile.GetAttribute("level");
                            if (!string.IsNullOrEmpty(level))
                                ConfigHashtable.Add("UserLevel", level);
                        }
                        else
                            elementName = configFile.Name;
                        break;
                    case XmlNodeType.Text:
                        string key = string.Format("{0}{1}", parentName, elementName);
                        ConfigHashtable.Add(key, configFile.Value);
                        break;
                    case XmlNodeType.EndElement:
                        if (configFile.Name == "Default" || configFile.Name == "Current"
                            || configFile.Name == "BitmapText" || configFile.Name == "Bitmap" || configFile.Name == "Clock"
                            || configFile.Name == "Animaton" || configFile.Name == "Temperature" || configFile.Name == "Paths")
                            parentName = "";
                        break;

                }
            }
            configFile.Close();
        }

        private void ConfigurationColorCheck()
        {
            if (CurrentPlaybill == null)
                return;

            if (!CurrentPlaybill.CheckColor(ConfigHashtable["BitmapTextBackColor"].ToString()))
            {
                ConfigHashtable["BitmapTextBackColor"] = bitmaptextBackgroundComboBox.Items[0];
                SetConfigurationSettingText("LedConfiguration/Program/Defaults/BitmapText/BackColor", ConfigHashtable["BitmapTextBackColor"].ToString());
            }

            if (!CurrentPlaybill.CheckColor(ConfigHashtable["ClockFontColor"].ToString()))
            {
                ConfigHashtable["ClockFontColor"] = clockFontColorComboBox.Items[0];
                SetConfigurationSettingText("LedConfiguration/Program/Defaults/Clock/FontColor", ConfigHashtable["ClockFontColor"].ToString());
            }

            if (!CurrentPlaybill.CheckColor(ConfigHashtable["TemperatureFontColor"].ToString()))
            {
                ConfigHashtable["TemperatureFontColor"] = temperatureFontColorComboBox.Items[0];
                SetConfigurationSettingText("LedConfiguration/Program/Defaults/Temperature/FontColor", ConfigHashtable["TemperatureFontColor"].ToString());
            }

            if (!CurrentPlaybill.CheckColor(ConfigHashtable["BitmapTextFontColor"].ToString()))
            {
                ConfigHashtable["BitmapTextFontColor"] = bitmaptextFontColorComboBox.Items[1];
                SetConfigurationSettingText("LedConfiguration/Program/Defaults/BitmapText/FontColor", ConfigHashtable["BitmapTextFontColor"].ToString());
            }


        }

        public void SetConfigurationSettingText(string nodePath, string newValue)
        {
            // opening xml config file to read data
            FileStream fs = new FileStream(configFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            XmlDocument configFile = new XmlDocument();
            configFile.Load(fs);

            // selecting node to be updated
            XmlNode nodeToUpdate = configFile.SelectSingleNode(nodePath);
            nodeToUpdate.InnerText = newValue;

            // closing xml config file
            fs.Close();

            // opening file once again to truncate it's old content and write new
            FileStream fsxml = new FileStream(configFileName, FileMode.Truncate, FileAccess.Write, FileShare.ReadWrite);

            // saving updated file
            configFile.Save(fsxml);

            // closing xml config file
            fsxml.Close();
        }

        public void SetConfigurationSettingAttribute(string nodePath, string attributeName, string newValue)
        {
            // opening xml config file to read data
            FileStream fs = new FileStream(configFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            XmlDocument configFile = new XmlDocument();
            configFile.Load(fs);

            // selecting node to be updated
            XmlNode nodeToUpdate = configFile.SelectSingleNode(nodePath);
            nodeToUpdate.Attributes[attributeName].Value = newValue;

            // closing xml config file
            fs.Close();

            // opening file once again to truncate it's old content and write new
            FileStream fsxml = new FileStream(configFileName, FileMode.Truncate, FileAccess.Write, FileShare.ReadWrite);

            // saving updated file
            configFile.Save(fsxml);

            // closing xml config file
            fsxml.Close();
        }

        public void UpdateSerialCommunication()
        {
            bool reloadConfigutation = false;

            // update current serial port name
            if (SerialCommunication.CurrentSerialPort.PortName != ConfigHashtable["CurrentComport"].ToString())
            {
                SetConfigurationSettingText("LedConfiguration/Communication/Current/Comport", SerialCommunication.CurrentSerialPort.PortName);
                reloadConfigutation = true;
            }
            // update current serial port baudrate
            if (SerialCommunication.CurrentSerialPort.Baudrate.ToString() != ConfigHashtable["CurrentBaudrate"].ToString())
            {
                SetConfigurationSettingText("LedConfiguration/Communication/Current/Baudrate", SerialCommunication.CurrentSerialPort.Baudrate.ToString());
                reloadConfigutation = true;
            }
            // reload configuration if needed
            if (reloadConfigutation)
                LoadConfiguration();
        }

        public void UpdateNetworkCommunication()
        {
            bool reloadConfigutation = false;

            // update current serial port name
            if (NetworkCommunication.SocketsList[0].Ip.ToString() != ConfigHashtable["CurrentIP"].ToString())
            {
                SetConfigurationSettingText("LedConfiguration/Communication/Current/IP", NetworkCommunication.SocketsList[0].Ip.ToString());
                reloadConfigutation = true;
            }
            // update current serial port baudrate
            if (NetworkCommunication.SocketsList[0].IPport.ToString() != ConfigHashtable["CurrentIPPort"].ToString())
            {
                SetConfigurationSettingText("LedConfiguration/Communication/Current/IPPort", NetworkCommunication.SocketsList[0].IPport.ToString());
                reloadConfigutation = true;
            }
            // reload configuration if needed
            if (reloadConfigutation)
                LoadConfiguration();
        }
        #endregion

        private void LoadEffects()
        {
            if (!File.Exists(string.Format("{0}/alleffects.xml", programSettingsFolder)))
            {
                string effectsFileContent = BengiLED_for_C_Power.Properties.Resources.alleffects;

                FileStream fs = File.Create(string.Format("{0}/alleffects.xml", programSettingsFolder));

                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(effectsFileContent);
                }
            }

            XmlTextReader effectsFile = new XmlTextReader(string.Format("{0}/alleffects.xml", programSettingsFolder));
            //XmlTextReader effectsFile = new XmlTextReader("alleffects.xml");

            TreeNode groupNode = new TreeNode();
            TreeNode textGroupNode = new TreeNode();
            TreeNode effectNode = new TreeNode();

            while (effectsFile.Read())
            {
                switch (effectsFile.NodeType)
                {
                    case XmlNodeType.Element:
                        if (effectsFile.Name == "Group")
                        {
                            groupNode.Tag = effectsFile.Name;
                            groupNode.Text = effectsFile.GetAttribute("name");
                            groupNode.Name = effectsFile.GetAttribute("name");


                            textGroupNode.Tag = effectsFile.Name;
                            textGroupNode.Text = effectsFile.GetAttribute("name");
                            textGroupNode.Name = effectsFile.GetAttribute("name");
                        }
                        else if (effectsFile.Name == "Effect")
                        {
                            effectNode.Tag = effectsFile.GetAttribute("code");
                            effectNode.Text = effectsFile.GetAttribute("name");
                            effectNode.Name = effectsFile.GetAttribute("name");
                            groupNode.Nodes.Add(effectNode);
                            effectNode = new TreeNode();

                            effectNode.Tag = effectsFile.GetAttribute("code");
                            effectNode.Text = effectsFile.GetAttribute("name");
                            effectNode.Name = effectsFile.GetAttribute("name");
                            textGroupNode.Nodes.Add(effectNode);
                            effectNode = new TreeNode();
                        }
                        break;
                    case XmlNodeType.EndElement:
                        if (effectsFile.Name == "Group")
                        {
                            effectsTreeView.Nodes.Add(groupNode);
                            bitmapTextEffectsTreeView.Nodes.Add(textGroupNode);
                            textGroupNode = new TreeNode();
                            groupNode = new TreeNode();
                        }
                        break;

                }
            }
        }

        private Control FindControlInForm(string name, Form parent)
        {
            Control retval = null;

            Control[] controlsTab = parent.Controls.Find(name, true);

            if (controlsTab == null || controlsTab.Length == 0)
            {
                foreach (Form f in parent.OwnedForms)
                {
                    retval = FindControlInForm(name, f);
                    if (retval != null)
                        break;
                }
            }
            else
                retval = controlsTab[0];

            return retval;
        }

        public void SetTooltips()
        {
            forButton.AutomaticDelay = 1000;
            forButton.IsBalloon = true;
            forButton.ToolTipIcon = ToolTipIcon.Info;
            if (!tooltipsHashTable.ContainsKey("tooltipTitle_Information"))
                forButton.ToolTipTitle = "Information";
            else
                forButton.ToolTipTitle = tooltipsHashTable["tooltipTitle_Information"].ToString();//"Information"; //"BengiLED context-sensitive help";

            foreach (object key in tooltipsHashTable.Keys)
            {
                string controlName = key.ToString().Substring("tooltipMessage_".Length);
                Control control = FindControlInForm(controlName, this);

                if (control != null)
                    forButton.SetToolTip(control, tooltipsHashTable[key.ToString()].ToString());
            }



            //forButton.SetToolTip(progSavButton, "Press this button to save your programs to file, so you will be able to load them in the future.");
            //forButton.SetToolTip(progLoadButton, "Press this button to load one of previously saved programs.");
            //forButton.SetToolTip(playbillSendButton, "Send everything to the screen.");
            //forButton.SetToolTip(progressBar1, "Progress bar that informs you if there is anything happenning at the moment.");



            //forButton.SetToolTip(beginnerLevelCheckBox, "Change application user level to the easiest - beginner.");
            //forButton.SetToolTip(advancedLevelCheckBox, "Change application user level to the one with most functions - advanced.");
            //forButton.SetToolTip(expertLevelCheckBox, "Change application user level to the one with access to all settings - expert.");

            //forButton.SetToolTip(languageComboBox,"This drop-down list allows to change language used by program \r\nsimply click on that down arrow and select best option.");
            //forButton.SetToolTip(showPreviewButton, "This button shows a preview window,\r\nwhich shows how the program would look like on the LED screen.");
            //forButton.SetToolTip(showSettingsButton, "Press this button to open an advanced network settings window.");

            //forButton.SetToolTip(addButton,"Press this button to add program, window or item to your playbill\r\nNew item'll be added depending on selected piece of a tree.");
            //forButton.SetToolTip(deleteButton,"Press this button to delete selected program, window or item from playbill.");
            //forButton.SetToolTip(upButton, "Press this button to move up selected program, window or item in playbill hierarchy.");
            //forButton.SetToolTip(downButton, "Press this button to move down selected program, window or item in playbill hierarchy.");

            //forButton.SetToolTip(playbillWidthTextBox, "The width of LED screen, that will display programs");
            //forButton.SetToolTip(playbillHeightTextBox, "The height of LED screen, that will display programs");
            //forButton.SetToolTip(playbillColorComboBox, "The available colors used by LED screen, that will display programs\r\ndifferent screens could use different pallets of colours.");
            //forButton.SetToolTip(useDeaultsForPlaybillCheckBox, "If this item is checked width, height and colors will return to default\r\nadjusted to screen which with you recieved this program.");

            //forButton.SetToolTip(programNameTextBox,"The name of currently selected program.");
            //forButton.SetToolTip(programWidthTextBox,"The width of currently selected program\r\nmust be the same as screen width.");
            //forButton.SetToolTip(programHeightTextBox, "The height of currently selected program\r\nmust be the same as screen height.");
            //forButton.SetToolTip(runningCheckBox, "If this item is checked, program will be played only at specified days and hours of the week.");
            //forButton.SetToolTip(runningDateTimeBegin, "The hour and minute when screen will start displaying selected program.");
            //forButton.SetToolTip(runningDateTimeEnd, "The hour and minute when screen will stop playing selected program.");
            //forButton.SetToolTip(runningDayscheckedListBox, "Program'll be played only at checked days of the week.");

            //forButton.SetToolTip(windowXTextBox, "The x position of left edge of the window in program \r\nIt should be divisible by 8.");
            //forButton.SetToolTip(windowYTextBox, "The y position of left edge of the window in program.");
            //forButton.SetToolTip(windowWidthTextBox, "The width of the window in program \r\nIt should be divisible by 8.");
            //forButton.SetToolTip(windowHeightTextBox, "The height of the window in program.");
            //forButton.SetToolTip(windowDivideHorizontalButton, "This button divides selected window horizontally \r\ninto two halves or in other possible proportions.");
            //forButton.SetToolTip(windowDivideVerticalButton, "This button divides selected window vertically \r\ninto two halves or in other possible proportions.");
            //forButton.SetToolTip(windowNameTextBox,"The name of currently selected window.");

            //forButton.SetToolTip(bitmaptextBoldButton,"Press this button to change font style to bold.");
            //forButton.SetToolTip(bitmaptextItalicButton,"Press this button to change font style to italic.");
            //forButton.SetToolTip(bitmaptextUnderlineButton,"Press this button to change font style to underline.");
            //forButton.SetToolTip(bitmaptextLeftButton,"This button change text align to left.");
            //forButton.SetToolTip(bitmaptextRightButton,"This button change text align to right.");
            //forButton.SetToolTip(bitmaptextCenterButton,"This button change text align to center.");

            //forButton.SetToolTip(animatorRenderComboBox,"This drop-down list allow to set drawing mode for the screen,\r\nif it should draw at the center, stretch to fit window, zoom, or create mosaic from small images.");
            //forButton.SetToolTip(bitmapRenderComboBox, "This drop-down list allow to set drawing mode for the screen,\r\nif it should draw at the center, stretch to fit window, zoom, or create mosaic from small images.");
            //            forButton.SetToolTip();

        }

        public void ApplyUserLevel(ApplicationUserLevel level)
        {
            System.IO.Stream tmp;
            playbillTree.SelectedNode = playbillTree.Nodes["mainNode"];
            //if(PreviewWindow.programsWithWindows != null && PreviewWindow.programsWithWindows.Count > 0 && previewWindow.ActiveProgram < PreviewWindow.programsWithWindows.Count)
            //    previewWindow.StopProgram(previewWindow.ActiveProgram);

            switch (level)
            {
                case ApplicationUserLevel.Beginner:
                    if (advancedLevelCheckBox.Checked)// || expertLevelCheckBox.Checked)
                    {
                        try
                        {
                            tmp = File.Open(NotBeginnerAutosaveFileName, FileMode.Create);
                            testPlaybill.SaveProgramsToFile(tmp);
                            tmp.Close();
                        }
                        catch { /*System.Diagnostics.Debugger.Break();*/ }
                    }
                    for (int i = 0; i < PreviewWindow.programsWithWindows.Count; i++)
                    {
                        foreach (WindowOnPreview window in PreviewWindow.programsWithWindows[i])
                            window.Clear();
                    }
                    PreviewWindow.programsWithWindows.Clear();
                    PreviewWindow.programsWithWindows.Add(new List<WindowOnPreview>());

                    try
                    {
                        previewWindow.ActiveProgram = 0;
                        previewWindow.ActiveWindow = 0;

                        tmp = File.Open(AutosaveFileName, FileMode.Open);
                        testPlaybill = Playbill.LoadProgramsFromFile(tmp);
                        tmp.Close();
                    }
                    catch
                    {
                        /*System.Diagnostics.Debugger.Break();*/

                        if (CurrentPlaybill == null)
                            CurrentPlaybill = new Playbill();

                        // to be sure that default playbill settings will be loaded
                        useDeaultsForPlaybillCheckBox.Checked = !useDeaultsForPlaybillCheckBox.Checked;
                        if (!useDeaultsForPlaybillCheckBox.Checked)
                            useDeaultsForPlaybillCheckBox.Checked = true;

                        if (CurrentPlaybill.ProgramsList.Count > 0)
                        {
                            CurrentPlaybill.ProgramsList.RemoveRange(1, testPlaybill.ProgramsList.Count - 1);
                            //if (testPlaybill.ProgramsList[0].WindowsList.Count > 1)
                            //    testPlaybill.ProgramsList[0].WindowsList.RemoveRange(1, testPlaybill.ProgramsList[0].WindowsList.Count - 1);
                            CurrentPlaybill.ProgramsList[0].WindowsList.Clear();
                        }
                        else
                            CurrentPlaybill.AddProgram("beginnerProgram");

                        CurrentPlaybill.ProgramsList[0].AddPlayWindow("beginnerWindow");

                        PlayWindow beginnerOnlyWindow = CurrentPlaybill.ProgramsList[0].WindowsList[0];
                        int diamWithSpace = (PreviewWindow.LedDiam + PreviewWindow.Space);
                        PreviewWindow.programsWithWindows[0].Add(
                            new WindowOnPreview(previewWindow, Convert.ToInt32(beginnerOnlyWindow.X), Convert.ToInt32(beginnerOnlyWindow.Y),
                                Convert.ToInt32(beginnerOnlyWindow.Width), Convert.ToInt32(beginnerOnlyWindow.Height), diamWithSpace));


                        bitmaptextRichTextBox.Rtf = "";
                        CurrentPlaybill.ProgramsList[0].WindowsList[0].AddItem(PlayWindowItemType.BitmapText);
                        previewWindow.SelectedWindow.Animations.Add(
                            new PreviewAnimation(previewWindow.SelectedWindow, CurrentPlaybill.ProgramsList[0].WindowsList[0].ItemsList[0], null, (EffectType)(CurrentPlaybill.ProgramsList[0].WindowsList[0].ItemsList[0].Effect),
                                (short)(100 - CurrentPlaybill.ProgramsList[0].WindowsList[0].ItemsList[0].Speed), (short)(100 - CurrentPlaybill.ProgramsList[0].WindowsList[0].ItemsList[0].Stay)));
                    }


                    if (UserLevel != ApplicationUserLevel.Beginner)
                    {
                        playbillTree.Visible = false;
                        playbillToolsGroupBox.Visible = false;
                        settingsTabControl.Left -= playbillTree.Width;
                        this.Width -= playbillTree.Width;
                        poweredByLinkLabel.Left -= playbillTree.Width;
                        showSettingsButton.Visible = false;
                        showProgramSettingsButton.Visible = false;
                    }

                    if (settingsTabControl.TabCount >= 4)
                    {
                        settingsTabControl.TabPages.Clear();
                        foreach (TabPage tab in allSettingsTabs)
                            if (tab.Name == "itemTab")
                                settingsTabControl.TabPages.Add(tab);

                        itemTypeTabControl.TabPages.Clear();
                        foreach (TabPage tab in allItemsTabs)
                            if (tab.Name == "bitmaptextItemTab")
                                itemTypeTabControl.TabPages.Add(tab);
                    }
                    SetupTree();
                    //playbillColorInitialization();
                    playbillTree.SelectedNode = playbillTree.Nodes["mainNode"].Nodes[0].Nodes[0].Nodes[0];
                    this.BackgroundImage = beginnerBackgroundImage;
                    OpenItemTab(testPlaybill.ProgramsList[0].WindowsList[0].ItemsList[0], false);
                    break;
                case ApplicationUserLevel.Advanced:
                    //if (UserLevel == ApplicationUserLevel.Beginner)
                    //if (playbillTree.Visible == false)
                    {
                        //if (CurrentPlaybill == null)
                        //    CurrentPlaybill = new Playbill();
                        try
                        {
                            if (PreviewWindow.programsWithWindows != null && PreviewWindow.programsWithWindows.Count == 1
                                && PreviewWindow.programsWithWindows[0].Count == 1 && PreviewWindow.programsWithWindows[0][0].Animations.Count == 1
                                && PreviewWindow.programsWithWindows[0][0].CurrentAnimationNumber == 0)
                            {
                                PreviewWindow.programsWithWindows[0][0].StopAnimation();

                                PreviewWindow.programsWithWindows[0][0].Clear();
                                PreviewWindow.programsWithWindows.Clear();
                            }
                        }
                        catch { /*System.Diagnostics.Debugger.Break();*/ }

                        if (UserLevel == ApplicationUserLevel.Beginner)
                        {
                            try
                            {
                                tmp = File.Open(AutosaveFileName, FileMode.Create);
                                testPlaybill.SaveProgramsToFile(tmp);
                                tmp.Close();
                            }
                            catch { /*System.Diagnostics.Debugger.Break();*/ }
                        }

                        try
                        {
                            tmp = File.Open(NotBeginnerAutosaveFileName, FileMode.Open);
                            testPlaybill = Playbill.LoadProgramsFromFile(tmp);
                            tmp.Close();
                        }
                        catch
                        {
                            if (CurrentPlaybill == null)
                                CurrentPlaybill = new Playbill();

                            // to be sure that default playbill settings will be loaded
                            useDeaultsForPlaybillCheckBox.Checked = !useDeaultsForPlaybillCheckBox.Checked;
                            if (!useDeaultsForPlaybillCheckBox.Checked)
                                useDeaultsForPlaybillCheckBox.Checked = true;

                            /*System.Diagnostics.Debugger.Break();*/
                        }

                        if (UserLevel == ApplicationUserLevel.Beginner)
                        {
                            playbillTree.Visible = true;
                            playbillToolsGroupBox.Visible = true;
                            this.Width += playbillTree.Width;
                            settingsTabControl.Left += playbillTree.Width;

                            poweredByLinkLabel.Left += playbillTree.Width;

                            showSettingsButton.Visible = true;
                            //showProgramSettingsButton.Visible = true;
                            this.BackgroundImage = backgroundImage;
                        }
                    }

                    if (settingsTabControl.TabCount != 4)
                    {
                        settingsTabControl.TabPages.Clear();
                        foreach (TabPage tab in allSettingsTabs)
                            if (tab.Name != "screenTab")
                                settingsTabControl.TabPages.Add(tab);

                        itemTypeTabControl.TabPages.Clear();
                        itemTypeTabControl.TabPages.AddRange(allItemsTabs);
                    }

                    if (CurrentPlaybill != null)
                    {

                        playbillTree.Nodes.Clear();
                        SetupTree();

                        //if (playbillTree.Nodes[0].Nodes.Count == 1)
                        //{
                        //    playbillTree.SelectedNode = playbillTree.Nodes[0].Nodes[0];
                        //    previewWindow.ActiveProgram = 0;
                        //    Thread.Sleep(100);
                        //    playbillTree.SelectedNode = playbillTree.Nodes[0].Nodes[0].Nodes[0];
                        //    previewWindow.ActiveWindow = 0;
                        //    Thread.Sleep(100);
                        //    playbillTree.SelectedNode = playbillTree.Nodes[0].Nodes[0].Nodes[0].Nodes[0];
                        //}
                    }
                    //playbillColorInitialization();
                    break;
                /*
            case ApplicationUserLevel.Expert:
                if (UserLevel == ApplicationUserLevel.Beginner)
                //if (playbillTree.Visible == false)
                {
                    try
                    {
                        tmp = File.Open(NotBeginnerAutosaveFileName, FileMode.Open);
                        testPlaybill = Playbill.LoadProgramsFromFile(tmp);
                        tmp.Close();
                    }
                    catch
                    {
                    }

                    playbillTree.Visible = true;
                    playbillToolsGroupBox.Visible = true;
                    this.Width += playbillTree.Width;
                    settingsTabControl.Left += playbillTree.Width;

                    poweredByLinkLabel.Left += playbillTree.Width;

                    showSettingsButton.Visible = true;
                    this.BackgroundImage = backgroundImage;
                }

                if (settingsTabControl.TabCount < 5)
                {
                    settingsTabControl.TabPages.Clear();
                    settingsTabControl.TabPages.AddRange(allSettingsTabs);

                    itemTypeTabControl.TabPages.Clear();
                    itemTypeTabControl.TabPages.AddRange(allItemsTabs);
                }

                if (CurrentPlaybill != null)
                {

                    playbillTree.Nodes.Clear();
                    SetupTree();
                }
                break;
                 * */
            }
            if (!changingLevelAtBeginning)
            {
                SetConfigurationSettingAttribute("LedConfiguration/Program/Other/User", "level", level.ToString());
                LoadConfiguration();
            }

            previewWindow.ChangeSize(CurrentPlaybill.Width, CurrentPlaybill.Height);
        }


        #region Language functions
        public void LoadLanguages()
        {
            languageComboBox.Items.Clear();

            if (!Directory.Exists(string.Format("{0}/Languages/", programSettingsFolder)))
                Directory.CreateDirectory(string.Format("{0}/Languages/", programSettingsFolder));
            
            DirectoryInfo di = new DirectoryInfo(string.Format("{0}/Languages/", programSettingsFolder));
            
            FileInfo[] rgFiles = di.GetFiles("*.xml");

            if (rgFiles == null || rgFiles.Length < 1)
            {
                string englishLangFileContent = BengiLED_for_C_Power.Properties.Resources.American_english_language_file;
                string[] tmp = BengiLED_for_C_Power.Properties.Resources.American_english_language_file.Split('\n');

                FileStream fs = File.Create(string.Format("{0}/Languages/American_english.xml", programSettingsFolder));

                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(englishLangFileContent);
                }
                
                rgFiles = di.GetFiles("*.xml");
            }

            flagsList = new ImageList();

            flagsList.ColorDepth = ColorDepth.Depth8Bit;
            flagsList.ImageSize = new Size(16, 12);

            ResourceManager rm = new ResourceManager("BengiLED_for_C_Power.Properties.Resources", Assembly.GetExecutingAssembly());

            foreach (FileInfo fi in rgFiles)
            {
                languageComboBox.Items.Add(fi.Name.Substring(0, fi.Name.IndexOf(fi.Extension)));

                if (rm.GetObject((languageComboBox.Items[languageComboBox.Items.Count - 1]).ToString()) == null)
                    flagsList.Images.Add(rm.GetObject("not_known_flag") as Image);
                else
                    flagsList.Images.Add(rm.GetObject((languageComboBox.Items[languageComboBox.Items.Count - 1]).ToString()) as Image);

                if ((languageComboBox.Items[languageComboBox.Items.Count - 1]).ToString() == "American_english")
                    (languageComboBox.Items[languageComboBox.Items.Count - 1]) = "English (US)";
                else if ((languageComboBox.Items[languageComboBox.Items.Count - 1]).ToString() == "British_english")
                    (languageComboBox.Items[languageComboBox.Items.Count - 1]) = "English (UK)";
            }

            rm.ReleaseAllResources();
            languageComboBox.DropDownHeight = languageComboBox.Items.Count * 16;
        }
        private void ApplyLocalization(Control parentControl)
        {
            //Loop for translating all child controls of parentControl
            foreach (Control c in parentControl.Controls)
            {
                if (c is TabControl)
                {
                    //TabControl doesn't have a text, but its every tab has it's own
                    foreach (TabPage tab in ((TabControl)c).TabPages)
                    {
                        //Translate tab text
                        string localizedText = (string)LangHash[string.Format("{0}.Text", tab.Name)];
                        //rm.GetString(string.Format("{0}.Text", tab.Name));
                        if (localizedText != null)
                            (tab).Text = String.Format("{0}  ", localizedText);


                        //Recursive call to translate all controls in that tab
                        ApplyLocalization((Control)tab);
                    }
                }
                else
                {
                    string localizedText = (string)LangHash[string.Format("{0}.Text", c.Name)];
                    //rm.GetString(string.Format("{0}.Text", c.Name));
                    if (localizedText != null && (c.Name != "previewZoomLabel"))
                        (c).Text = localizedText;

                    if (c.Controls != null && c.Controls.Count > 0)
                        ApplyLocalization(c);
                }
            }

            if (parentControl is Form)
            {
                foreach (Form f in ((Form)parentControl).OwnedForms)
                {
                    ApplyLocalization((Control)f);
                }
            }
        }
        #endregion

        #region Progressbar functions
        public static void SetupProgressBar(ref ProgressBar pb, int max)
        {
            pb.Value = 0;
            pb.Minimum = 0;
            pb.Maximum = max;
        }

        public static void WriteProgress(ref ProgressBar pb)
        {
            if (pb.Value < pb.Maximum)
                pb.Increment(1);

            using (Graphics progressGraphics = pb.CreateGraphics())
            {
                pb.Refresh();
                progressGraphics.DrawString(string.Format("{0}/{1}", pb.Value, pb.Maximum), new Font("Arial", 12, FontStyle.Bold), Brushes.Black,
                        new PointF(pb.Width / 2 - 10, pb.Height / 2 - 7));
            }

            //pb.CreateGraphics().DrawString(string.Format("{0}/{1}", pb.Value, pb.Maximum), new Font("Arial", 12, FontStyle.Bold), Brushes.Black,
            //    new PointF(pb.Width / 2 - 10, pb.Height / 2 - 7));
        }

        public static void WriteProgress(ref ProgressBar pb, int step)
        {
            if (pb.Value < pb.Maximum)
                pb.Increment(step);

            using (Graphics progressGraphics = pb.CreateGraphics())
            {
                pb.Refresh();
                progressGraphics.DrawString(string.Format("{0}/{1}", pb.Value, pb.Maximum), new Font("Arial", 12, FontStyle.Bold), Brushes.Black,
                        new PointF(pb.Width / 2 - 10, pb.Height / 2 - 7));
            }
        }
        #endregion

        #region Backworker functions

        private void InitializeBgWorker()
        {
            this.bgWorker = new BackgroundWorker();
            this.bgWorker.WorkerReportsProgress = true;

            this.bgWorker.DoWork += new DoWorkEventHandler(bgWorker_DoWork);
            this.bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
        }

        void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            alertWindow = new AlertWindow();
            //alertWindow.Owner = this;
            alertWindow.ShowDialog();
        }

        void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //  throw new NotImplementedException();
            //   alertWindow.ShouldCloseNow = true;
        }

        #endregion

        #region Portslist functions
        public void SetupPortsList()
        {
            //  if (bgWorker.IsBusy != true)
            //     bgWorker.RunWorkerAsync();
            //clb = new CheckedListBox();
            //SerialCommunication.CreatePortsList(ref progressBar1, 256);
            SerialCommunication.CreatePortsList(256);
        }

        public int TestCurrentSerialPort()
        {
            int retval = 0;
            CommunicationResult result = CommunicationResult.Failed;

            try
            {
                //AvailablePort defaultPort = new AvailablePort(ConfigHashtable["CurrentComport"].ToString(), int.Parse(ConfigHashtable["CurrentBaudrate"].ToString()));
                SerialCommunication.CurrentSerialPort = new AvailablePort(ConfigHashtable["CurrentComport"].ToString(), int.Parse(ConfigHashtable["CurrentBaudrate"].ToString()));

                result = Communication.TestController();

                if (result == CommunicationResult.Success)
                {
                    SerialCommunication.DevicesLists.Clear();
                    SerialCommunication.DevicesLists.Add(SerialCommunication.CurrentSerialPort);
                    retval = 1;
                }
            }
            catch
            {
                MessageBox.Show("Current port testing failed.");
                return 0;
            }
            SerialCommunication.CurrentSerialPort = new AvailablePort("test", 0);

            return retval;
        }

        public int SetupTestedPorts(bool showSuccessMsg)
        {
            this.Cursor = Cursors.WaitCursor;
            logWindow.Cursor = Cursors.WaitCursor;

            Communication.Type = CommunicationType.Serial;

            int portsCount = TestCurrentSerialPort();

            if (portsCount == 0)
            {
                //CheckedListBox clb = new CheckedListBox();
                SetupPortsList();

                //if (bgWorker.IsBusy != true)
                //    bgWorker.RunWorkerAsync();

                //clb.Items.Clear();
                try
                {
                    portsCount = SerialCommunication.CreateTestedPortsList(ref logWindow, logMessagesHashTable["logMessage_searchingByCOM"].ToString(), ref progressBar1, true);//breakCheckBox.Checked);
                }
                catch
                {
                    MessageBox.Show("Creating comports' list error.");
                    portsCount = 0;
                }
            }

            if (portsCount > 0)// && showSuccessMsg)
            {
                try
                {
                    //alertWindow.ShouldCloseNow = true;
                }
                catch (Exception)
                {
                    //  alertWindow = new AlertWindow();
                    //alertWindow.Owner = this;
                    //  alertWindow.ShouldCloseNow = true;
                }

                if (showSuccessMsg)
                    MessageBox.Show(messageBoxesHashTable["messageBoxMessage_screenWasFound"].ToString(),//"The screen was found", 
                        messageBoxesHashTable["messageBoxTitle_screenWasFound"].ToString(),//"LedConfig - Search Success", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                network = false;
            }
            else if (portsCount == 0)
            {
                Communication.Type = CommunicationType.None;

                try
                {
                    //  alertWindow.ShouldCloseNow = true;
                }
                catch (Exception)
                {
                    //   if(alertWindow == null)
                    //      alertWindow = new AlertWindow();
                    //   alertWindow.ShouldCloseNow = true;
                }
                //if (showSuccessMsg)
                //{
                //    var result = MessageBox.Show("There is no screen connected to serial port.\r\nDo you want to connect via network?",
                //        "BengiLED - Search Failure", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                //    if (result == DialogResult.Yes)
                //        network = true;
                //}
                //else
                //{

                //Should be uncommented in 'show msg boxes version
                //     MessageBox.Show("There is no screen connected to serial port.",
                //         "BengiLED - Search Failure", MessageBoxButtons.OK, MessageBoxIcon.Information);

                network = true;
                //    }
            }
            this.Cursor = Cursors.Default;
            logWindow.Cursor = Cursors.Default;

            return portsCount;
        }
        #endregion

        #region Treeview functions

        public void SetupTree()
        {
            if (playbillTree.Nodes.Count > 0)
                playbillTree.Nodes.Clear();

            TreeNode mainNode = new TreeNode();
            mainNode.Name = "mainNode";
            mainNode.Text = "Playbill";
            mainNode.Tag = "playbill";
            playbillTree.Nodes.Add(mainNode);

            if (PreviewWindow.programsWithWindows != null)
                PreviewWindow.programsWithWindows.Clear();
            else
                PreviewWindow.programsWithWindows = new List<List<WindowOnPreview>>();
            //PreviewWindow.programsWithWindows.Add(new List<WindowOnPreview>());

            playbillColorInitialization();
            
            if (CurrentPlaybill.ProgramsList.Count == 0)
                CurrentPlaybill.AddProgram(string.Format("{0}{1}", "program", 1));

            foreach (PlayProgram program in testPlaybill.ProgramsList)
            {
                if (program.WindowsList.Count == 0)
                    program.AddPlayWindow(string.Format("{0}{1}", "window", 1));

                TreeNode programNode = new TreeNode();
                programNode.Tag = "program";
                programNode.Name = program.Name;
                programNode.Text = program.Name;
                playbillTree.Nodes["mainNode"].Nodes.Add(programNode);

                PreviewWindow.programsWithWindows.Add(new List<WindowOnPreview>());
                previewWindow.ActiveProgram = programNode.Index;

                foreach (PlayWindow window in program.WindowsList)
                {
                    if (window.ItemsList.Count == 0)
                        window.AddItem(ConfigHashtable);

                    TreeNode windowNode = new TreeNode();
                    windowNode.Tag = "window";
                    windowNode.Name = window.Name;
                    windowNode.Text = window.Name;
                    playbillTree.Nodes["mainNode"].Nodes[program.Name].Nodes.Add(windowNode);

                    PreviewWindow.programsWithWindows[programNode.Index].Add(
                        new WindowOnPreview(previewWindow, Convert.ToInt32(window.X), Convert.ToInt32(window.Y),
                            Convert.ToInt32(window.Width), Convert.ToInt32(window.Height),
                            PreviewWindow.LedDiam + PreviewWindow.Space));

                    previewWindow.ActiveWindow = windowNode.Index;

                    foreach (PlayWindowItem item in window.ItemsList)
                    {
                        TreeNode itemNode = new TreeNode();
                        itemNode.Tag = "item";
                        itemNode.Name = item.Text;
                        itemNode.Text = item.ItemType.ToString();
                        playbillTree.Nodes["mainNode"].Nodes[program.Name].Nodes[window.Name].Nodes.Add(itemNode);

                        //List<Bitmap> tmpList = item.BitmapsList; //new List<Bitmap>();
                        //tmpList.Add(new Bitmap("Test.bmp"));

                        int stay = 0;

                        if (item.ItemType == PlayWindowItemType.Animator)
                            stay = item.FrameDelay * 10;
                        else
                            stay = item.Stay;

                        PreviewWindow.programsWithWindows[programNode.Index][windowNode.Index].Animations.Add(
                        new PreviewAnimation(PreviewWindow.programsWithWindows[programNode.Index][windowNode.Index], item, item.BitmapsList, (EffectType)item.Effect,
                            (short)(100 - item.Speed), (short)stay));


                        //playbillTree.SelectedNode = itemNode;
                        if (item.BitmapsList == null || item.BitmapsList.Count < 1)
                            RegenerateItemBitmaps(itemNode);

                        //if (item.BitmapsList == null || item.BitmapsList.Count < 1)
                        //{
                        //    playbillTree.SelectedNode = itemNode;
                        //    OpenItemTab(item, true);
                        //}
                    }

                    //Adding special node, that is used to add new item (text item is a default type added)
                    AddPlaybillNode(playbillTree.Nodes["mainNode"].Nodes[program.Name].Nodes[window.Name], "item");
                }

                //Adding special node, that is used to add new window
                AddPlaybillNode(playbillTree.Nodes["mainNode"].Nodes[program.Name], "window");
            }

            //Adding special node, that is used to add new program
            AddPlaybillNode(playbillTree.Nodes["mainNode"], "program");


            foreach (List<WindowOnPreview> program in PreviewWindow.programsWithWindows)
            {
                foreach (WindowOnPreview window in program)
                {
                    foreach (PreviewAnimation animation in window.Animations)
                    {
                        if (animation.PlaybillItem.ItemType == PlayWindowItemType.BitmapText
                            || animation.PlaybillItem.ItemType == PlayWindowItemType.Bitmap
                            || animation.PlaybillItem.ItemType == PlayWindowItemType.Animator)
                        {
                            if (animation.PlaybillItem.BitmapsForScreen != null)
                            {
                                List<Bitmap> newbmp = new List<Bitmap>();

                                foreach (Bitmap b in animation.PlaybillItem.BitmapsForScreen)
                                {
                                    newbmp.Add(new Bitmap(b));
                                }

                                animation.InputBitmaps = null;
                                animation.InputBitmaps = newbmp;
                                    
                                    
                                //animation.PlaybillItem.BitmapsForScreen.Clear();
                                //animation.PlaybillItem.BitmapsForScreen = null;
                            }
                        }
                    }
                }
            }

            playbillTree.ExpandAll();
            playbillTree.SelectedNode = null;
            playbillTree.SelectedNode = mainNode;
        }

        public void OpenTabForNode(TreeNode node)
        {
            //if (UserLevel == ApplicationUserLevel.Beginner)
            if (settingsTabControl.TabCount == 1)
                return;

            settingsTabControl.SelectTab(string.Format("{0}Tab", node.Tag));

            #region Open tab for item
            if ((string)(node.Tag) == "item")
            {
                int windowNumber = node.Parent.Index;
                int programNumber = node.Parent.Parent.Index;

                previewWindow.ActiveProgram = programNumber;
                previewWindow.ActiveWindow = windowNumber;

                if (node.Name == "new")
                {
                    delete = true;
                    bitmaptextRichTextBox.Rtf = "";
                    delete = false;

                    testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].AddItem(ConfigHashtable);
                    PlayWindowItem tmpItem = testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList.Count - 1];
                    previewWindow.SelectedWindow.Animations.Add(
                        new PreviewAnimation(previewWindow.SelectedWindow, tmpItem, null, (EffectType)tmpItem.Effect, (short)(100 - tmpItem.Speed), (short)tmpItem.Stay));

                    node.Text = string.Format("{0}", "BitmapText", (node.Index + 1));
                    node.Name = string.Format("[{0}{1}]", node.Tag, (node.Index + 1));

                    AddPlaybillNode(node.Parent, node.Tag.ToString());
                }
                PlayWindowItem item = testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[node.Index];
                previewWindow.SelectedWindow.CurrentAnimationNumber = node.Index;

                OpenItemTab(item, false);
            }
            #endregion
            #region Open tab for window
            else if ((string)(node.Tag) == "window")
            {
                changingTab = true;
                int programNumber = node.Parent.Index;

                previewWindow.ActiveProgram = programNumber;
                previewWindow.ActiveWindow = node.Index;

                if (node.Name == "new")
                {
                    if (testPlaybill.ProgramsList[programNumber].WindowsList.Count == 10)
                    {
                        MessageBox.Show(messageBoxesHashTable["messageBoxMessage_only10Windows"].ToString(), "LedConfig",
                                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                        Color tmpColor = Color.Empty;
                        if (playbillTree.SelectedNode.BackColor != tmpColor)
                            playbillTree.SelectedNode.BackColor = tmpColor;

                        playbillTree.SelectedNode = playbillTree.Nodes[0];
                        return;
                    }

                    testPlaybill.ProgramsList[programNumber].AddPlayWindow(string.Format("{0}{1}", node.Tag, (node.Index + 1)));

                    PlayWindow tmpWindow = testPlaybill.ProgramsList[programNumber].WindowsList[node.Index];
                    PreviewWindow.programsWithWindows[programNumber].Add(
                        new WindowOnPreview(previewWindow, Convert.ToInt32(tmpWindow.X), Convert.ToInt32(tmpWindow.Y),
                            Convert.ToInt32(tmpWindow.Width), Convert.ToInt32(tmpWindow.Height),
                            PreviewWindow.LedDiam + PreviewWindow.Space));

                    node.Text = string.Format("{0}{1}", node.Tag, (node.Index + 1));
                    node.Name = string.Format("{0}{1}", node.Tag, (node.Index + 1));

                    //Adding node for creating new items in that just created window
                    AddPlaybillNode(node, "item");

                    //playbillTree.SelectedNode = node.FirstNode;
                    //OpenTabForNode(node.FirstNode);

                    //Adding node for creating new windows
                    AddPlaybillNode(node.Parent, node.Tag.ToString());
                }
                PlayWindow window = testPlaybill.ProgramsList[programNumber].WindowsList[node.Index];

                windowNameTextBox.Text = window.Name;

                windowXTextBox.Text = window.X.ToString();
                windowYTextBox.Text = window.Y.ToString();
                windowWidthTextBox.Text = window.Width.ToString();
                windowHeightTextBox.Text = window.Height.ToString();

                windowWaitingModeComboBox.SelectedIndex = window.Waiting;


                if (previewWindow.SelectedWindow.Animations.Count > 0)
                {
                    previewWindow.SelectedWindow.CurrentAnimationNumber = 0;
                    previewWindow.DrawStaticWindows();
                    //PreviewWindow.programsWithWindows[previewWindow.ActiveProgram][previewWindow.ActiveWindow].Animations[PreviewWindow.programsWithWindows[previewWindow.ActiveProgram][previewWindow.ActiveWindow].CurrentAnimation].ShowStatic();
                    //PreviewWindow.programsWithWindows[previewWindow.ActiveProgram][previewWindow.ActiveWindow].Redraw();
                }
                changingTab = false;
            }
            #endregion
            #region Open tab for program
            else if ((string)(node.Tag) == "program")
            {
                if (node.Name == "new")
                {
                    if (testPlaybill.ProgramsList.Count == 17)
                    {
                        MessageBox.Show("Only 17 programs in playbill allowed.");
                        playbillTree.SelectedNode = playbillTree.Nodes[0];
                        return;
                    }

                    testPlaybill.AddProgram(string.Format("{0}{1}", node.Tag, (node.Index + 1)));
                    PreviewWindow.programsWithWindows.Add(new List<WindowOnPreview>());

                    node.Text = string.Format("{0}{1}", node.Tag, (node.Index + 1));
                    node.Name = string.Format("{0}{1}", node.Tag, (node.Index + 1));

                    //Adding node for creating new windows in that just created program
                    AddPlaybillNode(node, "window");

                    //playbillTree.SelectedNode = node.FirstNode;
                    //OpenTabForNode(node.FirstNode);

                    //Adding node for creating new programs
                    AddPlaybillNode(node.Parent, node.Tag.ToString());
                }
                PlayProgram program = testPlaybill.ProgramsList[node.Index];

                programNameTextBox.Text = program.Name;

                programWidthTextBox.Text = program.Width.ToString();
                programHeightTextBox.Text = program.Height.ToString();


                programRepeatNumericUpDown.Value = program.RepeatTimes;

                runningCheckBox.Checked = program.LTP;
                runningDateTimeBegin.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, (int)program.BeginHour, (int)program.BeginMinute, 0);
                runningDateTimeEnd.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, (int)program.EndHour, (int)program.EndMinute, 0);

                for (int i = 0; i < program.WeekDays.Length; ++i)
                    runningDayscheckedListBox.SetItemChecked(i, program.WeekDays[i]);

                //previewWindow.ActiveProgram = node.Index;
                if (PreviewWindow.programsWithWindows.Count > 0)
                {
                    if (previewWindow.ActiveProgram < 0)
                        previewWindow.ActiveProgram = 0;
                    if (PreviewWindow.programsWithWindows[previewWindow.ActiveProgram].Count > 0)
                        previewWindow.ActiveWindow = 0;
                    previewWindow.PlayNewActiveProgram(node.Index);
                    //previewWindow.DrawStaticWindows();
                }
            }
            #endregion
            #region Open tab for playbill
            else if ((string)(node.Tag) == "playbill")
            {
                //SetupPortsList(ref portsCheckedListBox);

                //if (node.Nodes.Count == 1)
                {
                    //playbillTree.SelectedNode = node.FirstNode;
                    //OpenTabForNode(node.FirstNode);
                }

                playbillWidthTextBox.Text = testPlaybill.Width.ToString();
                playbillHeightTextBox.Text = testPlaybill.Height.ToString();

                ChangeSelectedPlaybillColor();

            }
            #endregion
        }

        public void ChangeSelectedPlaybillColor()
        {
            switch (CurrentPlaybill.Color)
            {
                case 1: // Red
                    playbillColorComboBox.SelectedIndex = 0;
                    playbillColorComboBox.SelectedValue = 0;
                    break;
                case 2: // Green (Lime)
                    playbillColorComboBox.SelectedIndex = 1;
                    playbillColorComboBox.SelectedValue = 1;
                    break;
                case 4: // Blue
                    playbillColorComboBox.SelectedIndex = 2;
                    playbillColorComboBox.SelectedValue = 2;
                    break;
                case 3: // RG
                    playbillColorComboBox.SelectedIndex = 3;
                    playbillColorComboBox.SelectedValue = 3;
                    break;
                case 5: // RB
                    playbillColorComboBox.SelectedIndex = 4;
                    playbillColorComboBox.SelectedValue = 4;
                    break;
                case 6: // GB
                    playbillColorComboBox.SelectedIndex = 5;
                    playbillColorComboBox.SelectedValue = 5;
                    break;
                case 7: //RGB
                    playbillColorComboBox.SelectedIndex = 6;
                    playbillColorComboBox.SelectedValue = 6;
                    break;
                /*
            case 0x77: //full color
                playbillColorComboBox.SelectedIndex = 3;
                playbillColorComboBox.SelectedValue = 3;
                break;
                 */
            }
        }

        //Adding special node, that is used to add new item to the parent
        public void AddPlaybillNode(TreeNode parent, string tag)
        {
            TreeNode newNode = new TreeNode();
            newNode.Tag = tag;
            newNode.Name = "new";
            newNode.Text = string.Format("*new {0}*", tag.ToString());

            parent.Nodes.Add(newNode);
            if (parent.Name != "mainNode")
                parent.Expand();
        }

        public void RemovePlaybillNode(TreeNode node)
        {
            delete = true;
            switch (node.Tag.ToString())
            {
                case "program":
                    testPlaybill.ProgramsList.RemoveAt(node.Index);
                    foreach (WindowOnPreview window in PreviewWindow.programsWithWindows[node.Index])
                        window.Clear();
                    PreviewWindow.programsWithWindows.RemoveAt(node.Index);

                    break;
                case "window":
                    testPlaybill.ProgramsList[node.Parent.Index].WindowsList.RemoveAt(node.Index);

                    PreviewWindow.programsWithWindows[node.Parent.Index][node.Index].Clear();
                    PreviewWindow.programsWithWindows[node.Parent.Index].RemoveAt(node.Index);

                    break;
                case "item":
                    testPlaybill.ProgramsList[node.Parent.Parent.Index].WindowsList[node.Parent.Index].ItemsList.RemoveAt(node.Index);

                    PreviewWindow.programsWithWindows[node.Parent.Parent.Index][node.Parent.Index].Animations[node.Index].Clear();
                    PreviewWindow.programsWithWindows[node.Parent.Parent.Index][node.Parent.Index].Animations.RemoveAt(node.Index);
                    break;
            }
            if (node.Parent.Nodes.Count > 2)
                playbillTree.SelectedNode = node.Parent;
            node.Remove();
        }

        public void ClearTree(Playbill bill)
        {
            previewWindow.ClearProgramsList();

            playbillTree.Nodes.Clear();
        }

        #endregion


        public void OpenItemTab(PlayWindowItem item, bool loadingTree)
        {
            Cursor currentCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;

            if (regeneratePreviewTimer != null)
            {
                regeneratePreviewTimer.Stop();
                regeneratePreviewTimer.Dispose();
                regeneratePreviewTimer = null;
            }

            itemChanging = true;
            itemTypeTabControl.SelectTab(string.Format("{0}ItemTab", item.ItemType.ToString().ToLower()));

            bool breakLoop = false;
            if (item.ItemType == PlayWindowItemType.BitmapText)
            {
                foreach (TreeNode groupNode in bitmapTextEffectsTreeView.Nodes)
                {
                    foreach (TreeNode node in groupNode.Nodes)
                    {
                        if (item.Effect == int.Parse(node.Tag.ToString()))
                        {
                            //bitmapTextEffectsTreeView.HideSelection = false;
                            currentEffectLabel.Text = node.Text;
                            bitmapTextEffectsTreeView.SelectedNode = groupNode;
                            bitmapTextEffectsTreeView.SelectedNode.Expand();
                            bitmapTextEffectsTreeView.SelectedNode = node;

                            breakLoop = true;
                            break;
                        }
                    }
                    if (breakLoop == true)
                        break;
                }

                if (!breakLoop) // if current effect was not found
                    currentEffectLabel.Text = "unknown";
            }
            else if (item.ItemType == PlayWindowItemType.Bitmap)
            {
                foreach (TreeNode groupNode in effectsTreeView.Nodes)
                {
                    foreach (TreeNode node in groupNode.Nodes)
                    {
                        if (item.Effect == int.Parse(node.Tag.ToString()))
                        {
                            //effectsTreeView.HideSelection = false;
                            currentBitmapEffectLabel.Text = node.Text;
                            effectsTreeView.SelectedNode = groupNode;
                            effectsTreeView.SelectedNode.Expand();
                            effectsTreeView.SelectedNode = node;
                            breakLoop = true;
                            break;
                        }
                    }
                    if (breakLoop == true)
                        break;
                }

                if (!breakLoop) // if current effect was not found
                    currentBitmapEffectLabel.Text = "unknown";
            }

            //currentEffectLabel.Text = ((EffectType)item.Effect).ToString();

            if (item.ItemType.ToString().ToLower() == "text")
            {
                textTextBox.Text = item.Text;
                textFontSizeComboBox.SelectedItem = (object)item.FontSize.ToString();
                if (item.Speed >= 95)
                {

                    textEffectSpeedTrackBar.Value = item.Speed - 90;
                }
                else
                {
                    switch (item.Speed)
                    {
                        case 93: textEffectSpeedTrackBar.Value = 4; break;
                        case 90: textEffectSpeedTrackBar.Value = 3; break;
                        case 85: textEffectSpeedTrackBar.Value = 2; break;
                        case 78: textEffectSpeedTrackBar.Value = 1; break;
                    }
                }
                textStayTimeUpDown.Value = item.Stay;
                textEffectComboBox.SelectedIndex = (item.Effect == 63) ? 0 : item.Effect;
            }
            else if (item.ItemType.ToString().ToLower() == "bitmap")
            {
                bitmapTextBox.Text = item.Text;
                if ((100 - item.Speed) >= 95)
                {
                    bitmapEffectSpeedTrackBar.Value = (item.Speed == 0) ? 10 : (10 - item.Speed);
                }
                else
                {
                    switch (100 - item.Speed)
                    {
                        case 93: bitmapEffectSpeedTrackBar.Value = 4; break;
                        case 90: bitmapEffectSpeedTrackBar.Value = 3; break;
                        case 85: bitmapEffectSpeedTrackBar.Value = 2; break;
                        case 78: bitmapEffectSpeedTrackBar.Value = 1; break;
                    }
                }
                bitmapStayTimeUpDown.Value = item.Stay;
                //bitmapEffectComboBox.SelectedIndex = (item.Effect == 63) ? 0 : item.Effect;
                bitmapRenderComboBox.SelectedIndex = item.Mode;
                bitmapTransparentCheckBox.Checked = item.Transparent;

                if (!string.IsNullOrEmpty((bitmapTextBox).Text))
                {
                    previewWindow.SelectedWindow.Animations[previewWindow.SelectedWindow.CurrentAnimationNumber].InputBitmaps
                        = new List<Bitmap>() { new Bitmap((bitmapTextBox).Text) };
                }
                else if (!string.IsNullOrEmpty(item.Text))
                {
                    previewWindow.SelectedWindow.Animations[previewWindow.SelectedWindow.CurrentAnimationNumber].InputBitmaps
                        = new List<Bitmap>() { new Bitmap(item.Text) };
                }

                item.BitmapsList = previewWindow.SelectedWindow.Animations[previewWindow.SelectedWindow.CurrentAnimationNumber].InputBitmaps;
            }
            else if (item.ItemType.ToString().ToLower() == "bitmaptext")
            {
                delete = true; // an auxiliary variable to determine that selectedItemChange methods should not be runned this time

                //bitmaptextRichTextBox.Text = item.Text;
                if (string.IsNullOrEmpty(item.Rtf))
                {
                    // rtb should have always black background
                    bitmaptextRichTextBox.BackColor = Color.FromName(bitmaptextBackgroundComboBox.Items[0].ToString());
                    bitmaptextRichTextBox.Text = item.Text;
                    bitmaptextRichTextBox.Font = new Font(item.FontName, item.FontSize);

                    // setting text background color
                    bitmaptextRichTextBox.SelectAll();
                    try
                    {
                        bitmaptextRichTextBox.SelectionBackColor = Color.FromName(item.BgColor);
                    }
                    catch
                    {
                        bitmaptextRichTextBox.SelectionBackColor = Color.FromName(bitmaptextBackgroundComboBox.Items[0].ToString());
                    }
                    bitmaptextRichTextBox.Select(0, 0);

                    // setting font color
                    try
                    {
                        bitmaptextRichTextBox.ForeColor = Color.FromName(item.FontColor);
                    }
                    catch
                    {
                        bitmaptextRichTextBox.ForeColor = Color.FromName(bitmaptextBackgroundComboBox.Items[1].ToString());
                    }

                }
                else
                    bitmaptextRichTextBox.Rtf = item.Rtf;

                bitmaptextFontSizeComboBox.SelectedItem = (object)item.FontSize.ToString();
                bitmaptextFontComboBox.SelectedItem = (object)item.FontName.ToString();

                try
                {
                    bitmaptextFontColorComboBox.SelectedItem = (object)item.FontColor.ToString();

                    if (bitmaptextFontColorComboBox.SelectedItem == null)
                        bitmaptextFontColorComboBox.SelectedItem = bitmaptextFontColorComboBox.Items[1];
                }
                catch
                {
                    bitmaptextFontColorComboBox.SelectedItem = bitmaptextFontColorComboBox.Items[1];
                }
                bitmapTextSelectedFontColor = bitmaptextBackgroundComboBox.SelectedIndex;

                try
                {
                    bitmaptextBackgroundComboBox.SelectedItem = (object)item.BgColor.ToString();

                    if (bitmaptextBackgroundComboBox.SelectedItem == null)
                        bitmaptextBackgroundComboBox.SelectedItem = bitmaptextBackgroundComboBox.Items[0];
                }
                catch
                {
                    bitmaptextBackgroundComboBox.SelectedItem = bitmaptextBackgroundComboBox.Items[0];
                }
                bitmapTextSelectedBackgroundColor = bitmaptextBackgroundComboBox.SelectedIndex;

                bitmaptextStayTimeUpDown.Value = item.Stay;
                //bitmaptextEffectSpeedUpDown.Value = bitmaptextEffectSpeedUpDown.Maximum - item.Speed;
                if ((100 - item.Speed) >= 95)
                {
                    bitmaptextEffectSpeedTrackBar.Value = (item.Speed == 0) ? 10 : (10 - item.Speed);
                }
                else
                {
                    switch (100 - item.Speed)
                    {
                        case 93: bitmaptextEffectSpeedTrackBar.Value = 4; break;
                        case 90: bitmaptextEffectSpeedTrackBar.Value = 3; break;
                        case 85: bitmaptextEffectSpeedTrackBar.Value = 2; break;
                        case 78: bitmaptextEffectSpeedTrackBar.Value = 1; break;
                    }
                }

                bitmapTextTransparentCheckBox.Checked = item.Transparent;

                //  bitmaptextRichTextBox.BackColor = Color.FromName(item.BgColor);

                delete = false;

                RtbToBitmap(playbillTree.SelectedNode);
            }
            else if (item.ItemType.ToString().ToLower() == "clock")
            {
                clockTextBox.Text = item.Text;
                clockTransparentCheckBox.Checked = item.Transparent;
                dateTimeFormatComboBox.SelectedItem = dateTimeFormatComboBox.Items[item.DateFormatIndex];

                clockFontSizeComboBox.SelectedItem = (object)item.FontSize.ToString();

                try
                {
                    clockFontColorComboBox.SelectedItem = (object)item.FontColor.ToString();
                    if (clockFontColorComboBox.SelectedItem == null)
                    {
                        clockFontColorComboBox.SelectedIndex = 0;
                        clockFontColorComboBox.SelectedItem = clockFontColorComboBox.Items[0];
                    }
                }
                catch
                {
                    clockFontColorComboBox.SelectedIndex = 0;
                    clockFontColorComboBox.SelectedItem = clockFontColorComboBox.Items[0];
                }
                clockFontColor = clockFontColorComboBox.SelectedIndex;

                Boolean[] att = item.Attributes;

                clockYearCheckBox.Checked = att[0];
                clockMonthCheckBox.Checked = att[1];
                clockDayCheckBox.Checked = att[2];
                clockDayCheckBox.Enabled = att[1]; //enable day only if month is checked

                clockHourCheckBox.Checked = att[3];
                clockMinuteCheckBox.Checked = att[4];
                clockMinuteCheckBox.Enabled = att[3]; //enable minutes only if hours are checked
                clockSecondsCheckBox.Checked = att[5];
                clockSecondsCheckBox.Enabled = att[4]; //enable seconds only if minutes are checked

                clockDayOfWeekCheckBox.Checked = att[6];
                
                clockHandsCheckBox.Checked = att[7];
                clockMarksCheckBox.Checked = att[14];

                Clock24hCheckBox.Checked = att[8];
                clock2YcheckBox.Checked = att[9];
                clockMultiLineCheckBox.Checked = att[10];

                clockStayTimeNumericUpDown.Value = item.Stay;

                if (item.FontSize > CurrentWindow.Height / 2)
                {
                    clockMultiLineCheckBox.Checked = false;
                    clockMultiLineCheckBox.Enabled = false;
                }

                item.FontSize = int.Parse((string)clockFontSizeComboBox.SelectedItem);
                item.FontSizeIndex = clockFontSizeComboBox.SelectedIndex;

                GenerateClockBitmap(item);

            }
            else if (item.ItemType.ToString().ToLower() == "temperature")
            {
                temperatureTextBox.Text = item.Text;
                temperatureTransparentCheckBox.Checked = item.Transparent;

                try
                {
                    temperatureFontColorComboBox.SelectedItem = (object)item.FontColor.ToString();

                    if (temperatureFontColorComboBox.SelectedItem == null)
                    {
                        temperatureFontColorComboBox.SelectedIndex = 0;
                        temperatureFontColorComboBox.SelectedItem = temperatureFontColorComboBox.Items[0];
                    }
                }
                catch
                {
                    temperatureFontColorComboBox.SelectedIndex = 0;
                    temperatureFontColorComboBox.SelectedItem = temperatureFontColorComboBox.Items[0];
                }
                temperatureFontColor = temperatureFontColorComboBox.SelectedIndex;
                temperatureStayTimeNumericUpDown.Value = item.Stay;

                temperatureFontSizeComboBox.SelectedItem = (object)item.FontSize.ToString();
                previewWindow.SelectedWindow.Animations[previewWindow.SelectedWindow.CurrentAnimationNumber].Effect = EffectType.Instant;
                previewWindow.SelectedWindow.Animations[previewWindow.SelectedWindow.CurrentAnimationNumber].Speed = 100;
                previewWindow.SelectedWindow.Animations[previewWindow.SelectedWindow.CurrentAnimationNumber].StayTime = (short)temperatureStayTimeNumericUpDown.Value;

                if (string.IsNullOrEmpty(temperatureFontSizeComboBox.Text))
                    temperatureFontSizeComboBox.SelectedItem = 0;


                if (item.TempAtt == 1)
                {
                    temperatureFahrenheitRadioButton.Checked = true;
                    temperatureFontSizeComboBox.Text = temperatureFontSizeComboBox.SelectedText;
                }
                else
                {

                    temperatureCelsiusRadioButton.Checked = true;
                    temperatureFontSizeComboBox.Text = temperatureFontSizeComboBox.SelectedText;
                }

                item.FontSize = int.Parse((string)temperatureFontSizeComboBox.SelectedItem);
                item.FontSizeIndex = temperatureFontSizeComboBox.SelectedIndex;

                //item.BitmapsList = previewWindow.SelectedWindow.Animations[previewWindow.SelectedWindow.CurrentAnimationNumber].InputBitmaps;
                GenerateTemperatureBitmap(item);



                //previewWindow.SelectedWindow.StopAnimation();
                previewWindow.SelectedWindow.Animate(false, previewWindow.SelectedWindow.CurrentAnimationNumber);
            }
            else if (item.ItemType.ToString().ToLower() == "animator")
            {
                animatorSelectTextBox.Text = item.Text;

                animatorRenderComboBox.SelectedIndex = item.Mode;
                animatorRepeatNumericUpDown.Value = item.Stay;
            }


            if (!loadingTree)
            {
                TreeNode selectedItem = playbillTree.SelectedNode;
                if (selectedItem != null && selectedItem.Tag.ToString() == "item")
                {
                    int programNumber = selectedItem.Parent.Parent.Index;
                    int windowNumber = selectedItem.Parent.Index;
                    int animationNumber = selectedItem.Index;

                    previewWindow.SelectedWindow.Animate(false, animationNumber);
                }
            }

            itemChanging = false;
            this.Cursor = currentCursor;
        }

        private void GenerateClockBitmap(PlayWindowItem item)
        {
            CurrentWindowNumber = previewWindow.ActiveWindow;
            CurrentProgramNumber = previewWindow.ActiveProgram;

            #region Format correction
            bool hour = item.Attributes[3];
            bool minute = item.Attributes[4];
            bool second = item.Attributes[5];

            bool year = item.Attributes[0];
            bool month = item.Attributes[1];
            bool day = item.Attributes[2];
            bool dayOfWeek = item.Attributes[6];

            bool use24h = item.Attributes[8];
            bool show2year = item.Attributes[9];
            bool multiline = item.Attributes[10];
            
            previewWindow.SelectedWindow.Animations[previewWindow.SelectedWindow.CurrentAnimationNumber].Effect = EffectType.Instant;
            previewWindow.SelectedWindow.Animations[previewWindow.SelectedWindow.CurrentAnimationNumber].Speed = 100;
            //previewWindow.SelectedWindow.Animations[previewWindow.SelectedWindow.CurrentAnimationNumber].StayTime = (short)temperatureStayTimeNumericUpDown.Value;

            //if (clockFontSizeComboBox.SelectedItem == null)
            //    clockFontSizeComboBox.SelectedItem = "8";

            string dateTime = "";
            bool drawTime = true;


            CultureInfo ci = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            DateTime now = DateTime.Now;
            List<string> dateFormats = new List<string>();
            string selectedFormat = "";

            ////0 = chinese
            ////1 = Day,DD/MM/YYYY HH:MM
            //dateFormats.Add("ddd, dd/MM/yyyy HH:mm:ss");
            ////2 = YYY-MM-DD Day. HH:MM
            //dateFormats.Add("yyyy-MM-dd ddd. HH:mm:ss");
            ////3 = Daylong, DD Month YYYY HH:MM
            //dateFormats.Add("dddd, dd MMMM yyyy HH:mm:ss");
            ////4 = Day, Mon DD, YYYY HH:MM
            //dateFormats.Add("ddd, MMM dd, yyyy HH:mm:ss");
            ////5 = Daylong,  Month DD,YYYY HH:MM
            //dateFormats.Add("dddd, MMMM dd, yyyy HH:mm:ss");
            ////6 = Day,M/DD/YYYY HH:MM
            //dateFormats.Add("ddd, M/dd/yyyy HH:mm:ss");
            ////7 = YYYY/MM/DD, Day. HH:MM
            //dateFormats.Add("yyyy/MM/dd, ddd HH:mm:ss");

            //0 = chinese
            //1 = Day,DD/MM/YYYY HH:MM
            dateFormats.Add("ddd,dd/MM/yyyy HH:mm:ss");
            //2 = YYY-MM-DD Day. HH:MM
            dateFormats.Add("yyyy-MM-dd ddd. HH:mm:ss");
            //3 = Daylong, DD Month YYYY HH:MM
            dateFormats.Add("ddd,dd MMM yyyy HH:mm:ss");
            //4 = Day, Mon DD, YYYY HH:MM
            dateFormats.Add("ddd,MMM dd,yyyy HH:mm:ss");
            //5 = Day,  Mon DD,YYYY HH:MM
            dateFormats.Add("ddd,MMM dd,yyyy HH:mm:ss");
            //6 = Day,M/DD/YYYY HH:MM
            dateFormats.Add("ddd,M/dd/yyyy HH:mm:ss");
            //7 = YYYY/MM/DD, Day. HH:MM
            dateFormats.Add("yyyy/MM/dd ddd. HH:mm:ss");

            selectedFormat = dateFormats[item.DateFormatIndex];

            if (year)
            {
                if (show2year)
                    selectedFormat = selectedFormat.Replace("yyyy", "yy");
            }
            else
            {
                if (selectedFormat.StartsWith("yyyy"))
                    selectedFormat = selectedFormat.Remove(0, 5);
                else
                    selectedFormat = selectedFormat.Remove(selectedFormat.IndexOf("yyyy"), 5);
            }

            if (!month)
            {
                if (selectedFormat.Contains("MMMM"))
                    selectedFormat = selectedFormat.Remove(selectedFormat.IndexOf("MMMM"), 5);
                else if (selectedFormat.Contains("MMM"))
                    selectedFormat = selectedFormat.Remove(selectedFormat.IndexOf("MMM"), 4);
                else if (selectedFormat.Contains("MM"))
                    selectedFormat = selectedFormat.Remove(selectedFormat.IndexOf("MM"), 3);
                else if (selectedFormat.Contains("M"))
                    selectedFormat = selectedFormat.Remove(selectedFormat.IndexOf("M"), 2);
            }

            if (!dayOfWeek)
            {
                if (selectedFormat.Contains("dddd,"))
                    selectedFormat = selectedFormat.Remove(selectedFormat.IndexOf("dddd,"), 5);
                else if (selectedFormat.Contains("ddd,"))
                    selectedFormat = selectedFormat.Remove(selectedFormat.IndexOf("ddd,"), 4);
                else if (selectedFormat.Contains("ddd."))
                    selectedFormat = selectedFormat.Remove(selectedFormat.IndexOf("ddd."), 4);
                else if (selectedFormat.Contains("ddd"))
                    selectedFormat = selectedFormat.Remove(selectedFormat.IndexOf("ddd"), 3);
            }

            if (!day)
            {
                if (selectedFormat.Contains("dd/"))
                    selectedFormat = selectedFormat.Remove(selectedFormat.IndexOf("dd/"), 3);
                else if (selectedFormat.Contains("/dd"))
                    selectedFormat = selectedFormat.Remove(selectedFormat.IndexOf("/dd"), 3);
                else if (selectedFormat.Contains(",dd"))
                    selectedFormat = selectedFormat.Remove(selectedFormat.IndexOf(",dd"), 3);
                else if (selectedFormat.Contains("-dd"))
                    selectedFormat = selectedFormat.Remove(selectedFormat.IndexOf("-dd"), 3);
                else if (selectedFormat.StartsWith("dd,"))
                    selectedFormat = selectedFormat.Remove(selectedFormat.IndexOf("dd,"), 3);
                else if (selectedFormat.Contains(" dd"))
                    selectedFormat = selectedFormat.Remove(selectedFormat.IndexOf(" dd"), 3);
                else if (selectedFormat.StartsWith("dd "))
                    selectedFormat = selectedFormat.Remove(selectedFormat.IndexOf("dd "), 3);
            }


            if ((!year && !month && !day))
            {
                selectedFormat = selectedFormat.Replace("/", "");
                selectedFormat = selectedFormat.Replace("-", "");                
            }


            if ((hour || minute || second))
            {
                if (multiline && (year || month || day || dayOfWeek))
                {
                    //if (int.Parse(clockFontSizeComboBox.SelectedItem.ToString()) * 2 <= CurrentPlaybill.Height)
                    if (item.FontSize * 2 <= CurrentWindow.Height)
                    {
                        selectedFormat = selectedFormat.Replace(" HH", "\r\nHH");

                        drawTime = true;
                    }
                    else
                        drawTime = false;
                }
            }
            else
                drawTime = false;

            //if (!drawTime)
            {
                if (!hour)
                {
                    if (selectedFormat.Contains("HH:"))
                        selectedFormat = selectedFormat.Remove(selectedFormat.IndexOf("HH:"), 3);
                }
                else if (hour && !use24h)
                {
                    selectedFormat = string.Format("{0} tt", selectedFormat.Replace("HH", "h"));
                }

                if (!minute)
                {
                    if (selectedFormat.Contains("mm:"))
                        selectedFormat = selectedFormat.Remove(selectedFormat.IndexOf("mm:"), 3);
                }
                if (!second)
                {
                    if (selectedFormat.Contains(":ss"))
                        selectedFormat = selectedFormat.Remove(selectedFormat.IndexOf(":ss"), 3);
                    else if (selectedFormat.Contains("ss"))
                        selectedFormat = selectedFormat.Remove(selectedFormat.IndexOf("ss"), 2);
                }
            }

            selectedFormat = selectedFormat.Replace(",,", ",");
            selectedFormat = selectedFormat.Replace("  ", " ");
            selectedFormat = selectedFormat.Replace("::", ":");

            if (selectedFormat.EndsWith(" "))
                selectedFormat = selectedFormat.Remove(selectedFormat.Length - 1);
            if (selectedFormat.EndsWith(",") || selectedFormat.EndsWith(" ") || selectedFormat.EndsWith(":") || selectedFormat.EndsWith("/"))
                selectedFormat = selectedFormat.Remove(selectedFormat.Length - 1);
            if (selectedFormat.StartsWith(" ") || selectedFormat.StartsWith(".") || selectedFormat.StartsWith(",") || selectedFormat.StartsWith("/"))
                selectedFormat = selectedFormat.Remove(0, 1);
            #endregion

            if (!(string.IsNullOrEmpty(selectedFormat) || selectedFormat.Equals(" ")))
            {
                
                previewWindow.SelectedWindow.Animations[previewWindow.SelectedWindow.CurrentAnimationNumber].InputBitmaps
                        = new List<Bitmap>() { DrawMarkFont(now.ToString(selectedFormat), item.FontSize,  Color.FromName(item.FontColor)) };
                
            }
            else
            {
                previewWindow.SelectedWindow.Animations[previewWindow.SelectedWindow.CurrentAnimationNumber].InputBitmaps
                    = new List<Bitmap>() { CreateBitmapImage(" ",  new Font("Batang", item.FontSize),  
                                Color.FromName(item.FontColor)) };
            }

            item.BitmapsList = previewWindow.SelectedWindow.Animations[previewWindow.SelectedWindow.CurrentAnimationNumber].InputBitmaps;


            //previewWindow.SelectedWindow.StopAnimation();
            previewWindow.SelectedWindow.Animate(false, previewWindow.SelectedWindow.CurrentAnimationNumber);


            Thread.CurrentThread.CurrentCulture = ci;
        }

        private void GenerateTemperatureBitmap(PlayWindowItem item)
        {
            CurrentWindowNumber = previewWindow.ActiveWindow;
            CurrentProgramNumber = previewWindow.ActiveProgram;
            string tempText = string.Empty;

            if (item.TempAtt == 1)
                tempText = "53.4°F";
            else
                tempText = "21.4°C";

            previewWindow.SelectedWindow.Animations[previewWindow.SelectedWindow.CurrentAnimationNumber].InputBitmaps
                    = new List<Bitmap>() { DrawMarkFont(tempText, item.FontSize, Color.FromName(item.FontColor)) };
            

            item.BitmapsList = previewWindow.SelectedWindow.Animations[previewWindow.SelectedWindow.CurrentAnimationNumber].InputBitmaps;

            previewWindow.SelectedWindow.Animate(false, previewWindow.SelectedWindow.CurrentAnimationNumber);
        }
        
        #region Save & Load functions

        private void progSavButton_Click(object sender, EventArgs e)
        {
            System.IO.Stream filestream;

            SaveFileDialog savefile1 = new SaveFileDialog();
            if (userLevel == ApplicationUserLevel.Beginner)
            {
                savefile1.Filter = "program files (*.bpr)|*.bpr";
                savefile1.FilterIndex = 1;
                savefile1.DefaultExt = "bpr";
            }
            else
            {
                savefile1.Filter = "program files (*.apr)|*.apr";
                savefile1.FilterIndex = 1;
                savefile1.DefaultExt = "apr";
            }

            if (ConfigHashtable["PathsSaveProgram"].ToString() != "Empty")
                savefile1.InitialDirectory = ConfigHashtable["PathsSaveProgram"].ToString();

            if (savefile1.ShowDialog() == DialogResult.OK)
                try
                {
                    if ((filestream = savefile1.OpenFile()) != null)
                    {
                        testPlaybill.SaveProgramsToFile(filestream);
                        filestream.Close();

                        string progPath = savefile1.FileName.Remove(savefile1.FileName.LastIndexOf('\\'));

                        SetConfigurationSettingText("LedConfiguration/Program/Other/Paths/SaveProgram", progPath);
                        LoadConfiguration();
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show(messageBoxesHashTable["messageBoxMessage_couldNotOpenFile"].ToString());
                }

        }

        private void progLoadButton_Click(object sender, EventArgs e)
        {
            System.IO.Stream filestream;

            OpenFileDialog openFile1 = new OpenFileDialog();
            openFile1.RestoreDirectory = true;

            if (ConfigHashtable["PathsLoadProgram"].ToString() != "Empty")
                openFile1.InitialDirectory = ConfigHashtable["PathsLoadProgram"].ToString();

            if (userLevel == ApplicationUserLevel.Beginner)
            {
                openFile1.Filter = "program files (*.bpr)|*.bpr";
                openFile1.FilterIndex = 1;
            }
            else
            {
                openFile1.Filter = "advanced program files (*.apr)|*.apr|basic program files (*.bpr)|*.bpr";
                openFile1.FilterIndex = 1;
            }

            if (openFile1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if ((filestream = openFile1.OpenFile()) != null)
                    {
                        string progPath = openFile1.FileName.Remove(openFile1.FileName.LastIndexOf('\\'));

                        SetConfigurationSettingText("LedConfiguration/Program/Other/Paths/LoadProgram", progPath);
                        LoadConfiguration();

                        Playbill tmp = testPlaybill;

                        testPlaybill = Playbill.LoadProgramsFromFile(filestream);
                        filestream.Close();

                        ClearTree(tmp);
                        SetupTree();

                        if (userLevel == ApplicationUserLevel.Beginner)
                            OpenItemTab(testPlaybill.ProgramsList[playbillTree.Nodes["mainNode"].Nodes[0].Index].WindowsList[playbillTree.Nodes["mainNode"].Nodes[0].Nodes[0].Index].ItemsList[playbillTree.Nodes["mainNode"].Nodes[0].Nodes[0].Nodes[0].Index], false);
                        //playbillTree.SelectedNode = playbillTree.Nodes["mainNode"].Nodes[0].Nodes[0].Nodes[0];
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show(messageBoxesHashTable["messageBoxMessage_couldNotOpenFile"].ToString());
                }
            }

        }

        private string SelectBMPFile()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.RestoreDirectory = true;
            if (ConfigHashtable["PathsLoadBMP"].ToString() != "Empty")
                dialog.InitialDirectory = ConfigHashtable["PathsLoadBMP"].ToString();
            dialog.Filter =
                "Images (*.jpg;*.bmp;*.png)|*.jpg;*.bmp;*.png";
            dialog.Title = "Select image file";
            return (dialog.ShowDialog() == DialogResult.OK)
                ? dialog.FileName : null;
        }

        private string SelectAnimatorFile()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.RestoreDirectory = true;
            if (ConfigHashtable["PathsLoadGIF"].ToString() != "Empty")
                dialog.InitialDirectory = ConfigHashtable["PathsLoadGIF"].ToString();
            dialog.Filter =
               "(*.gif)|*.gif";
            dialog.Title = "Select a gif file";
            return (dialog.ShowDialog() == DialogResult.OK)
               ? dialog.FileName : null;
        }

        private string SelectRTFFile()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.RestoreDirectory = true;
            if (ConfigHashtable["PathsLoadRTF"].ToString() != "Empty")
                dialog.InitialDirectory = ConfigHashtable["PathsLoadRTF"].ToString();
            dialog.Filter =
                "(*.txt)|*.txt|(*.rtf)|*.rtf";
            dialog.Title = "Select a rtf or text file";
            return (dialog.ShowDialog() == DialogResult.OK)
                ? dialog.FileName : null;
        }

        #endregion

        public void ChangeTextSize(int sizeChange, int selectionStart, int selectionLength)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {

                bitmaptextRichTextBox.SelectionStart = selectionStart;
                bitmaptextRichTextBox.SelectionLength = selectionLength;
                Font tmp = bitmaptextRichTextBox.SelectionFont;
                try
                {
                    //tmp.Size+sizeChange
                    bitmaptextRichTextBox.SelectionFont = new Font(tmp.Name, sizeChange, tmp.Style);

                    //int selectionIndex = bitmaptextRichTextBox.SelectionStart;
                    //int selectionLength = bitmaptextRichTextBox.SelectionLength;

                    //RtbToBitmap();

                    //bitmaptextRichTextBox.SelectionStart = selectionStart;
                    //bitmaptextRichTextBox.SelectionLength = selectionLength;
                }
                catch (NullReferenceException)
                {
                    ChangeTextSize(sizeChange, selectionStart, selectionLength / 2);
                    ChangeTextSize(sizeChange, selectionStart + selectionLength / 2, selectionLength - selectionLength / 2);
                    //MessageBox.Show("Unable to change font size");
                }
            }
        }

        public void ChangeFont(string newFont, int selectionStart, int selectionLength)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                bitmaptextRichTextBox.SelectionStart = selectionStart;
                bitmaptextRichTextBox.SelectionLength = selectionLength;

                Font tmp = bitmaptextRichTextBox.SelectionFont;
                try
                {
                    bitmaptextRichTextBox.SelectionFont = new Font(newFont, tmp.Size, tmp.Style);
                }
                catch (NullReferenceException)
                {
                    ChangeFont(newFont, selectionStart, selectionLength / 2);
                    ChangeFont(newFont, selectionStart + selectionLength / 2, selectionLength - selectionLength / 2);
                }
                catch (ArgumentException)
                {
                    FontFamily tmpFamily = new FontFamily(newFont);
                    if (tmpFamily.IsStyleAvailable(FontStyle.Regular))
                        bitmaptextRichTextBox.SelectionFont = new Font(newFont, tmp.Size);
                    else if (tmpFamily.IsStyleAvailable(FontStyle.Bold))
                        bitmaptextRichTextBox.SelectionFont = new Font(newFont, tmp.Size, FontStyle.Bold);
                    else if (tmpFamily.IsStyleAvailable(FontStyle.Italic))
                        bitmaptextRichTextBox.SelectionFont = new Font(newFont, tmp.Size, FontStyle.Italic);
                    else if (tmpFamily.IsStyleAvailable(FontStyle.Underline))
                        bitmaptextRichTextBox.SelectionFont = new Font(newFont, tmp.Size, FontStyle.Underline);
                    else if (tmpFamily.IsStyleAvailable(FontStyle.Strikeout))
                        bitmaptextRichTextBox.SelectionFont = new Font(newFont, tmp.Size, FontStyle.Strikeout);
                }
            }
        }

        public void FontSizeCheck(int selectionLenght)
        {
            #region Old Function
            //bitmaptextRichTextBox.SelectionChanged -= new EventHandler(bitmaptextRichTextBox_SelectionChanged);

            //if (selectionLenght <= 1)
            //    bitmaptextFontSizeComboBox.SelectedItem = (object)(Convert.ToInt32(bitmaptextRichTextBox.SelectionFont.Size).ToString());
            //else
            //{
            //    int tmpSize;
            //    bitmaptextRichTextBox.Select(selectionStart, 1);
            //    tmpSize = Convert.ToInt32(bitmaptextRichTextBox.SelectionFont.Size);

            //    for (int i = 1 + selectionStart; i < selectionStart + selectionLenght; )
            //    {
            //        bitmaptextRichTextBox.Select(i, 1);
            //        if (tmpSize != Convert.ToInt32(bitmaptextRichTextBox.SelectionFont.Size))
            //        {
            //            bitmaptextFontSizeComboBox.SelectedItem = null;
            //            bitmaptextRichTextBox.Select(selectionStart, selectionLenght);
            //            bitmaptextRichTextBox.SelectionChanged += new EventHandler(bitmaptextRichTextBox_SelectionChanged);
            //            return;
            //        }
            //        ++i;
            //    }
            //    bitmaptextFontSizeComboBox.SelectedItem = (object)(tmpSize.ToString());
            //}

            //bitmaptextRichTextBox.Select(selectionStart, selectionLenght);
            //bitmaptextRichTextBox.SelectionChanged += new EventHandler(bitmaptextRichTextBox_SelectionChanged);
            #endregion

            #region New One
            try
            {
                if (selectionLenght <= 1)
                    bitmaptextFontSizeComboBox.SelectedItem = (object)(Convert.ToInt32(bitmaptextRichTextBox.SelectionFont.Size).ToString());
                else
                {
                    int size;
                    RichTextBox rtb = new RichTextBox();
                    rtb.Rtf = bitmaptextRichTextBox.SelectedRtf;
                    rtb.Select(0, 1);
                    size = Convert.ToInt32(rtb.SelectionFont.Size);

                    for (int i = 1; i < rtb.TextLength; ++i)
                    {
                        rtb.Select(i, 1);
                        if (size != Convert.ToInt32(rtb.SelectionFont.Size))
                        {
                            bitmaptextFontSizeComboBox.SelectedItem = null;
                            break;
                        }
                    }

                    if (bitmaptextFontSizeComboBox.SelectedItem != null)
                        bitmaptextFontSizeComboBox.SelectedItem = (object)(size.ToString());
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            #endregion
        }

        #endregion

        #region Eventhandlers

        #region Playbill's events

        private void comDetectButton_Click(object sender, EventArgs e)
        {
            //SetupTestedPorts(ref portsCheckedListBox, true);
        }

        private void playbillSendButton_Click(object sender, EventArgs e)
        {
            if (PreviewWindow.programsWithWindows == null || PreviewWindow.programsWithWindows.Count == 0
                || PreviewWindow.programsWithWindows[0] == null || PreviewWindow.programsWithWindows[0].Count == 0)
            {
                return;
            }

            CommunicationResult result = CommunicationResult.Success;

            int currentProgramNumber = previewWindow.ActiveProgram;
            int currentWindowNumber = previewWindow.ActiveWindow;
            int currentAnimationNumber = previewWindow.SelectedWindow.CurrentAnimationNumber;
            
            
            if(PreviewWindow.programsWithWindows != null & PreviewWindow.programsWithWindows.Count > 0)
            {
                foreach (var p in PreviewWindow.programsWithWindows)
                {
                    foreach (WindowOnPreview w in p)
                    {
                        foreach (PreviewAnimation a in w.Animations)
                            a.PauseAnimation();
                    }
                }
                //for (int i = 0; i < PreviewWindow.programsWithWindows.Count; i++)
                //{
                //    previewWindow.StopProgram(i);
                //}
            }
            else
                currentProgramNumber  = -1;


            //try
            {
                string tempDirectory = string.Format("{0}/temp", programSettingsFolder); //"temp";

                Directory.CreateDirectory(tempDirectory);

                logWindow.Clearlog();
                logWindow.Show();
                
                SearchForScreen();
                
                switch (Communication.Type)
                {
                    case CommunicationType.None:
                        {
                            screenFoundInfoLabel.Text = "NOT FOUND";
                            screenFoundInfoLabel.ForeColor = Color.Red;

                            logWindow.Hide(); 

                            if (currentProgramNumber > -1)
                                previewWindow.PlayGivenProgram(currentProgramNumber);
                
                            return;
                            //break;
                        }
                    case CommunicationType.Serial:
                        {

                            UpdateSerialCommunication();
                            break;
                        }
                    case CommunicationType.Network:
                        {
                            UpdateNetworkCommunication();
                            break;
                        }
                }

                screenFoundInfoLabel.Text = "FOUND";
                screenFoundInfoLabel.ForeColor = Color.Green;

                try
                {

                    foreach (List<WindowOnPreview> program in PreviewWindow.programsWithWindows)
                    {
                        foreach (WindowOnPreview window in program)
                        {
                            foreach (PreviewAnimation animation in window.Animations)
                            {
                                if (animation.PlaybillItem.ItemType == PlayWindowItemType.BitmapText
                                    || animation.PlaybillItem.ItemType == PlayWindowItemType.Bitmap
                                    || animation.PlaybillItem.ItemType == PlayWindowItemType.Animator)
                                {
                                    if (animation.InputBitmaps != null && animation.InputBitmaps.Count > 0)
                                        animation.PlaybillItem.BitmapsForScreen = animation.InputBitmaps;
                                }
                            }
                        }
                    }

                    logWindow.Message = string.Format(logMessagesHashTable["logMessage_savingFiles"].ToString(), CurrentPlaybill.FilesToSend);

                    long freeSpace = 0;
                    result = Communication.GetFreeSpace(out freeSpace);

                    if (result != CommunicationResult.Success)
                        MessageBox.Show(result.ToString());
                    else
                    {
                        long fileSize = CurrentPlaybill.SaveToFile("playbill.lpp", SerialCommunication.CardID);

                        if (freeSpace - fileSize < 0)
                            MessageBox.Show(string.Format("Wolne miejsce: {0}\nRozmiar plików: {1}", freeSpace, fileSize));
                        if (freeSpace - fileSize >= 0)
                        {
                            // number of file that is currently being sent
                            int currentFileNumber = 0;

                            SetupProgressBar(ref progressBar1, (int)fileSize);

                            logWindow.Message = logMessagesHashTable["logMessage_deletingOldFiles"].ToString();
                            result = Communication.DeleteFile("*.*");
                            // wait to let controller clear memory
                            Thread.Sleep(40);

                            if (result != CommunicationResult.Success)
                                MessageBox.Show(result.ToString());
                            else
                            {
                                SerialCommunication.Timeout = 3000;

                                if (File.Exists(string.Format("{0}/{1}", tempDirectory, CurrentPlaybill.FileName)))
                                {
                                    logWindow.Message = string.Format(logMessagesHashTable["logMessage_uploadingFile"].ToString(), ++currentFileNumber, CurrentPlaybill.FilesToSend);
                                    result = Communication.UploadFile(string.Format("{0}/{1}", tempDirectory, CurrentPlaybill.FileName), ("playbill.lpp")
                                        , ref progressBar1);

                                }

                                if (result != CommunicationResult.Success)
                                    result = Communication.Format();
                                else
                                {
                                    if (File.Exists(string.Format("{0}/playbill.lpt", tempDirectory)))
                                    {
                                        logWindow.Message = string.Format(logMessagesHashTable["logMessage_uploadingFile"].ToString(), ++currentFileNumber, CurrentPlaybill.FilesToSend);
                                        result = Communication.UploadFile(string.Format("{0}/playbill.lpt", tempDirectory), "playbill.lpt"
                                            , ref progressBar1);
                                    }

                                    foreach (PlayProgram program in testPlaybill.ProgramsList)
                                    {
                                        if (result != CommunicationResult.Success)
                                            break;

                                        if (File.Exists(string.Format("{0}/{1}", tempDirectory, program.FileName)))
                                        {
                                            logWindow.Message = string.Format(logMessagesHashTable["logMessage_uploadingFile"].ToString(), ++currentFileNumber, CurrentPlaybill.FilesToSend);
                                            result = Communication.UploadFile(string.Format("{0}/{1}", tempDirectory, program.FileName), program.FileName
                                                , ref progressBar1);
                                        }
                                    }

                                    if (result != CommunicationResult.Success)
                                        result = Communication.Format();

                                    logWindow.Message = logMessagesHashTable["logMessage_resetingControllerApp"].ToString();
                                    result = Communication.RestartApp();

                                    if (send2DCheckBox.Checked)
                                        result = Communication.EndSendingFiles();

                                    foreach (List<WindowOnPreview> program in PreviewWindow.programsWithWindows)
                                    {
                                        foreach (WindowOnPreview window in program)
                                        {
                                            foreach (PreviewAnimation animation in window.Animations)
                                            {
                                                if (animation.PlaybillItem.ItemType == PlayWindowItemType.BitmapText
                                                    || animation.PlaybillItem.ItemType == PlayWindowItemType.Bitmap
                                                    || animation.PlaybillItem.ItemType == PlayWindowItemType.Animator)
                                                {
                                                    try
                                                    {
                                                        if (animation.PlaybillItem.BitmapsForScreen != null)
                                                        {
                                                            foreach (Bitmap bmp in animation.PlaybillItem.BitmapsForScreen)
                                                            {
                                                                try
                                                                {
                                                                    if (bmp != null)
                                                                        bmp.Dispose();
                                                                }
                                                                catch { MessageBox.Show("clearing bmps failed"); }
                                                            }
                                                            animation.PlaybillItem.BitmapsForScreen = null;
                                                        }
                                                    }
                                                    catch { MessageBox.Show("clearing bmps failed"); }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            SetupProgressBar(ref progressBar1, 0);
                        }
                    }

                    try
                    {
                        Directory.Delete(tempDirectory, true);
                    }
                    catch { }

                    SerialCommunication.Timeout = 1000;
                    Thread.Sleep(2000);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error");
                }

                try
                {
                    logWindow.Hide();
                }
                catch { MessageBox.Show("failed hiding window"); }
            }
            //catch { MessageBox.Show("sending failed"); }

            
            if (currentProgramNumber > -1)
            {
                foreach (var p in PreviewWindow.programsWithWindows)
                {
                    foreach (WindowOnPreview w in p)
                    {
                        foreach (PreviewAnimation a in w.Animations)
                            a.UnpauseAnimation();
                    }
                }
                //previewWindow.PlayNewActiveProgram(currentProgramNumber);
                //previewWindow.ActiveWindow = currentWindowNumber;
                //previewWindow.SelectedWindow.CurrentAnimationNumber = currentAnimationNumber;
            }
        }
        #endregion


        private void settingsTabControl_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (playbillTree.SelectedNode != null)
            {
                string nodeType = (string)playbillTree.SelectedNode.Tag;
                TreeNode node = playbillTree.SelectedNode;
                if (e.TabPage == null)
                    e.Cancel = true;
                //else if (nodeType != ((string)(e.TabPage.Tag)) && (string)e.TabPage.Tag != "screen")
                //    e.Cancel = true;
                //   It's a piece of code that guarded tabpage to not change pages when user didn't had selected proper node, and tried to move somewhere else.

                    // added code, an idea after it is to select a proper item, according to selected node.
                else if (nodeType != ((string)(e.TabPage.Tag)) && (string)e.TabPage.Tag == "playbill")
                {
                    if (nodeType == "item")
                        playbillTree.SelectedNode = node.Parent.Parent.Parent;
                    else if (nodeType == "window")
                        playbillTree.SelectedNode = node.Parent.Parent;
                    else if (nodeType == "program")
                        playbillTree.SelectedNode = node.Parent;

                    e.Cancel = false;
                }
                else if (nodeType != ((string)(e.TabPage.Tag)) && (string)e.TabPage.Tag == "program")
                {
                    if (nodeType == "item")
                    {
                        playbillTree.SelectedNode = node.Parent.Parent;
                        e.Cancel = false;
                    }
                    else if (nodeType == "window")
                    {
                        playbillTree.SelectedNode = node.Parent;
                        e.Cancel = false;
                    }
                    else if (nodeType == "playbill" && node.FirstNode.Name != "new")
                    {
                        playbillTree.SelectedNode = node.FirstNode;
                        e.Cancel = false;
                    }
                    else
                        e.Cancel = true;
                }
                else if (nodeType != ((string)(e.TabPage.Tag)) && (string)e.TabPage.Tag == "window")
                {
                    try
                    {
                        if (nodeType == "item")
                        {
                            playbillTree.SelectedNode = node.Parent;
                            e.Cancel = false;
                        }
                        else if (nodeType == "program" && node.FirstNode.Name != "new")
                        {
                            playbillTree.SelectedNode = node.FirstNode;
                            e.Cancel = false;
                        }
                        else if (nodeType == "playbill" && node.FirstNode.FirstNode.Name != "new")
                        {
                            playbillTree.SelectedNode = node.FirstNode.FirstNode;
                            e.Cancel = false;
                        }
                        else
                            e.Cancel = true;
                    }
                    catch (NullReferenceException)
                    {
                        e.Cancel = true;
                    }

                }
                else if (nodeType != ((string)(e.TabPage.Tag)) && (string)e.TabPage.Tag == "item")
                {
                    try
                    {
                        if (nodeType == "window" && node.FirstNode.Name != "new")
                        {
                            playbillTree.SelectedNode = node.FirstNode;
                            e.Cancel = false;
                        }
                        else if (nodeType == "program" && node.FirstNode.FirstNode.Name != "new")
                        {
                            playbillTree.SelectedNode = node.FirstNode.FirstNode;
                            e.Cancel = false;
                        }
                        else if (nodeType == "playbill" && node.FirstNode.FirstNode.FirstNode.Name != "new")
                        {
                            playbillTree.SelectedNode = node.FirstNode.FirstNode.FirstNode;
                            e.Cancel = false;
                        }
                        else
                            e.Cancel = true;
                    }
                    catch (NullReferenceException)
                    {
                        e.Cancel = true;
                    }
                }
                else
                {
                    //if (network == false)
                    //{
                    //    if ((string)e.TabPage.Tag == "screen" && SetupTestedPorts(ref portsCheckedListBox, false) > 0)
                    //        GetBrightness();
                    //}
                    //else 
                    //if ((string)e.TabPage.Tag == "screen")
                    //GetBrightness();

                    e.Cancel = false;
                }
            }
        }

        private void itemTypeTabControl_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (playbillTree.SelectedNode != null  && playbillTree.SelectedNode.Tag.ToString() == "item" && !levelChanging)
            {
                TreeNode node = playbillTree.SelectedNode;
                string tag = (string)(e.TabPage.Tag);

                //[{0}{1}]
                if (node.Text.ToLower() != string.Format("{0}", (tag.ToLower()), node.Index))
                {
                    //DialogResult result = MessageBox.Show(messageBoxesHashTable["messageBoxMessage_changeType"].ToString(),//"Change type?", 
                    //    messageBoxesHashTable["messageBoxTitle_changeType"].ToString(),//"Changing item's type", 
                    //    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    //if (result == System.Windows.Forms.DialogResult.No)
                    //    e.Cancel = true;
                    //else
                    {
                        //testPlaybill.ProgramsList[node.Parent.Parent.Index].WindowsList[node.Parent.Index].ItemsList[node.Index].ItemType =
                        //    (PlayWindowItemType)Enum.Parse(typeof(PlayWindowItemType), ((string)(e.TabPage.Tag.ToString())));

                        int windowNumber = node.Parent.Index;
                        int programNumber = node.Parent.Parent.Index;
                        int itemNumber = node.Index;

                        PlayWindowItem item = testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber];



                        //node.Text = string.Format("{0}", (tag), node.Index);

                        if (tag.ToLower() == "text")
                        {
                            item.ItemType = (PlayWindowItemType)Enum.Parse(typeof(PlayWindowItemType), ((string)(e.TabPage.Tag.ToString())));
                            textTextBox.Text = testPlaybill.ProgramsList[node.Parent.Parent.Index].WindowsList[node.Parent.Index].ItemsList[node.Index].Text;
                            node.Text = string.Format("{0}", (tag), node.Index);
                        }
                        else if (tag.ToLower() == "bitmap")
                        {
                            // bitmapTextBox.Text = testPlaybill.ProgramsList[node.Parent.Parent.Index].WindowsList[node.Parent.Index].ItemsList[node.Index].Text;
                            //if (item.Text == "")
                            {
                                string newBMPPath = SelectBMPFile();
                                if (!string.IsNullOrEmpty(newBMPPath))
                                {
                                    bitmapTextBox.Text = "";
                                    item.ItemType = (PlayWindowItemType)Enum.Parse(typeof(PlayWindowItemType), ((string)(e.TabPage.Tag.ToString())));
                                    bitmapTextBox.Text = newBMPPath;
                                    node.Text = string.Format("{0}", (tag), node.Index);

                                    newBMPPath = newBMPPath.Remove(newBMPPath.LastIndexOf("\\"));
                                    SetConfigurationSettingText("LedConfiguration/Program/Other/Paths/LoadBMP", newBMPPath);
                                    LoadConfiguration();

                                    OpenItemTab(item, false);
                                }
                                else
                                    e.Cancel = true;
                            }
                        }
                        else if (tag.ToLower() == "bitmaptext")
                        {
                            // Font tmp = new Font(testPlaybill.ProgramsList[node.Parent.Parent.Index].WindowsList[node.Parent.Index].ItemsList[node.Index].FontName,
                            //     testPlaybill.ProgramsList[node.Parent.Parent.Index].WindowsList[node.Parent.Index].ItemsList[node.Index].FontSize);
                            item.ItemType = (PlayWindowItemType)Enum.Parse(typeof(PlayWindowItemType), ((string)(e.TabPage.Tag.ToString())));
                            PlayWindowItem tmp = testPlaybill.ProgramsList[node.Parent.Parent.Index].WindowsList[node.Parent.Index].ItemsList[node.Index];

                            if (string.IsNullOrEmpty(bitmaptextRichTextBox.Rtf))
                                bitmaptextRichTextBox.Text = tmp.Text;
                            else
                                bitmaptextRichTextBox.Rtf = tmp.Rtf;
                            //bitmaptextRichTextBox.ForeColor = Color.FromName(tmp.FontColor);
                            //bitmaptextRichTextBox.BackColor = Color.FromName(tmp.BgColor);
                            //bitmaptextRichTextBox.Font = new Font(tmp.FontName, tmp.FontSize);
                            node.Text = string.Format("{0}", (tag), node.Index);

                            OpenItemTab(item, false);

                        }
                        else if (tag.ToLower() == "clock")
                        {
                            item.ItemType = (PlayWindowItemType)Enum.Parse(typeof(PlayWindowItemType), ((string)(e.TabPage.Tag.ToString())));
                            OpenItemTab(item, false);
                            node.Text = string.Format("{0}", (tag), node.Index);
                        }
                        else if (tag.ToLower() == "temperature")
                        {       // temperatureTextBox.Text = testPlaybill.ProgramsList[node.Parent.Parent.Index].WindowsList[node.Parent.Index].ItemsList[node.Index].Text;
                            item.ItemType = (PlayWindowItemType)Enum.Parse(typeof(PlayWindowItemType), ((string)(e.TabPage.Tag.ToString())));
                            OpenItemTab(item, false);
                            node.Text = string.Format("{0}", (tag), node.Index);
                        }
                        else if (tag.ToLower() == "animator")
                        //animatorSelectTextBox.Text = testPlaybill.ProgramsList[node.Parent.Parent.Index].WindowsList[node.Parent.Index].ItemsList[node.Index].Text;
                        //if (item.Text == "")
                        {
                            string newAnimatorPath = SelectAnimatorFile();
                            if (!string.IsNullOrEmpty(newAnimatorPath))
                            {
                                animatorSelectTextBox.Text = "";
                                item.ItemType = (PlayWindowItemType)Enum.Parse(typeof(PlayWindowItemType), ((string)(e.TabPage.Tag.ToString())));
                                animatorSelectTextBox.Text = newAnimatorPath;
                                node.Text = string.Format("{0}", (tag), node.Index);

                                newAnimatorPath = newAnimatorPath.Remove(newAnimatorPath.LastIndexOf("\\"));
                                SetConfigurationSettingText("LedConfiguration/Program/Other/Paths/LoadGIF", newAnimatorPath);
                                LoadConfiguration();

                                OpenItemTab(item, false);
                            }
                            else
                                e.Cancel = true;
                        }
                    }
                }
                else
                    e.Cancel = false;
            }
        }

        #region Item's settings changing

        #region Text (simple)
        private void textTextBox_TextChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Text =
                        ((System.Windows.Forms.TextBox)(sender)).Text;
                //playbillTree.SelectedNode.Name = ((System.Windows.Forms.TextBox)(sender)).Text;
            }
        }

        private void textStayTimeUpDown_ValueChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Stay =
                        (int)((NumericUpDown)(sender)).Value;
                previewWindow.SelectedWindow.Animations[itemNumber].StayTime = (short)((NumericUpDown)(sender)).Value;
                previewWindow.SelectedWindow.Animate(false, itemNumber);//commenting animating//

                if (!itemChanging)
                {
                    switch (testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].ItemType)
                    {
                        case PlayWindowItemType.Bitmap:
                            SetConfigurationSettingText("LedConfiguration/Program/Defaults/Bitmap/Stay", testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Stay.ToString());
                            break;
                        case PlayWindowItemType.BitmapText:
                            SetConfigurationSettingText("LedConfiguration/Program/Defaults/BitmapText/Stay", testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Stay.ToString());
                            break;
                        case PlayWindowItemType.Clock:
                            SetConfigurationSettingText("LedConfiguration/Program/Defaults/Clock/Stay", testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Stay.ToString());
                            break;
                        case PlayWindowItemType.Temperature:
                            SetConfigurationSettingText("LedConfiguration/Program/Defaults/Temperature/Stay", testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Stay.ToString());
                            break;
                        case PlayWindowItemType.Animator:
                            SetConfigurationSettingText("LedConfiguration/Program/Defaults/Animation/Stay", testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Stay.ToString());
                            break;

                    }
                    LoadConfiguration();
                }
            }
        }

        private void textEffectComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Effect =
                        ((ComboBox)(sender)).SelectedIndex;

                previewWindow.SelectedWindow.Animations[itemNumber].Effect = (EffectType)((ComboBox)(sender)).SelectedIndex;
                previewWindow.SelectedWindow.Animate(false, itemNumber);//commenting animating//
            }
        }

        private void textFontSizeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].FontSize =
                        int.Parse(((ComboBox)(sender)).Text);
            }
        }

        //private void textEffectSpeedUpDown_ValueChanged(object sender, EventArgs e)
        //{
        //    TreeNode selectedNode = playbillTree.SelectedNode;
        //    if (selectedNode != null)
        //    {
        //        int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
        //        int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
        //        int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

        //        testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Speed =
        //                (int)((NumericUpDown)(sender)).Maximum - (int)((NumericUpDown)(sender)).Value;
        //    }
        //}

        private void textEffectSpeedTrackBar_ValueChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                if (((TrackBar)(sender)).Value >= 5)
                {

                    testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Speed =
                            ((int)((TrackBar)(sender)).Value + 90);
                }
                else
                {
                    switch (((TrackBar)(sender)).Value)
                    {
                        case 4: testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Speed = 93; break;
                        case 3: testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Speed = 90; break;
                        case 2: testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Speed = 85; break;
                        case 1: testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Speed = 78; break;
                    }
                }

                previewWindow.SelectedWindow.Animations[itemNumber].Speed =
                    (short)testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Speed;
                previewWindow.SelectedWindow.Animations[itemNumber].StopAnimation();
                previewWindow.SelectedWindow.Animate(false, itemNumber);//commenting animating//
                //PreviewWindow.programsWithWindows[programNumber][windowNumber].Animations[itemNumber].Animate(false);

                testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Speed = 100 - testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Speed;

                if (testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].ItemType == PlayWindowItemType.Text)
                    textEffectSpeedValueLabel.Text = ((TrackBar)(sender)).Value.ToString();
                else if (testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].ItemType == PlayWindowItemType.Bitmap)
                    bitmapEffectSpeedValueLabel.Text = ((TrackBar)(sender)).Value.ToString();
                else if (testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].ItemType == PlayWindowItemType.BitmapText)
                    bitmaptextEffectSpeedValueLabel.Text = ((TrackBar)(sender)).Value.ToString();

                if (!itemChanging)
                {
                    if (testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].ItemType == PlayWindowItemType.Bitmap)
                        SetConfigurationSettingText("LedConfiguration/Program/Defaults/Bitmap/Speed", testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Speed.ToString());
                    else if (testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].ItemType == PlayWindowItemType.BitmapText)
                        SetConfigurationSettingText("LedConfiguration/Program/Defaults/BitmapText/Speed", testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Speed.ToString());

                    LoadConfiguration();
                }
            }
        }

        #endregion

        #region Bitmap
        private void selectBmpButton_Click(object sender, EventArgs e)
        {
            string bmp = SelectBMPFile();
            if (!string.IsNullOrEmpty(bmp))
            {
                bitmapTextBox.Text = bmp;
                previewWindow.LoadBitmapForItem(bitmapTextBox.Text);

                bmp = bmp.Remove(bmp.LastIndexOf("\\"));
                SetConfigurationSettingText("LedConfiguration/Program/Other/Paths/LoadBMP", bmp);
                LoadConfiguration();
            }
        }

        private void bitmapTextBox_TextChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Text =
                        ((System.Windows.Forms.TextBox)(sender)).Text;
                //playbillTree.SelectedNode.Name = ((System.Windows.Forms.TextBox)(sender)).Text;

                TreeNode itemNode = playbillTree.SelectedNode;


                if (!string.IsNullOrEmpty(((System.Windows.Forms.TextBox)(sender)).Text))
                {
                    previewWindow.SelectedWindow.Animations[itemNode.Index].InputBitmaps
                        = new List<Bitmap>() { new Bitmap(((System.Windows.Forms.TextBox)(sender)).Text) };
                    previewWindow.SelectedWindow.Animate(false, itemNode.Index);
                }
            }
        }
        #endregion

        #region bitmapText

        private void bitmaptextFontSizeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (((ComboBox)(sender)).SelectedItem != null && !delete)
            {
                TreeNode selectedNode = playbillTree.SelectedNode;
                if (selectedNode != null)
                {
                    int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                    int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                    int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                    Font tmp = bitmaptextRichTextBox.SelectionFont;
                    try
                    {
                        testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].FontSize = int.Parse(((ComboBox)(sender)).Text);
                        bitmaptextRichTextBox.SelectionFont = new Font(tmp.Name, testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].FontSize, tmp.Style);

                        //int selectionIndex = bitmaptextRichTextBox.SelectionStart;
                        //int selectionLength = bitmaptextRichTextBox.SelectionLength;

                        //// RtbToBitmap();

                        //bitmaptextRichTextBox.SelectionStart = selectionIndex;
                        //bitmaptextRichTextBox.SelectionLength = selectionLength;


                    }
                    catch (NullReferenceException)
                    {
                        int selectionIndex = bitmaptextRichTextBox.SelectionStart;
                        int selectionLength = bitmaptextRichTextBox.SelectionLength;

                        int newSize = int.Parse(((ComboBox)(sender)).Text);

                        //   ChangeTextSize(int.Parse(((ComboBox)(sender)).Text), selectionIndex, selectionLength / 2);
                        //   ChangeTextSize(int.Parse(((ComboBox)(sender)).Text), selectionLength / 2, selectionLength - selectionLength / 2);

                        ChangeTextSize(newSize, selectionIndex, selectionLength / 2);
                        ChangeTextSize(newSize, selectionIndex + selectionLength / 2, selectionLength - selectionLength / 2);


                        //RtbToBitmap();

                        bitmaptextRichTextBox.SelectionStart = selectionIndex;
                        bitmaptextRichTextBox.SelectionLength = selectionLength;

                        //MessageBox.Show("Unable to change font size");

                        //bitmaptextRichTextBox.Focus();
                    }

                    if (!itemChanging && !delete)
                    {
                        SetConfigurationSettingText("LedConfiguration/Program/Defaults/BitmapText/FontSize", testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].FontSize.ToString());
                        LoadConfiguration();
                    }

                    bitmaptextRichTextBox.Focus();
                }
            }
        }

        private void bitmaptextFontColorComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (delete == false)
            {
                int selectionIndex = bitmaptextRichTextBox.SelectionStart;
                int selectionLength = bitmaptextRichTextBox.SelectionLength;


                TreeNode SelectedNode = playbillTree.SelectedNode;
                if (SelectedNode != null)
                {
                    if (bitmaptextRichTextBox.SelectedText.Length > 0)
                    {
                        bitmaptextRichTextBox.SelectionColor = Color.FromName(((ComboBox)(sender)).SelectedItem.ToString());

                        if (((ComboBox)(sender)).SelectedIndex == bitmaptextBackgroundComboBox.SelectedIndex)
                        {
                            if (((ComboBox)(sender)).Focused)
                            {

                                bitmaptextRichTextBox.SelectionStart = selectionIndex;
                                bitmaptextRichTextBox.SelectionLength = selectionLength;

                                //bitmaptextFontColorComboBox.Focus();
                                bitmaptextBackgroundComboBox.SelectedIndex = (bitmaptextBackgroundComboBox.SelectedIndex + 1) % bitmaptextBackgroundComboBox.Items.Count;
                                bitmaptextBackgroundComboBox.SelectedItem = bitmaptextBackgroundComboBox.Items[bitmaptextBackgroundComboBox.SelectedIndex];
                                //bitmaptextFontColorComboBox.invo


                                //if (oldbitmaps)
                                //    bitmaptextRichTextBox.SelectionBackColor = Color.FromName(bitmaptextBackgroundComboBox.SelectedItem.ToString());
                                //else
                                //    SetBackGroudColorViaRTF(Color.FromName(bitmaptextBackgroundComboBox.SelectedItem.ToString()));

                                bitmaptextRichTextBox.SelectionBackColor = Color.FromName(bitmaptextBackgroundComboBox.SelectedItem.ToString());
                            }
                        }
                    }
                    else
                    {
                        //DialogResult result = MessageBox.Show("No text is selected. Change all text to the new color?", "Color", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        //if (result == System.Windows.Forms.DialogResult.Yes)
                        //{

                        //    int windowNumber = (SelectedNode.Tag.ToString() == "item") ? SelectedNode.Parent.Index : 0;
                        //    int programNumber = (SelectedNode.Tag.ToString() == "item") ? SelectedNode.Parent.Parent.Index : 0;
                        //    int itemNumber = (SelectedNode.Tag.ToString() == "item") ? SelectedNode.Index : 0;

                        //    testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].BgColor = ((ComboBox)(sender)).SelectedItem.ToString();

                        //    int selectionOneIndex = bitmaptextRichTextBox.SelectionStart;
                        //    bitmaptextRichTextBox.SelectAll();
                        //    bitmaptextRichTextBox.SelectionBackColor = Color.FromName(testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].BgColor);


                        //    //RtbToBitmap();
                        //    //bitmaptextRichTextBox.BackColor = Color.FromName(testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[SelectedNode.Index].BgColor);
                        //    bitmaptextRichTextBox.SelectionStart = selectionOneIndex;
                        //    bitmaptextRichTextBox.SelectionLength = 0;
                        //}
                        bitmaptextRichTextBox.SelectionColor = Color.FromName(((ComboBox)(sender)).SelectedItem.ToString());
                    }
                    
                    //RtbToBitmap();

                    bitmaptextRichTextBox.SelectionStart = selectionIndex;
                    bitmaptextRichTextBox.SelectionLength = selectionLength;

                    if (!itemChanging)
                    {
                        SetConfigurationSettingText("LedConfiguration/Program/Defaults/BitmapText/FontColor", ((ComboBox)(sender)).SelectedItem.ToString());
                        LoadConfiguration();
                    }

                    bitmaptextRichTextBox.Focus();
                }
            }

            /*if (delete == false)
            {
                if (bitmaptextRichTextBox.SelectedText.Length > 0)
                {
                    bitmaptextRichTextBox.SelectionColor = Color.FromName(((ComboBox)(sender)).SelectedItem.ToString());
                }
                else
                {
                    //DialogResult result = MessageBox.Show("No text is selected. Change all text to the new color?", "Color", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    //if (result == System.Windows.Forms.DialogResult.Yes)
                    //    bitmaptextRichTextBox.ForeColor = Color.FromName(((ComboBox)(sender)).SelectedItem.ToString());

                    bitmaptextRichTextBox.SelectionColor = Color.FromName(((ComboBox)(sender)).SelectedItem.ToString());
                }
            }
            else if (levelChanging == false && itemChanging == false)
                bitmaptextRichTextBox.ForeColor = Color.FromName(((ComboBox)(sender)).SelectedItem.ToString());

            int selectionIndex = bitmaptextRichTextBox.SelectionStart;
            int selectionLength = bitmaptextRichTextBox.SelectionLength;

            RtbToBitmap();

            bitmaptextRichTextBox.SelectionStart = selectionIndex;
            bitmaptextRichTextBox.SelectionLength = selectionLength;

            bitmaptextRichTextBox.Focus();*/
        }

        private void bitmaptextBackgroundComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (delete == true)
                return;

            TreeNode SelectedNode = playbillTree.SelectedNode;
            if (SelectedNode != null)
            {

                int selectionIndex = bitmaptextRichTextBox.SelectionStart;
                int selectionLength = bitmaptextRichTextBox.SelectionLength;
                
                //if(oldbitmaps)
                //    bitmaptextRichTextBox.SelectionBackColor = Color.FromName(((ComboBox)(sender)).SelectedItem.ToString());
                //else
                //    SetBackGroudColorViaRTF(Color.FromName(((ComboBox)(sender)).SelectedItem.ToString()));
                bitmaptextRichTextBox.SelectionBackColor = Color.FromName(((ComboBox)(sender)).SelectedItem.ToString());


                if (((ComboBox)(sender)).SelectedIndex == bitmaptextFontColorComboBox.SelectedIndex)
                {
                    if (((ComboBox)(sender)).Focused)
                    {

                        bitmaptextRichTextBox.SelectionStart = selectionIndex;
                        bitmaptextRichTextBox.SelectionLength = selectionLength;

                        //bitmaptextFontColorComboBox.Focus();
                        bitmaptextFontColorComboBox.SelectedIndex = (bitmaptextFontColorComboBox.SelectedIndex + 1) % bitmaptextFontColorComboBox.Items.Count;
                        bitmaptextFontColorComboBox.SelectedItem = bitmaptextFontColorComboBox.Items[bitmaptextFontColorComboBox.SelectedIndex];
                        //bitmaptextFontColorComboBox.invo
                        bitmaptextRichTextBox.SelectionColor = Color.FromName(bitmaptextFontColorComboBox.SelectedItem.ToString());
                    }
                }



                //RtbToBitmap();

                if (!itemChanging)
                {
                    SetConfigurationSettingText("LedConfiguration/Program/Defaults/BitmapText/BackColor", ((ComboBox)(sender)).SelectedItem.ToString());
                    LoadConfiguration();
                }

                bitmaptextRichTextBox.SelectionStart = selectionIndex;
                bitmaptextRichTextBox.SelectionLength = selectionLength;

                bitmaptextRichTextBox.Focus();
            }
        }

        private void bitmaptextFontComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (((ComboBox)(sender)).SelectedItem != null && !delete)
            {
                Font tmp = bitmaptextRichTextBox.SelectionFont;
                try
                {
                    bitmaptextRichTextBox.SelectionFont = new Font(((ComboBox)(sender)).SelectedItem.ToString(), tmp.Size, tmp.Style);
                }
                catch (Exception)
                {
                    //  MessageBox.Show(messageBoxesHashTable["messageBoxMessage_unableToChangeFont"].ToString());

                    int selectionIndex = bitmaptextRichTextBox.SelectionStart;
                    int selectionLenght = bitmaptextRichTextBox.SelectionLength;
                    string newFont = ((ComboBox)(sender)).SelectedItem.ToString();

                    ChangeFont(newFont, selectionIndex, selectionLenght / 2);
                    ChangeFont(newFont, selectionIndex + selectionLenght / 2, selectionLenght - selectionLenght / 2);

                    bitmaptextRichTextBox.SelectionStart = selectionIndex;
                    bitmaptextRichTextBox.SelectionLength = selectionLenght;

                }

                if (!itemChanging && !delete)
                {
                    SetConfigurationSettingText("LedConfiguration/Program/Defaults/BitmapText/FontName", ((ComboBox)(sender)).SelectedItem.ToString());
                    LoadConfiguration();
                }

                bitmaptextRichTextBox.Focus();
            }
        }

        private void bitmaptextRichTextBox_TextChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : ((windowsizechanged == true) ? wndchangingItemNumber : 0);

                if (testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList.Count <= itemNumber)
                    return;

                testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Text =
                    ((System.Windows.Forms.RichTextBox)(sender)).Text;

                testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Rtf =
                    ((System.Windows.Forms.RichTextBox)(sender)).Rtf;

                //playbillTree.SelectedNode.Name = ((System.Windows.Forms.RichTextBox)(sender)).Text;

                //int selectionIndex = bitmaptextRichTextBox.SelectionStart;
                //int selectionLength = bitmaptextRichTextBox.SelectionLength;

                //RtbToBitmap();

                //bitmaptextRichTextBox.SelectionStart = selectionIndex;
                //bitmaptextRichTextBox.SelectionLength = selectionLength;

                if (regeneratePreviewTimer != null)
                {
                    regeneratePreviewTimer.Stop();
                    regeneratePreviewTimer.Dispose();
                    regeneratePreviewTimer = null;
                }

                regeneratePreviewTimer = new System.Windows.Forms.Timer();
                regeneratePreviewTimer.Interval = 1500;
                regeneratePreviewTimer.Tick += new EventHandler(regeneratePreviewTimer_Tick);
                regeneratePreviewTimer.Start();
            }
        }

        private void bitmaptextBoldButton_Click(object sender, EventArgs e)
        {
            delete = true;
            
            int selectionStart = bitmaptextRichTextBox.SelectionStart;
            int selectionLength = bitmaptextRichTextBox.SelectionLength;

            bool setBold; // = !bitmaptextRichTextBox.SelectionFont.Bold;

            if (bitmaptextBoldButton.FlatStyle == FlatStyle.Flat)
            {
                setBold = false;
                bitmaptextBoldButton.FlatStyle = FlatStyle.Standard;
                bitmaptextBoldButton.BackColor = Control.DefaultBackColor;
            }
            else
            {
                setBold = true;
                bitmaptextBoldButton.FlatStyle = FlatStyle.Flat;
                bitmaptextBoldButton.BackColor = Color.SkyBlue;
            }

            if (selectionLength > 0)
            {

                RichTextBox rtbTemp = new RichTextBox();
                rtbTemp.Rtf = bitmaptextRichTextBox.Rtf;

                rtbTemp.SelectionStart = selectionStart;
                rtbTemp.SelectionLength = selectionLength;

                for (int i = selectionStart; i < selectionStart + selectionLength; i++)
                {
                    rtbTemp.Select(i, 1);

                    Font tmp = rtbTemp.SelectionFont;

                    if (setBold)
                        rtbTemp.SelectionFont = new Font(tmp.Name, tmp.Size, tmp.Style | FontStyle.Bold);
                    else
                        rtbTemp.SelectionFont = new Font(tmp.Name, tmp.Size, (tmp.Style | FontStyle.Bold) ^ FontStyle.Bold);
                }

                bitmaptextRichTextBox.Rtf = rtbTemp.Rtf;

            }
            else
            {
                Font tmp = bitmaptextRichTextBox.SelectionFont;

                if (setBold)
                    bitmaptextRichTextBox.SelectionFont = new Font(tmp.Name, tmp.Size, tmp.Style | FontStyle.Bold);
                else
                    bitmaptextRichTextBox.SelectionFont = new Font(tmp.Name, tmp.Size, (tmp.Style | FontStyle.Bold) ^ FontStyle.Bold);
            }

            bitmaptextRichTextBox.SelectionStart = selectionStart;
            bitmaptextRichTextBox.SelectionLength = selectionLength;

            delete = false;

            if (regeneratePreviewTimer != null)
            {
                regeneratePreviewTimer.Stop();
                regeneratePreviewTimer.Dispose();
                regeneratePreviewTimer = null;
            }

            regeneratePreviewTimer = new System.Windows.Forms.Timer();
            regeneratePreviewTimer.Interval = 1500;
            regeneratePreviewTimer.Tick += new EventHandler(regeneratePreviewTimer_Tick);
            regeneratePreviewTimer.Start();

            bitmaptextRichTextBox.Focus();
        }

        private void bitmaptextItalicButton_Click(object sender, EventArgs e)
        {
            delete = true;

            int selectionStart = bitmaptextRichTextBox.SelectionStart;
            int selectionLength = bitmaptextRichTextBox.SelectionLength;



            bool setItalic; // = !bitmaptextRichTextBox.SelectionFont.Italic;

            if (bitmaptextItalicButton.FlatStyle == FlatStyle.Flat)
            {
                setItalic = false;
                bitmaptextItalicButton.FlatStyle = FlatStyle.Standard;
                bitmaptextItalicButton.BackColor = Control.DefaultBackColor;
            }
            else
            {
                setItalic = true;
                bitmaptextItalicButton.FlatStyle = FlatStyle.Flat;
                bitmaptextItalicButton.BackColor = Color.SkyBlue;
                
            }

            if (selectionLength > 0)
            {

                RichTextBox rtbTemp = new RichTextBox();
                rtbTemp.Rtf = bitmaptextRichTextBox.Rtf;
                rtbTemp.SelectionStart = selectionStart;
                rtbTemp.SelectionLength = selectionLength;


                for (int i = selectionStart; i < selectionStart + selectionLength; i++)
                {
                    rtbTemp.Select(i, 1);

                    Font tmp = rtbTemp.SelectionFont;

                    if (setItalic)
                        rtbTemp.SelectionFont = new Font(tmp.Name, tmp.Size, tmp.Style | FontStyle.Italic);
                    else
                        rtbTemp.SelectionFont = new Font(tmp.Name, tmp.Size, (tmp.Style | FontStyle.Italic) ^ FontStyle.Italic);


                }


                bitmaptextRichTextBox.Rtf = rtbTemp.Rtf;
            }
            else
            {
                Font tmp = bitmaptextRichTextBox.SelectionFont;

                if (setItalic)
                    bitmaptextRichTextBox.SelectionFont = new Font(tmp.Name, tmp.Size, tmp.Style | FontStyle.Italic);
                else
                    bitmaptextRichTextBox.SelectionFont = new Font(tmp.Name, tmp.Size, (tmp.Style | FontStyle.Italic) ^ FontStyle.Italic);
            }

            bitmaptextRichTextBox.SelectionStart = selectionStart;
            bitmaptextRichTextBox.SelectionLength = selectionLength;

            delete = false;

            if (regeneratePreviewTimer != null)
            {
                regeneratePreviewTimer.Stop();
                regeneratePreviewTimer.Dispose();
                regeneratePreviewTimer = null;
            }

            regeneratePreviewTimer = new System.Windows.Forms.Timer();
            regeneratePreviewTimer.Interval = 1500;
            regeneratePreviewTimer.Tick += new EventHandler(regeneratePreviewTimer_Tick);
            regeneratePreviewTimer.Start();

            bitmaptextRichTextBox.Focus();

           
        }

        private void bitmaptextUnderlineButton_Click(object sender, EventArgs e)
        {
            delete = true;
            int selectionStart = bitmaptextRichTextBox.SelectionStart;
            int selectionLength = bitmaptextRichTextBox.SelectionLength;

            //bitmaptextRichTextBox.Select(selectionStart, 1);

            bool setUnderline; // = !bitmaptextRichTextBox.SelectionFont.Underline;

            if (bitmaptextUnderlineButton.FlatStyle == FlatStyle.Flat)
            {
                setUnderline = false;
                bitmaptextUnderlineButton.FlatStyle = FlatStyle.Standard;
                bitmaptextUnderlineButton.BackColor = Control.DefaultBackColor;
                
            }
            else
            {
                setUnderline = true;
                bitmaptextUnderlineButton.FlatStyle = FlatStyle.Flat;
                bitmaptextUnderlineButton.BackColor = Color.SkyBlue;
            }

            if (selectionLength > 0)
            {

                RichTextBox rtbTemp = new RichTextBox();
                rtbTemp.Rtf = bitmaptextRichTextBox.Rtf;
                rtbTemp.SelectionStart = selectionStart;
                rtbTemp.SelectionLength = selectionLength;

                for (int i = selectionStart; i < selectionStart + selectionLength; i++)
                {
                    rtbTemp.Select(i, 1);

                    Font tmp = rtbTemp.SelectionFont;

                    if (setUnderline)
                        rtbTemp.SelectionFont = new Font(tmp.Name, tmp.Size, tmp.Style | FontStyle.Underline);
                    else
                        rtbTemp.SelectionFont = new Font(tmp.Name, tmp.Size, (tmp.Style | FontStyle.Underline) ^ FontStyle.Underline);
                }


                bitmaptextRichTextBox.Rtf = rtbTemp.Rtf;
            }
            else
            {
                Font tmp = bitmaptextRichTextBox.SelectionFont;

                if (setUnderline)
                    bitmaptextRichTextBox.SelectionFont = new Font(tmp.Name, tmp.Size, tmp.Style | FontStyle.Underline);
                else
                    bitmaptextRichTextBox.SelectionFont = new Font(tmp.Name, tmp.Size, (tmp.Style | FontStyle.Underline) ^ FontStyle.Underline);
            }

            bitmaptextRichTextBox.SelectionStart = selectionStart;
            bitmaptextRichTextBox.SelectionLength = selectionLength;

            delete = false;

            if (regeneratePreviewTimer != null)
            {
                regeneratePreviewTimer.Stop();
                regeneratePreviewTimer.Dispose();
                regeneratePreviewTimer = null;
            }

            regeneratePreviewTimer = new System.Windows.Forms.Timer();
            regeneratePreviewTimer.Interval = 1500;
            regeneratePreviewTimer.Tick += new EventHandler(regeneratePreviewTimer_Tick);
            regeneratePreviewTimer.Start();

            bitmaptextRichTextBox.Focus();
            //bitmaptextRichTextBox.SelectionStart = selectionStart;
            //bitmaptextRichTextBox.SelectionLength = selectionLength;

            //RtbToBitmap();

            //bitmaptextRichTextBox.SelectionStart = selectionStart;
            //bitmaptextRichTextBox.SelectionLength = selectionLength;
        }

        private void bitmaptextRightButton_Click(object sender, EventArgs e)
        {
            bitmaptextRichTextBox.SelectionAlignment = HorizontalAlignment.Right;

            int selectionStart = bitmaptextRichTextBox.SelectionStart;
            int selectionLength = bitmaptextRichTextBox.SelectionLength;

            //RtbToBitmap();
            bitmaptextCenterButton.FlatStyle = FlatStyle.Standard;
            bitmaptextRightButton.FlatStyle = FlatStyle.Flat;
            bitmaptextLeftButton.FlatStyle = FlatStyle.Standard;


            bitmaptextCenterButton.BackColor = Control.DefaultBackColor;
            bitmaptextRightButton.BackColor = Color.SkyBlue;
            bitmaptextLeftButton.BackColor = Control.DefaultBackColor;

            bitmaptextRichTextBox.SelectionStart = selectionStart;
            bitmaptextRichTextBox.SelectionLength = selectionLength;

            bitmaptextRichTextBox.Focus();
        }

        private void bitmaptextCenterButton_Click(object sender, EventArgs e)
        {
            bitmaptextRichTextBox.SelectionAlignment = HorizontalAlignment.Center;

            int selectionStart = bitmaptextRichTextBox.SelectionStart;
            int selectionLength = bitmaptextRichTextBox.SelectionLength;

            //RtbToBitmap();
            bitmaptextCenterButton.FlatStyle = FlatStyle.Flat;
            bitmaptextRightButton.FlatStyle = FlatStyle.Standard;
            bitmaptextLeftButton.FlatStyle = FlatStyle.Standard;

            bitmaptextCenterButton.BackColor = Color.SkyBlue;
            bitmaptextRightButton.BackColor = Control.DefaultBackColor;
            bitmaptextLeftButton.BackColor = Control.DefaultBackColor;

            bitmaptextRichTextBox.SelectionStart = selectionStart;
            bitmaptextRichTextBox.SelectionLength = selectionLength;

            bitmaptextRichTextBox.Focus();
        }

        private void bitmaptextLeftButton_Click(object sender, EventArgs e)
        {
            bitmaptextRichTextBox.SelectionAlignment = HorizontalAlignment.Left;

            int selectionStart = bitmaptextRichTextBox.SelectionStart;
            int selectionLength = bitmaptextRichTextBox.SelectionLength;

            //RtbToBitmap();
            bitmaptextCenterButton.FlatStyle = FlatStyle.Standard;
            bitmaptextRightButton.FlatStyle = FlatStyle.Standard;
            bitmaptextLeftButton.FlatStyle = FlatStyle.Flat;

            bitmaptextCenterButton.BackColor = Control.DefaultBackColor;
            bitmaptextRightButton.BackColor = Control.DefaultBackColor;
            bitmaptextLeftButton.BackColor = Color.SkyBlue;

            bitmaptextRichTextBox.SelectionStart = selectionStart;
            bitmaptextRichTextBox.SelectionLength = selectionLength;

            bitmaptextRichTextBox.Focus();
        }

        private void LoadRTFbutton_Click(object sender, EventArgs e)
        {
            string path = SelectRTFFile();
            if (!string.IsNullOrEmpty(path) && path.Contains(".txt"))
            {
                bitmaptextRichTextBox.LoadFile(path, RichTextBoxStreamType.PlainText);

                path = path.Remove(path.LastIndexOf("\\"));

                SetConfigurationSettingText("LedConfiguration/Program/Other/Paths/LoadRTF", path);
                LoadConfiguration();

                TreeNode selectedNode = playbillTree.SelectedNode;
                if (selectedNode != null)
                {
                    int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                    int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                    int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                    Font tmp = new Font(testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].FontName, float.Parse(testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].FontSize.ToString()), bitmaptextRichTextBox.SelectionFont.Style);

                    bitmaptextRichTextBox.Font = tmp;

                    testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Text = bitmaptextRichTextBox.Text;
                    bitmaptextRichTextBox.SelectAll();
                    bitmaptextRichTextBox.SelectionColor = Color.FromName(testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].FontColor);
                    bitmaptextRichTextBox.DeselectAll();
                }
            }
            else if (!string.IsNullOrEmpty(path) && path.Contains(".rtf"))
            {
                bitmaptextRichTextBox.LoadFile(path, RichTextBoxStreamType.RichText);

                path = path.Remove(path.LastIndexOf("\\"));

                SetConfigurationSettingText("LedConfiguration/Program/Other/Paths/LoadRTF", path);
                LoadConfiguration();

                TreeNode selectedNode = playbillTree.SelectedNode;
                if (selectedNode != null)
                {
                    int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                    int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                    int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                    testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Rtf = bitmaptextRichTextBox.Rtf;
                    testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Text = bitmaptextRichTextBox.Text;
                }
            }
        }

        #endregion

        #region Clock

        private void clockTextBox_TextChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Text =
                    ((System.Windows.Forms.TextBox)(sender)).Text;

                OpenItemTab(testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber], false);
            }
        }

        private void clockFontSizeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                PlayWindowItem item = CurrentPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber];
                item.FontSize = int.Parse(((ComboBox)(sender)).Text);
                item.FontSizeIndex = ((ComboBox)sender).SelectedIndex;

                if (item.FontSize > CurrentWindow.Height / 2)
                {
                    clockMultiLineCheckBox.Checked = false;
                    clockMultiLineCheckBox.Enabled = false;
                }
                else if ((item.FontSize <= CurrentWindow.Height / 2)
                    && (clockHourCheckBox.Checked || clockMinuteCheckBox.Checked || clockSecondsCheckBox.Checked)
                    && (clockYearCheckBox.Checked || clockMonthCheckBox.Checked || clockDayCheckBox.Checked || clockDayOfWeekCheckBox.Checked))
                {
                    clockMultiLineCheckBox.Enabled = true;
                }

                //OpenItemTab(item, false);
                if(item.ItemType == PlayWindowItemType.Clock)
                    GenerateClockBitmap(item);
                else if (item.ItemType == PlayWindowItemType.Temperature)
                    GenerateTemperatureBitmap(item);
                
                if (!itemChanging)
                {
                    if (testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].ItemType == PlayWindowItemType.Clock)
                        SetConfigurationSettingText("LedConfiguration/Program/Defaults/Clock/FontSize", testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].FontSize.ToString());
                    else if (testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].ItemType == PlayWindowItemType.Temperature)
                        SetConfigurationSettingText("LedConfiguration/Program/Defaults/Temperature/FontSize", testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].FontSize.ToString());
                    LoadConfiguration();
                }
            }
        }

        private void clockFontColorComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                PlayWindowItem item = testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber];

                item.FontColor = ((ComboBox)(sender)).Text;
                item.Color = (uint)System.Drawing.Color.FromName(item.FontColor).R
                    + (uint)System.Drawing.Color.FromName(item.FontColor).G * 256
                    + (uint)System.Drawing.Color.FromName(item.FontColor).B * 256 * 256;

                //OpenItemTab(item, false);

                if (item.ItemType == PlayWindowItemType.Clock)
                    GenerateClockBitmap(item);
                else if (item.ItemType == PlayWindowItemType.Temperature)
                    GenerateTemperatureBitmap(item);

                if (!itemChanging)
                {
                    if (testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].ItemType == PlayWindowItemType.Clock)
                        SetConfigurationSettingText("LedConfiguration/Program/Defaults/Clock/FontColor", ((ComboBox)(sender)).SelectedItem.ToString());
                    else if (testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].ItemType == PlayWindowItemType.Temperature)
                        SetConfigurationSettingText("LedConfiguration/Program/Defaults/Temperature/FontColor", ((ComboBox)(sender)).SelectedItem.ToString());
                    LoadConfiguration();
                }
            }
        }

        private void clock24hCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                Boolean[] att = testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Attributes;

                att[8] = ((CheckBox)(sender)).Checked;

                SetConfigurationSettingText("LedConfiguration/Program/Defaults/Clock/ShortDay", att[8].ToString());
                LoadConfiguration();

                testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Attributes = att;
                GenerateClockBitmap(testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber]);

            }
        }

        private void clock2YcheckBox_CheckedChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                Boolean[] att = testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Attributes;

                att[9] = ((CheckBox)(sender)).Checked;

                SetConfigurationSettingText("LedConfiguration/Program/Defaults/Clock/ShortYear", att[9].ToString());
                LoadConfiguration();

                testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Attributes = att;
                GenerateClockBitmap(testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber]);
            }
        }

        private void clockMultiLineCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                Boolean[] att = testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Attributes;

                att[10] = ((CheckBox)(sender)).Checked;

                SetConfigurationSettingText("LedConfiguration/Program/Defaults/Clock/Multiline", att[10].ToString());
                LoadConfiguration();

                testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Attributes = att;
                GenerateClockBitmap(testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber]);
            }
        }

        private void clockYearCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                Boolean[] att = testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Attributes;

                att[0] = ((CheckBox)(sender)).Checked;

                SetConfigurationSettingText("LedConfiguration/Program/Defaults/Clock/Year", att[0].ToString());
                LoadConfiguration();

                testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Attributes = att;

                if (!(clockHourCheckBox.Checked || clockMinuteCheckBox.Checked || clockSecondsCheckBox.Checked)
                && !(clockYearCheckBox.Checked || clockMonthCheckBox.Checked || clockDayCheckBox.Checked || clockDayOfWeekCheckBox.Checked))
                {
                    clockHourCheckBox.Checked = true;
                    clockMinuteCheckBox.Checked = true;
                }

                if (!clockYearCheckBox.Checked)
                {
                    clock2YcheckBox.Checked = false;
                    clock2YcheckBox.Enabled = false;
                }
                else
                {
                    clock2YcheckBox.Enabled = true;                    
                }

                if (!(clockYearCheckBox.Checked || clockMonthCheckBox.Checked || clockDayCheckBox.Checked || clockDayOfWeekCheckBox.Checked))
                {
                    clockMultiLineCheckBox.Checked = false;
                    clockMultiLineCheckBox.Enabled = false;
                }
                else if (clockHourCheckBox.Checked || clockMinuteCheckBox.Checked || clockSecondsCheckBox.Checked)
                {
                    clockMultiLineCheckBox.Enabled = true;
                }

                GenerateClockBitmap(testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber]);
            }
        }

        private void clockMonthCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                Boolean[] att = testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Attributes;

                att[1] = ((CheckBox)(sender)).Checked;

                SetConfigurationSettingText("LedConfiguration/Program/Defaults/Clock/Month", att[1].ToString());
                LoadConfiguration();

                testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Attributes = att;

                if (((CheckBox)sender).Checked)
                {
                    clockDayCheckBox.Enabled = true;
                }
                else
                {
                    clockDayCheckBox.Checked = false;
                    clockDayCheckBox.Enabled = false;
                }

                if (!(clockHourCheckBox.Checked || clockMinuteCheckBox.Checked || clockSecondsCheckBox.Checked)
                && !(clockYearCheckBox.Checked || clockMonthCheckBox.Checked || clockDayCheckBox.Checked || clockDayOfWeekCheckBox.Checked))
                {
                    clockHourCheckBox.Checked = true;
                    clockMinuteCheckBox.Checked = true;
                }

                if (!(clockYearCheckBox.Checked || clockMonthCheckBox.Checked || clockDayCheckBox.Checked || clockDayOfWeekCheckBox.Checked))
                {
                    clockMultiLineCheckBox.Checked = false;
                    clockMultiLineCheckBox.Enabled = false;
                }
                else if (clockHourCheckBox.Checked || clockMinuteCheckBox.Checked || clockSecondsCheckBox.Checked)
                {
                    clockMultiLineCheckBox.Enabled = true;
                }

                GenerateClockBitmap(testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber]);
            }
        }

        private void clockDayCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                Boolean[] att = testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Attributes;

                att[2] = ((CheckBox)(sender)).Checked;

                SetConfigurationSettingText("LedConfiguration/Program/Defaults/Clock/Day", att[2].ToString());
                LoadConfiguration();

                testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Attributes = att;

                if (!(clockHourCheckBox.Checked || clockMinuteCheckBox.Checked || clockSecondsCheckBox.Checked)
                && !(clockYearCheckBox.Checked || clockMonthCheckBox.Checked || clockDayCheckBox.Checked || clockDayOfWeekCheckBox.Checked))
                {
                    clockHourCheckBox.Checked = true;
                    clockMinuteCheckBox.Checked = true;
                }

                if (!(clockYearCheckBox.Checked || clockMonthCheckBox.Checked || clockDayCheckBox.Checked || clockDayOfWeekCheckBox.Checked))
                {
                    clockMultiLineCheckBox.Checked = false;
                    clockMultiLineCheckBox.Enabled = false;
                }
                else if (clockHourCheckBox.Checked || clockMinuteCheckBox.Checked || clockSecondsCheckBox.Checked)
                {
                    clockMultiLineCheckBox.Enabled = true;
                }

                GenerateClockBitmap(testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber]);
            }
        }

        private void clockHourCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                Boolean[] att = testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Attributes;

                att[3] = ((CheckBox)(sender)).Checked;

                SetConfigurationSettingText("LedConfiguration/Program/Defaults/Clock/Hour", att[3].ToString());
                LoadConfiguration();

                testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Attributes = att;

                if (((CheckBox)sender).Checked)
                {
                    clockMinuteCheckBox.Enabled = true;
                }
                else
                {
                    clockMinuteCheckBox.Checked = false;
                    clockMinuteCheckBox.Enabled = false;
                }

                if (!(clockHourCheckBox.Checked || clockMinuteCheckBox.Checked || clockSecondsCheckBox.Checked)
                && !(clockYearCheckBox.Checked || clockMonthCheckBox.Checked || clockDayCheckBox.Checked || clockDayOfWeekCheckBox.Checked))
                {
                    clockYearCheckBox.Checked = true;
                    clockMonthCheckBox.Checked = true;
                    clockDayCheckBox.Checked = true;
                }

                if (!clockHourCheckBox.Checked)
                {
                    Clock24hCheckBox.Checked = true;
                    Clock24hCheckBox.Enabled = false;
                }
                else
                {
                    Clock24hCheckBox.Enabled = true;
                }

                if (!(clockHourCheckBox.Checked || clockMinuteCheckBox.Checked || clockSecondsCheckBox.Checked))
                {
                    clockMultiLineCheckBox.Checked = false;
                    clockMultiLineCheckBox.Enabled = false;
                }
                else if (clockYearCheckBox.Checked || clockMonthCheckBox.Checked || clockDayCheckBox.Checked || clockDayOfWeekCheckBox.Checked)
                {
                    clockMultiLineCheckBox.Enabled = true;
                }

                GenerateClockBitmap(testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber]);
            }
        }

        private void clockMinuteCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                Boolean[] att = testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Attributes;

                att[4] = ((CheckBox)(sender)).Checked;

                SetConfigurationSettingText("LedConfiguration/Program/Defaults/Clock/Minutes", att[4].ToString());
                LoadConfiguration();

                testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Attributes = att;


                if (((CheckBox)sender).Checked)
                {
                    clockSecondsCheckBox.Enabled = true;
                }
                else
                {
                    clockSecondsCheckBox.Checked = false;
                    clockSecondsCheckBox.Enabled = false;
                }

                if (!(clockHourCheckBox.Checked || clockMinuteCheckBox.Checked || clockSecondsCheckBox.Checked)
                && !(clockYearCheckBox.Checked || clockMonthCheckBox.Checked || clockDayCheckBox.Checked || clockDayOfWeekCheckBox.Checked))
                {
                    clockYearCheckBox.Checked = true;
                    clockMonthCheckBox.Checked = true;
                    clockDayCheckBox.Checked = true;
                }

                GenerateClockBitmap(testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber]);
            }
        }

        private void clockSecondsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                Boolean[] att = testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Attributes;

                att[5] = ((CheckBox)(sender)).Checked;

                SetConfigurationSettingText("LedConfiguration/Program/Defaults/Clock/Seconds", att[5].ToString());
                LoadConfiguration();

                testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Attributes = att;

                if (!(clockHourCheckBox.Checked || clockMinuteCheckBox.Checked || clockSecondsCheckBox.Checked)
                && !(clockYearCheckBox.Checked || clockMonthCheckBox.Checked || clockDayCheckBox.Checked || clockDayOfWeekCheckBox.Checked))
                {
                    clockYearCheckBox.Checked = true;
                    clockMonthCheckBox.Checked = true;
                    clockDayCheckBox.Checked = true;
                }

                GenerateClockBitmap(testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber]);
            }
        }

        private void clockDayOfWeekCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                Boolean[] att = testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Attributes;

                att[6] = ((CheckBox)(sender)).Checked;

                SetConfigurationSettingText("LedConfiguration/Program/Defaults/Clock/DayOfWeek", att[6].ToString());
                LoadConfiguration();

                testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Attributes = att;


                if (!(clockHourCheckBox.Checked || clockMinuteCheckBox.Checked || clockSecondsCheckBox.Checked)
                && !(clockYearCheckBox.Checked || clockMonthCheckBox.Checked || clockDayCheckBox.Checked || clockDayOfWeekCheckBox.Checked))
                {
                    clockHourCheckBox.Checked = true;
                    clockMinuteCheckBox.Checked = true;
                }

                if (!(clockYearCheckBox.Checked || clockMonthCheckBox.Checked || clockDayCheckBox.Checked || clockDayOfWeekCheckBox.Checked))
                {
                    clockMultiLineCheckBox.Checked = false;
                    clockMultiLineCheckBox.Enabled = false;
                }
                else if (clockHourCheckBox.Checked || clockMinuteCheckBox.Checked || clockSecondsCheckBox.Checked)
                {
                    clockMultiLineCheckBox.Enabled = true;
                }

                GenerateClockBitmap(testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber]);
            }
        }

        private void clockHandsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                Boolean[] att = testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Attributes;

                att[7] = ((CheckBox)(sender)).Checked;

                SetConfigurationSettingText("LedConfiguration/Program/Defaults/Clock/Hands", att[7].ToString());
                LoadConfiguration();

                testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Attributes = att;
                GenerateClockBitmap(testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber]);
            }
        }

        private void clockMarksCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                Boolean[] att = testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Attributes;

                att[14] = ((CheckBox)(sender)).Checked;

                SetConfigurationSettingText("LedConfiguration/Program/Defaults/Clock/Marks", att[14].ToString());
                LoadConfiguration();

                testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Attributes = att;
                GenerateClockBitmap(testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber]);
            }
        }

        #endregion

        #region Temperature

        private void temperatureCelsiusRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                PlayWindowItem item = testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber];
                item.TempAtt = (((RadioButton)(sender)).Checked) ? (byte)0 : (byte)1;

                temperatureFahrenheitRadioButton.Checked = !(((RadioButton)(sender)).Checked);

                if (!itemChanging)
                {
                    SetConfigurationSettingText("LedConfiguration/Program/Defaults/Temperature/Celcius", ((((RadioButton)(sender)).Checked) ? (byte)0 : (byte)1).ToString());
                    LoadConfiguration();
                }

                //OpenItemTab(item, false);
                GenerateTemperatureBitmap(item);
            }
        }

        private void temperatureFahrenheitRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                PlayWindowItem item = testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber];
                item.TempAtt = (((RadioButton)(sender)).Checked) ? (byte)1 : (byte)0;

                temperatureCelsiusRadioButton.Checked = !(((RadioButton)(sender)).Checked);

                //OpenItemTab(item, false);
                GenerateTemperatureBitmap(item);
            }
        }

        #endregion

        #region Animator

        private void animatorSelectButton_Click(object sender, EventArgs e)
        {
            string newAnimatorPath = SelectAnimatorFile();
            if (!string.IsNullOrEmpty(newAnimatorPath))
            {
                animatorSelectTextBox.Text = newAnimatorPath;

                newAnimatorPath = newAnimatorPath.Remove(newAnimatorPath.LastIndexOf("\\"));
                SetConfigurationSettingText("LedConfiguration/Program/Other/Paths/LoadGIF", newAnimatorPath);
                LoadConfiguration();
            }
            // here's place to put sth like
            // previewWindow.LoadBitmapForItem(animatorSelectTextBox.Text);
        }

        private void animatorSelectTextBox_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(((TextBox)sender).Text))// != null)
            {
                TreeNode selectedNode = playbillTree.SelectedNode;
                if (selectedNode != null)
                {
                    int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                    int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                    int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                    testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Text =
                            ((System.Windows.Forms.TextBox)(sender)).Text;
                    //playbillTree.SelectedNode.Name = ((System.Windows.Forms.TextBox)(sender)).Text;

                    TreeNode itemNode = playbillTree.SelectedNode;

                    List<Bitmap> tmpbmpl = new List<Bitmap>();

                    using (Image gifImg = Image.FromFile(((System.Windows.Forms.TextBox)(sender)).Text))
                    {
                        FrameDimension dimension = new FrameDimension(gifImg.FrameDimensionsList[0]);

                        for (int i = 0; i < gifImg.GetFrameCount(dimension); i++)
                        {
                            gifImg.SelectActiveFrame(dimension, i);
                            tmpbmpl.Add(new Bitmap(gifImg));
                        }

                        PropertyItem item = gifImg.GetPropertyItem(0x5100); // FrameDelay in libgdiplus
                        // Time is in 1/100th of a second
                        int delay = (item.Value[0] + item.Value[1] * 256) * 10;
                        CurrentPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].FrameDelay = delay / 10;

                        testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].BitmapsList = tmpbmpl;

                        previewWindow.SelectedWindow.CurrentAnimation.Speed = -1;
                        previewWindow.SelectedWindow.CurrentAnimation.StayTime = short.Parse(delay.ToString());
                        previewWindow.SelectedWindow.CurrentAnimation.InputBitmaps
                                = tmpbmpl;
                        previewWindow.SelectedWindow.Animate(false, itemNode.Index);
                    }
                }
            }
            else
            {
                itemTypeTabControl.SelectedIndex = 0;
            }
        }

        private void animatorRenderComboBox_IntexChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Mode = ((ComboBox)(sender)).SelectedIndex;

                if (testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].ItemType == PlayWindowItemType.Animator)
                    SetConfigurationSettingText("LedConfiguration/Program/Defaults/Animation/Mode", ((ComboBox)(sender)).SelectedIndex.ToString());
                else if (testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].ItemType == PlayWindowItemType.Bitmap)
                    SetConfigurationSettingText("LedConfiguration/Program/Defaults/Bitmap/Mode", ((ComboBox)(sender)).SelectedIndex.ToString());

                LoadConfiguration();

                TreeNode itemNode = playbillTree.SelectedNode;

                List<Bitmap> tmpbmpl = new List<Bitmap>();

                if (CurrentPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].ItemType == PlayWindowItemType.Animator)
                {
                    Image gifImg = Image.FromFile((animatorSelectTextBox).Text);
                    FrameDimension dimension = new FrameDimension(gifImg.FrameDimensionsList[0]);

                    for (int i = 0; i < gifImg.GetFrameCount(dimension); i++)
                    {
                        gifImg.SelectActiveFrame(dimension, i);
                        tmpbmpl.Add(new Bitmap(gifImg));
                    }

                    PropertyItem item = gifImg.GetPropertyItem(0x5100); // FrameDelay in libgdiplus
                    // Time is in 1/100th of a second
                    int delay = (item.Value[0] + item.Value[1] * 256) * 10;
                    CurrentPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].FrameDelay = delay / 10;

                    previewWindow.SelectedWindow.Animations[itemNode.Index].Speed = -1;
                    previewWindow.SelectedWindow.Animations[itemNode.Index].StayTime = short.Parse(delay.ToString());
                }
                else if (testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].ItemType == PlayWindowItemType.Bitmap)
                {
                    if (!string.IsNullOrEmpty((bitmapTextBox).Text))
                    {
                        tmpbmpl = new List<Bitmap>() { new Bitmap((bitmapTextBox).Text) };
                        previewWindow.SelectedWindow.Animations[itemNode.Index].StayTime =
                            (short)bitmapStayTimeUpDown.Value;
                        previewWindow.SelectedWindow.Animations[itemNode.Index].Speed =
                            (short)(100 - CurrentPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Speed);
                        //short.Parse(bitmapEffectSpeedLabel.ToString());
                        //previewWindow.SelectedWindow.Animate(false, itemNode.Index);
                    }
                }

                testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].BitmapsList = tmpbmpl;

                previewWindow.SelectedWindow.CurrentAnimation.InputBitmaps
                        = tmpbmpl;
                previewWindow.SelectedWindow.Animate(false, itemNode.Index);
            }
        }

        #endregion

        #endregion

        private void languageComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedLanguage = ((ComboBox)sender).SelectedItem.ToString();

            if (selectedLanguage == "English (US)")
                selectedLanguage = "American_english";
            else if (selectedLanguage == "English (UK)")
                selectedLanguage = "British_english";

            if (!changingLevelAtBeginning)
            {
                SetConfigurationSettingAttribute("LedConfiguration/Program/Other/Language", "name", ((ComboBox)sender).SelectedItem.ToString());
                LoadConfiguration();
            }

            //XmlTextReader languageFile = new XmlTextReader(string.Format("{0}/Languages/{1}.xml", Application.StartupPath, selectedLanguage));
            XmlTextReader languageFile = new XmlTextReader(string.Format("{0}/Languages/{1}.xml", programSettingsFolder, selectedLanguage));
            //string culture = Thread.CurrentThread.CurrentUICulture.ToString();
            LangHash.Clear();

            while (languageFile.Read())
            {
                switch (languageFile.NodeType)
                {
                    case XmlNodeType.Element:
                        //if (languageFile.Name == "Language")
                        //culture = languageFile.GetAttribute("culture");
                        //else 
                        if (languageFile.Name == "LocaleResource")
                        {
                            if (languageFile.GetAttribute("name").Contains("logMessage_"))
                            {
                                if (!logMessagesHashTable.ContainsKey(languageFile.GetAttribute("name")))
                                    logMessagesHashTable.Add(languageFile.GetAttribute("name"), languageFile.GetAttribute("value"));
                                else
                                    logMessagesHashTable[languageFile.GetAttribute("name")] = languageFile.GetAttribute("value");
                            }
                            else if (languageFile.GetAttribute("name").Contains("messageBoxMessage_") || languageFile.GetAttribute("name").Contains("messageBoxTitle_"))
                            {
                                if (!messageBoxesHashTable.ContainsKey(languageFile.GetAttribute("name")))
                                    messageBoxesHashTable.Add(languageFile.GetAttribute("name"), languageFile.GetAttribute("value"));
                                else
                                    messageBoxesHashTable[languageFile.GetAttribute("name")] = languageFile.GetAttribute("value");
                            }
                            else if (languageFile.GetAttribute("name").Contains("tooltipMessage_") || languageFile.GetAttribute("name").Contains("tooltipTitle_"))
                            {
                                if (!tooltipsHashTable.ContainsKey(languageFile.GetAttribute("name")))
                                    tooltipsHashTable.Add(languageFile.GetAttribute("name"), languageFile.GetAttribute("value"));
                                else
                                    tooltipsHashTable[languageFile.GetAttribute("name")] = languageFile.GetAttribute("value");
                            }
                            else
                            {
                                if (!LangHash.ContainsKey(languageFile.GetAttribute("name")))
                                    LangHash.Add(languageFile.GetAttribute("name"), languageFile.GetAttribute("value"));
                            }
                        }
                        break;
                }
            }

            //Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(culture);

            ApplyLocalization(this);
            SetTooltips();
        }

        private void bitmaptextRichTextBox_SelectionChanged(object sender, EventArgs e)
        {
            if (delete == true)
                return;

            if (!itemChanging)
            {
                try
                {
                delete = true;
                Font temp = bitmaptextRichTextBox.SelectionFont;

                
                    if (temp.Bold)
                    {
                        bitmaptextBoldButton.FlatStyle = FlatStyle.Flat;
                        bitmaptextBoldButton.BackColor = Color.SkyBlue;
                    }
                    else
                    {
                        bitmaptextBoldButton.FlatStyle = FlatStyle.Standard;
                        bitmaptextBoldButton.BackColor = Control.DefaultBackColor;
                    }

                    if (temp.Italic)
                    {
                        bitmaptextItalicButton.FlatStyle = FlatStyle.Flat;
                        bitmaptextItalicButton.BackColor = Color.SkyBlue;
                    }
                    else
                    {
                        bitmaptextItalicButton.FlatStyle = FlatStyle.Standard;
                        bitmaptextItalicButton.BackColor = Control.DefaultBackColor;
                    }

                    if (temp.Underline)
                    {
                        bitmaptextUnderlineButton.FlatStyle = FlatStyle.Flat;
                        bitmaptextUnderlineButton.BackColor = Color.SkyBlue;
                    }
                    else
                    {
                        bitmaptextUnderlineButton.FlatStyle = FlatStyle.Standard;
                        bitmaptextUnderlineButton.BackColor = Control.DefaultBackColor;
                    }

                    if (bitmaptextRichTextBox.SelectionAlignment == HorizontalAlignment.Center)
                    {
                        bitmaptextCenterButton.FlatStyle = FlatStyle.Flat;
                        bitmaptextRightButton.FlatStyle = FlatStyle.Standard;
                        bitmaptextLeftButton.FlatStyle = FlatStyle.Standard;

                        bitmaptextCenterButton.BackColor = Color.SkyBlue;
                        bitmaptextRightButton.BackColor = Control.DefaultBackColor;
                        bitmaptextLeftButton.BackColor = Control.DefaultBackColor;
                    }
                    else if (bitmaptextRichTextBox.SelectionAlignment == HorizontalAlignment.Left)
                    {
                        bitmaptextCenterButton.FlatStyle = FlatStyle.Standard;
                        bitmaptextRightButton.FlatStyle = FlatStyle.Standard;
                        bitmaptextLeftButton.FlatStyle = FlatStyle.Flat;

                        bitmaptextCenterButton.BackColor = Control.DefaultBackColor;
                        bitmaptextRightButton.BackColor = Control.DefaultBackColor;
                        bitmaptextLeftButton.BackColor = Color.SkyBlue;
                    }
                    else
                    {
                        bitmaptextCenterButton.FlatStyle = FlatStyle.Standard;
                        bitmaptextRightButton.FlatStyle = FlatStyle.Flat;
                        bitmaptextLeftButton.FlatStyle = FlatStyle.Standard;

                        bitmaptextCenterButton.BackColor = Control.DefaultBackColor;
                        bitmaptextRightButton.BackColor = Color.SkyBlue;
                        bitmaptextLeftButton.BackColor = Control.DefaultBackColor;
                    }

                    bitmaptextFontComboBox.SelectedItem = (object)(temp.Name.ToString());

                    if (bitmaptextFontColorComboBox.SelectedItem == null)
                        bitmaptextFontColorComboBox.SelectedIndex = 1;

                    if (bitmaptextBackgroundComboBox.SelectedItem == null)
                        bitmaptextFontColorComboBox.SelectedIndex = 0;

                    bitmaptextFontSizeComboBox.SelectedItem = (object) (Convert.ToInt32(temp.Size)).ToString();
                }
                catch (System.NullReferenceException ex)
                {
                    bitmaptextFontComboBox.SelectedItem = null;
                }
                finally
                {
                    FontSizeCheck(bitmaptextRichTextBox.SelectionLength);

                    byte changed = 1; // 1 - didn't change, 0 - subtracted, 1 - added

                    if (bitmaptextRichTextBox.SelectionLength <= 0)
                    {
                        if (bitmaptextRichTextBox.SelectionStart > 0)
                        {
                            bitmaptextRichTextBox.Select(bitmaptextRichTextBox.SelectionStart - 1, 1);
                            changed = 2;
                        }
                        else
                        {
                            bitmaptextRichTextBox.Select(bitmaptextRichTextBox.SelectionStart, 1);
                            changed = 0;
                        }
                    }

                    bitmaptextFontColorComboBox.SelectedItem = (object)(bitmaptextRichTextBox.SelectionColor.Name.ToString());
                    if(bitmaptextRichTextBox.SelectedText.Length > 0)
                        bitmaptextBackgroundComboBox.SelectedItem = (object)((GetBackGroundColorFromRTF(bitmaptextRichTextBox.SelectedRtf)).Name.ToString());
                    //else
                        //bitmaptextBackgroundComboBox.SelectedItem = (object)(bitmaptextRichTextBox.BackColor.Name.ToString());


                    if (changed != 1)
                    {
                        if (changed == 2)
                            bitmaptextRichTextBox.Select(bitmaptextRichTextBox.SelectionStart + 1, 0);
                        else if (changed == 0)
                            bitmaptextRichTextBox.Select(bitmaptextRichTextBox.SelectionStart, 0);

                        changed = 1;
                    }

                    delete = false;
                }
            }
        }

        private Color GetBackGroundColorFromRTF(string rtf)
        {
            Color bgColor = Color.Black;
            List<Color> usedColors = new List<Color>();
            
            int colorDefStart, colorDefEnd, index;
            byte R = 0, G = 0, B = 0, iteration;
            string colorDef, colorComponentName = null;

            try
            {
                colorDefStart = rtf.IndexOf("colortbl");
                colorDefEnd = rtf.IndexOf(";}", colorDefStart);

                colorDef = rtf.Substring(colorDefStart, colorDefEnd - colorDefStart + 1);
                for (index = 11; index < colorDef.Length; )
                {
                    if (String.IsNullOrEmpty(colorComponentName) && !String.Equals(colorDef[index], '\\'))
                        colorComponentName = colorDef[index].ToString();
                    else if (!String.Equals(colorDef[index], '\\'))
                        colorComponentName += colorDef[index].ToString();

                    switch (colorComponentName)
                    {
                        case "red":
                            iteration = 100;
                            while (!string.Equals(colorDef[++index], '\\'))
                            {
                                R += Convert.ToByte(Convert.ToByte(colorDef[index].ToString()) * iteration);
                                iteration /= 10;
                            }
                            colorComponentName = null;
                            break;
                        case "green":
                            iteration = 100;
                            while (!string.Equals(colorDef[++index], '\\'))
                            {
                                G += Convert.ToByte(Convert.ToByte(colorDef[index].ToString()) * iteration);
                                iteration /= 10;
                            }
                            colorComponentName = null;
                            break;
                        case "blue":
                            iteration = 100;
                            while (!string.Equals(colorDef[++index], ';'))
                            {
                                B += Convert.ToByte(Convert.ToByte(colorDef[index].ToString()) * iteration);
                                iteration /= 10;
                            }

                            usedColors.Add(Color.FromArgb(R, G, B));
                            R = B = G = 0;
                            colorComponentName = null;
                            break;
                    }
                    ++index;
                }

                index = rtf.LastIndexOf("highlight") + 9;
                index = Convert.ToByte(rtf[index].ToString()) - 1;

                if (index == -1)
                    ++index;

                bgColor = usedColors[index];

                foreach (string s in allcolours)
                {
                    Color tmp = Color.FromName(s);
                    if (tmp.ToArgb() == bgColor.ToArgb())
                    {
                        bgColor = tmp;
                        break;
                    }
                }
            }
            catch
            {
                Color fgColor = Color.FromName(bitmaptextFontColorComboBox.Items[bitmaptextFontColorComboBox.SelectedIndex].ToString());

                if(fgColor.ToArgb() != Color.Black.ToArgb())
                    bgColor = Color.Black;
                else
                    bgColor = Color.FromName(bitmaptextFontColorComboBox.Items[(bitmaptextFontColorComboBox.SelectedIndex + 1) % bitmaptextFontColorComboBox.Items.Count].ToString());
            }

            

            return bgColor;
        }

        private void SetBackGroudColorViaRTF(Color ColorToSet)
        {
            try
            {
                byte R = ColorToSet.R, G = ColorToSet.G, B = ColorToSet.B;

                string rtf = bitmaptextRichTextBox.SelectedRtf;

                int RTFstart = rtf.IndexOf("colortbl"), RTFend = rtf.IndexOf(";}", RTFstart), color = 0, index = 0;

                string OldColorDefinition = rtf.Substring(RTFstart, RTFend - RTFstart + 2);
                string newColorDefinition = OldColorDefinition;

                newColorDefinition = newColorDefinition.Insert(newColorDefinition.Length - 2, String.Format(";\\red{0}\\green{1}\\blue{2}", R, G, B));

                RTFstart = 0;
                RTFend = newColorDefinition.LastIndexOf("red");
                while (RTFstart != -1 && RTFstart <= RTFend)
                {
                    RTFstart = newColorDefinition.IndexOf("red", RTFstart) + 3;
                    ++color;
                }

                rtf = rtf.Replace(OldColorDefinition, newColorDefinition);

                RTFstart = rtf.LastIndexOf("highlight");
                while (index != -1 && index <= RTFstart)
                {
                    index = rtf.IndexOf("highlight", index) + 9;

                    rtf = rtf.Remove(index, 1);
                    rtf = rtf.Insert(index, color.ToString());
                }

                bitmaptextRichTextBox.SelectedRtf = rtf;
            }
            catch { }
        }

        #region Tree events

        private void playbill_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag.ToString() == "program")
            {
                CurrentProgramNumber = e.Node.Index;
            }
            else if (e.Node.Tag.ToString() == "window")
            {
                CurrentProgramNumber = e.Node.Parent.Index;
                CurrentWindowNumber = e.Node.Index;  
            }
            else if (e.Node.Tag.ToString() == "item")
            {
                CurrentProgramNumber = e.Node.Parent.Parent.Index;
                CurrentWindowNumber = e.Node.Parent.Index;  
                CurrentItemNumber = e.Node.Index;  
            }

            IsCurrentElementNew = (e.Node.Name == "new");


            // Somewere here is bug connected with rtb text changing!
            bool newNode = (e.Node.Name == "new");

            OpenTabForNode(e.Node);
            Font tmpFont = (e.Node.NodeFont == null) ? ((TreeView)sender).Font : e.Node.NodeFont;
            //((TreeView)sender).SelectedNode.NodeFont = new Font(tmpFont.Name, tmpFont.Size, tmpFont.Style ^ FontStyle.Bold);
            e.Node.BackColor = Color.SkyBlue;


            //OpenTabForNode(((TreeView)sender).SelectedNode);
            //Font tmpFont = (((TreeView)sender).SelectedNode.NodeFont == null) ? ((TreeView)sender).Font : ((TreeView)sender).SelectedNode.NodeFont;
            ////((TreeView)sender).SelectedNode.NodeFont = new Font(tmpFont.Name, tmpFont.Size, tmpFont.Style ^ FontStyle.Bold);
            //((TreeView)sender).SelectedNode.BackColor = Color.SkyBlue;

            if (newNode && e.Node.Nodes.Count == 1)
            {
                playbillTree.SelectedNode = e.Node.Nodes[0];

                //playbillTree.SelectedNode = e.Node;
            }
        }

        private void playbill_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            TreeNode SelectedNode = playbillTree.SelectedNode;
            TreeNode tmpnode = e.Node;


            if (SelectedNode != null)
            {
                if (tmpnode.Tag.ToString() == "window" && tmpnode.Name == "new"
                    && CurrentPlaybill.ProgramsList[tmpnode.Parent.Index].WindowsList.Count == 10)
                {
                    MessageBox.Show(messageBoxesHashTable["messageBoxMessage_only10Windows"].ToString(), "LedConfig",
                                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    e.Cancel = true;
                    return;
                }

                Color tmpColor = Color.Empty;
                if (SelectedNode.BackColor != tmpColor)
                    ((TreeView)sender).SelectedNode.BackColor = tmpColor;

                if (SelectedNode.NodeFont != null && SelectedNode.NodeFont.Bold == true)
                {
                    Font tmpFont = SelectedNode.NodeFont;
                    //SelectedNode.NodeFont = new Font(tmpFont.Name, tmpFont.Size, tmpFont.Style ^ FontStyle.Bold);
                }
            }
            //if (SelectedNode != null && delete == false && levelChanging == false)
            //    if ((string)SelectedNode.Tag == "item" && testPlaybill.ProgramsList[SelectedNode.Parent.Parent.Index].WindowsList[SelectedNode.Parent.Index].ItemsList[SelectedNode.Index].ItemType == PlayWindowItemType.BitmapText)
            //        testPlaybill.ProgramsList[SelectedNode.Parent.Parent.Index].WindowsList[SelectedNode.Parent.Index].ItemsList[SelectedNode.Index].Rtf = bitmaptextRichTextBox.Rtf;
            delete = false;


            for (int i = 0; i < PreviewWindow.programsWithWindows.Count; i++)
                previewWindow.StopProgram(i);
        }

        private void playbillTree_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Keys.Delete.GetHashCode())
                RemovePlaybillNode(((TreeView)sender).SelectedNode);
        }

        private void playbillTree_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Delete && ((TreeView)sender).SelectedNode.Name != "mainNode")
            {
                DialogResult result = MessageBox.Show(messageBoxesHashTable["messageBoxMessage_deleteItem"].ToString(),
                    messageBoxesHashTable["messageBoxTitle_deleteItem"].ToString(),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == System.Windows.Forms.DialogResult.Yes)
                    RemovePlaybillNode(((TreeView)sender).SelectedNode);
            }
        }
        #endregion

        private void numbersOnlyTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
                e.Handled = true;
        }

        #region Window's settings changing
        private void windowNameTextBox_TextChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int programNumber = selectedNode.Parent.Index;
                string windowName = ((System.Windows.Forms.TextBox)(sender)).Text;

                CurrentWindow.Name = windowName;
                playbillTree.SelectedNode.Text = windowName;
            }
        }

        private void windowXTextBox_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(((System.Windows.Forms.TextBox)(sender)).Text))
                return;

            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int programNumber = selectedNode.Parent.Index;

                if (CurrentWindow.X != uint.Parse(((System.Windows.Forms.TextBox)(sender)).Text))
                    windowsizechanged = true;
            }
        }

        private void windowWidthTextBox_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(((System.Windows.Forms.TextBox)(sender)).Text))
                return;

            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int programNumber = selectedNode.Parent.Index;

                if (CurrentWindow.Width != uint.Parse(((System.Windows.Forms.TextBox)(sender)).Text))
                    windowsizechanged = true;
            }
        }

        private void windowYTextBox_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(((System.Windows.Forms.TextBox)(sender)).Text))
                return;

            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                if (CurrentWindow.Y != uint.Parse(((System.Windows.Forms.TextBox)(sender)).Text))
                    windowsizechanged = true;
            }
        }

        private void windowHeightTextBox_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(((System.Windows.Forms.TextBox)(sender)).Text))
                return;

            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                if (CurrentWindow.Height != uint.Parse(((System.Windows.Forms.TextBox)(sender)).Text))
                    windowsizechanged = true;
            }
        }
        #endregion
        #region Program's settings changing
        private void programNameTextBox_TextChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                string programName = ((System.Windows.Forms.TextBox)(sender)).Text;

                testPlaybill.ProgramsList[selectedNode.Index].Name =
                        programName;
                playbillTree.SelectedNode.Text = programName;
            }
        }

        private void programWidthTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void programHeightTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void runningCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            runningGroupBox.Enabled = ((CheckBox)(sender)).Checked;
            CurrentPlaybill.ProgramsList[playbillTree.SelectedNode.Index].LTP = ((CheckBox)(sender)).Checked;

            CurrentPlaybill.SendLPT = CurrentPlaybill.ProgramsList[playbillTree.SelectedNode.Index].LTP;
        }

        private void runningDateTimeBegin_ValueChanged(object sender, EventArgs e)
        {
            CurrentPlaybill.ProgramsList[playbillTree.SelectedNode.Index].BeginHour = (byte)((DateTimePicker)(sender)).Value.Hour;
            CurrentPlaybill.ProgramsList[playbillTree.SelectedNode.Index].BeginMinute = (byte)((DateTimePicker)(sender)).Value.Minute;
        }

        private void runningDateTimeEnd_ValueChanged(object sender, EventArgs e)
        {
            CurrentPlaybill.ProgramsList[playbillTree.SelectedNode.Index].EndHour = (byte)((DateTimePicker)(sender)).Value.Hour;
            CurrentPlaybill.ProgramsList[playbillTree.SelectedNode.Index].EndMinute = (byte)((DateTimePicker)(sender)).Value.Minute;
        }

        private void runningDayscheckedListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool[] newDays = new bool[7] { false, false, false, false, false, false, false };

            foreach (int index in runningDayscheckedListBox.CheckedIndices)
                newDays[index] = true;

            CurrentPlaybill.ProgramsList[playbillTree.SelectedNode.Index].WeekDays = newDays;
        }



        #endregion

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (logoFromFile != null)
                logoFromFile.Dispose();

            if (ConfigHashtable != null && Convert.ToBoolean(ConfigHashtable["AutoSendTime"].ToString()) 
                && Communication.Type != CommunicationType.None)
            {
                logWindow.Show();
                logWindow.Clearlog();

                automaticallySetTime();

                logWindow.Hide();
            }

            //TreeNode SelectedNode = playbillTree.SelectedNode;
            //if (SelectedNode != null)
            //{
            //    if (SelectedNode.Tag.ToString() == "item"
            //        && CurrentPlaybill.ProgramsList[SelectedNode.Parent.Parent.Index].WindowsList[SelectedNode.Parent.Index].ItemsList[SelectedNode.Index].ItemType == PlayWindowItemType.BitmapText)
            //    {
            //        CurrentPlaybill.ProgramsList[SelectedNode.Parent.Parent.Index].WindowsList[SelectedNode.Parent.Index].ItemsList[SelectedNode.Index].Rtf = bitmaptextRichTextBox.Rtf;
            //    }
            //}
            //if (UserLevel == ApplicationUserLevel.Beginner)
            //{
            //    CurrentPlaybill.ProgramsList[0].WindowsList[0].ItemsList[0].Rtf = bitmaptextRichTextBox.Rtf;
            //}

            try
            {

                System.IO.Stream tmp;

                if (UserLevel == ApplicationUserLevel.Beginner)
                    tmp = File.Open(AutosaveFileName, FileMode.Create);
                else
                    tmp = File.Open(NotBeginnerAutosaveFileName, FileMode.Create);

                CurrentPlaybill.SaveProgramsToFile(tmp);
                tmp.Close();
            }
            catch { }

            //previewWindow.Close();
            try
            {

                previewWindow.ClearProgramsList();
                if (previewWindow.screenBitmap != null)
                {
                    previewWindow.screenBitmap.Dispose();
                    previewWindow.screenBitmap = null;
                }
                if (previewWindow.ghostBitmap != null)
                {
                    previewWindow.ghostBitmap.Dispose();
                    previewWindow.ghostBitmap = null;
                }


                if (backgroundImage != null)
                {
                    backgroundImage.Dispose();
                    backgroundImage = null;
                }

                if (beginnerBackgroundImage != null)
                {
                    beginnerBackgroundImage.Dispose();
                    beginnerBackgroundImage = null;
                }

                if (testVersionTimer != null)
                {
                    testVersionTimer.Stop();
                    testVersionTimer.Dispose();
                    testVersionTimer = null;
                }

                if (regeneratePreviewTimer != null)
                {
                    regeneratePreviewTimer.Stop();
                    regeneratePreviewTimer.Dispose();
                    regeneratePreviewTimer = null;
                }

                if (clockCurrentTimeTimer != null)
                {
                    clockCurrentTimeTimer.Stop();
                    clockCurrentTimeTimer.Dispose();
                    clockCurrentTimeTimer = null;
                }

                if (programCurrentTimeTimer != null)
                {
                    programCurrentTimeTimer.Stop();
                    programCurrentTimeTimer.Dispose();
                    programCurrentTimeTimer = null;
                }
            }
            catch { /*System.Diagnostics.Debugger.Break();*/ }
        }
        #endregion

        #region rest

        private void bitmaptextRichTextBox_ContentsResized(object sender, ContentsResizedEventArgs e)
        {

            contentRectangle = e.NewRectangle;

        }

        private void bitmaptextButton_Click(object sender, EventArgs e)
        {
            RtbToBitmap(playbillTree.SelectedNode);
        }

        private int StringLength(Graphics g, string s, Font f)
        {

            StringFormat sf = (StringFormat)StringFormat.GenericTypographic.Clone();

            sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;



            return (int)g.MeasureString(s, f, 1024, sf).Width;

        }

        private void DrawOnBaseline(string s, Graphics g, Font f, Brush b, Point pos)
        {

            float baselineOffset = f.SizeInPoints / f.FontFamily.GetEmHeight(f.Style) * f.FontFamily.GetCellAscent(f.Style);

            float baselineOffsetPixels = g.DpiY / 72f * baselineOffset;



            g.DrawString(s, f, b, new Point(pos.X, pos.Y - (int)(baselineOffsetPixels + 0.5f)), StringFormat.GenericTypographic);

        }

        static public int MeasureDisplayStringWidth(Graphics graphics, string text, Font font)
        {
            const int width = 32;

            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(width, 1,
                                                                        graphics);
            System.Drawing.SizeF size = graphics.MeasureString(text, font);
            System.Drawing.Graphics anagra = System.Drawing.Graphics.FromImage(bitmap);

            int measured_width = (int)size.Width;

            if (anagra != null)
            {
                anagra.Clear(Color.White);
                anagra.DrawString(text + "|", font, Brushes.Black,
                                   width - measured_width, -font.Height / 2);

                for (int i = width - 1; i >= 0; i--)
                {
                    measured_width--;
                    if (bitmap.GetPixel(i, 0).R != 255)    // found a non-white pixel ?

                        break;
                }
            }

            return measured_width;
        }

        static public int MeasureFirstCharacterOffset(Graphics graphics, string text, Font font)
        {
            const int width = 132;

            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(width, 1,
                                                                        graphics);
            System.Drawing.Graphics anagra = System.Drawing.Graphics.FromImage(bitmap);

            int measured_offset = 0;

            if (anagra != null)
            {
                anagra.Clear(Color.White);
                anagra.DrawString("|" + text, font, Brushes.Black,
                                   0, -font.Height / 2);

                for (int i = 0; i <= width; ++i)
                {
                    measured_offset++;
                    if (bitmap.GetPixel(i, 0).R != 255)    // found a non-white pixel ?

                        break;
                }
            }

            return measured_offset;
        }

        static public int MeasureLastCharacterOffset(Graphics graphics, string text, Font font)
        {
            const int width = 132;

            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(width, 1,
                                                                        graphics);
            System.Drawing.SizeF size = graphics.MeasureString(text, font);
            System.Drawing.Graphics anagra = System.Drawing.Graphics.FromImage(bitmap);

            int measured_width = 0; //(int)size.Width;

            if (anagra != null)
            {
                anagra.Clear(Color.White);
                anagra.DrawString(text + "|", font, Brushes.Black,
                                   width - size.Width, -font.Height / 2);

                for (int i = width - 1; i >= 0; i--)
                {
                    measured_width++;
                    if (bitmap.GetPixel(i, 0).R != 255)    // found a non-white pixel ?

                        break;
                }
            }

            return measured_width;
        }

        public void RtbToBitmap(TreeNode current)
        {
            #region Declarations
            delete = true;
        //    TreeNode current = playbillTree.SelectedNode;
            if (current != null && current.Text == "BitmapText")
            {

             //   RichTextBox rtb = copyRTB(bitmaptextRichTextBox);

                RichTextBox rtb; 

                if(!string.IsNullOrEmpty(CurrentPlaybill.ProgramsList[current.Parent.Parent.Index].WindowsList[current.Parent.Index].ItemsList[current.Index].Rtf))
                    rtb = generateRTBfromRTF(CurrentPlaybill.ProgramsList[current.Parent.Parent.Index].WindowsList[current.Parent.Index].ItemsList[current.Index].Rtf);
                else
                    rtb = generateRTBfromItem(CurrentPlaybill.ProgramsList[current.Parent.Parent.Index].WindowsList[current.Parent.Index].ItemsList[current.Index]);


                PlayWindow window = CurrentPlaybill.ProgramsList[current.Parent.Parent.Index].WindowsList[current.Parent.Index];
                PlayWindowItem playbillItem = CurrentPlaybill.ProgramsList[current.Parent.Parent.Index].WindowsList[current.Parent.Index].ItemsList[current.Index];

                Bitmap bmp = new Bitmap((int)window.Width,
                                        (int)window.Height);

                Graphics gr = Graphics.FromImage(bmp);

                List<Bitmap> bmpList = new List<Bitmap>();

                int itemNumber = (current.Tag.ToString() == "item") ? current.Index : 0;
                previewWindow.SelectedWindow.Animations[itemNumber].Clear();
                previewWindow.SelectedWindow.Animations[itemNumber].InputBitmaps = null;

                bool scroll = false;
                int effectNumber = CurrentPlaybill.ProgramsList[current.Parent.Parent.Index].WindowsList[current.Parent.Index].ItemsList[current.Index].Effect;


                if (Enum.GetName(typeof(EffectType), effectNumber).Contains("Scroll"))
                    scroll = true;

                if (scroll == false)
                {

                    rtb.Width = (int)window.Width+5 - window.ItemsList[current.Index].Offset.X;
                    rtb.WordWrap = true;

                //    bitmaptextRichTextBox.Width = (int)bmp.Width - window.ItemsList[current.Index].Offset.X;

                    gr.FillRectangle(Brushes.Black, new Rectangle(0, 0, bmp.Width, bmp.Height));

                    gr.SmoothingMode = SmoothingMode.HighSpeed;
                    gr.PageUnit = GraphicsUnit.Pixel;

                    gr.Clear(Color.Black);//Uncomment when shure it's all save & sound.

                }

                // Variables to navigate inside bmp
                int currentOffset = 0;//playbillItem.Offset.X;//0;
                int x = 0;//playbillItem.Offset.X;//0;
                float y = playbillItem.Offset.Y; //0; 
                float relY = y; // Relative y, that'll help to draw multi sized line on the same level.

                // Variables to navigate inside of richtextbox control

                int line = 0;
                int charpos = 0;

                // Properties of current line of text, and  current text fragment to draw.

                float maxSize = 0;
                float curSize = 0; 

                int kerning = 0; 
                float maxdesc = 0; // Font Descent

                // Variables that contains properties of text, and the text itself

                string textToDraw = null;
                Font fontToDraw = null; 
                Color colorToFill = Color.Black;
                Color colorToDraw = Color.Black;

                #endregion

                if (scroll == false)
                {
                    if (oldbitmaps)
                    {
                        #region Original way
                        try
                        {
                            //RichTextBox rtb = copyRTB(bitmaptextRichTextBox);

                            #region Creating bmp - final revolution

                            TreeNode selectedNode = current;
                            if (selectedNode != null && previewWindow.SelectedWindow != null)
                            {
                                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                                /*
                                 * Original itemNumber variable used to create bitmap. This definition is commented out dute to problems with time counter.
                                 * 
                                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;
                                */
                                //PlayWindow window = testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber];

                                //int itemNumber;
                                if (selectedNode.Tag.ToString() == "item")
                                {
                                    itemNumber = selectedNode.Index;
                                    window.ItemsList[itemNumber].Rtf = rtb.Rtf;
                                }
                                else
                                {
                                    itemNumber = 0;
                                    rtb.Rtf = window.ItemsList[itemNumber].Rtf;
                                }

                                previewWindow.SelectedWindow.Animations[itemNumber].Clear();
                                previewWindow.SelectedWindow.Animations[itemNumber].InputBitmaps = null;

                                // Here was oryginal definitionof window variable (copy pasted right after definition of programNumber variable
                                // And also here was instruction copy-pasted right after itemNumber = selectedNode.Index; instruction above.

                                rtb.ContentsResized += new System.Windows.Forms.ContentsResizedEventHandler(this.bitmaptextRichTextBox_ContentsResized);

                                rtb.Width = (int)window.Width - window.ItemsList[itemNumber].Offset.X + 6;

                                //while (contentRectangle.Width != (int)window.Width)
                                //{
                                //    rtb.Width = rtb.Width + (int)window.Width - contentRectangle.Width;
                                //    if (rtb.Width == 0)
                                //    {
                                //        rtb.Width = (int)window.Width;
                                //        if (rtb.Width < contentRectangle.Width)
                                //            contentRectangle.Width = rtb.Width;
                                //        break;
                                //    }
                                //}
                                // A while instruction above guards that text use all available space in terms of width.
                                // after all sadly it's unnecessary.

                                RichTextBox bitmap_rtb = copyRTB(rtb);
                                List<Bitmap> bitmapsList = new List<Bitmap>();

                                int lines = bitmap_rtb.GetLineFromCharIndex(Int32.MaxValue);
                                int i = 0;
                                do
                                {
                                    if (bitmap_rtb.GetFirstCharIndexFromLine(i + 1) != -1)
                                        bitmap_rtb.Select(bitmap_rtb.GetFirstCharIndexFromLine(i), bitmap_rtb.GetFirstCharIndexFromLine(i + 1) - bitmap_rtb.GetFirstCharIndexFromLine(i) - 1);
                                    else
                                        bitmap_rtb.Select(bitmap_rtb.GetFirstCharIndexFromLine(i), bitmap_rtb.Text.Length);

                                    rtb.Rtf = bitmap_rtb.SelectedRtf;
                                    rtb.SelectionAlignment = bitmap_rtb.SelectionAlignment;


                                    //Bitmap 
                                        bmp = new Bitmap(contentRectangle.Width, contentRectangle.Height);

                                    using (gr = Graphics.FromImage(bmp))
                                    {
                                        gr.Clear(Color.FromName(testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[selectedNode.Index].BgColor));
                                        if (bitmap_rtb.Text.Length > 0)
                                        {
                                            if (bitmap_rtb.GetFirstCharIndexFromLine(i + 1) != -1)
                                            {
                                                //if (i + 1 < lines)
                                                {
                                                    //if (rtb.Lines[i + 1].Length == 1)
                                                    bitmap_rtb.Select(bitmap_rtb.GetFirstCharIndexFromLine(i + 1) - 1, 1);
                                                    //else
                                                    //  bitmap_rtb.Select(bitmap_rtb.GetFirstCharIndexFromLine(i + 1) - 2, 1);
                                                }
                                                //else
                                                //{
                                                //    bitmap_rtb.Select(bitmap_rtb.Lines[i].Length - 1, 1);
                                                //}

                                            }
                                            else
                                                bitmap_rtb.Select(bitmap_rtb.Text.Length - 1, 1);
                                        }
                                        //gr.Clear(bitmap_rtb.SelectionBackColor);
                                        rtb.BackColor = bitmap_rtb.SelectionBackColor;

                                        gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                                        IntPtr hDC = gr.GetHdc();
                                        FORMATRANGE fmtRange;
                                        RECT rect;
                                        int fromAPI;
                                        rect.Top = 0; rect.Left = 0;
                                        rect.Bottom = (int)(bmp.Height + (bmp.Height * inch));
                                        rect.Right = (int)(bmp.Width + (bmp.Width * inch));
                                        fmtRange.chrg.cpMin = 0;
                                        fmtRange.chrg.cpMax = bitmaptextRichTextBox.Text.Length;
                                        fmtRange.hdc = hDC;
                                        fmtRange.hdcTarget = hDC;
                                        fmtRange.rc = rect;
                                        fmtRange.rcPage = rect;
                                        int wParam = 1;
                                        IntPtr lParam = Marshal.AllocCoTaskMem(Marshal.SizeOf(fmtRange));
                                        Marshal.StructureToPtr(fmtRange, lParam, false);
                                        fromAPI = SendMessage(rtb.Handle, EM_FORMATRANGE, wParam, lParam);
                                        Marshal.FreeCoTaskMem(lParam);
                                        fromAPI = SendMessage(rtb.Handle, EM_FORMATRANGE, wParam, new IntPtr(0));
                                        gr.ReleaseHdc(hDC);
                                    }

                                    if (selectedNode.Tag.ToString() == "item")
                                        window.ItemsList[itemNumber].FilePath = string.Format("Font {0}{1}{2}.bmp", selectedNode.Parent.Index, selectedNode.Index, i);

                                    //bmp.Save(window.ItemsList[itemNumber].FilePath, System.Drawing.Imaging.ImageFormat.Bmp);
                                    bitmapsList.Add(bmp);

                                    ++i;
                                } while (i <= lines);

                                window.ItemsList[itemNumber].BitmapsList = bitmapsList;
                                previewWindow.SelectedWindow.Animations[itemNumber].InputBitmaps = bitmapsList;

                                previewWindow.SelectedWindow.Animate(false, itemNumber);

                                foreach (Bitmap b in bitmapsList)
                                    b.Dispose();
                            }

                            #endregion
                        }
                        catch { }
                        #endregion
                    }
                    else
                    {
                        #region Algorithm for non scrolling effects

                        while (line <= rtb.GetLineFromCharIndex(rtb.TextLength))
                        {
                            #region Line initialization
                            try
                            {
                                gr.Flush();
                            }
                            catch (Exception)
                            {
                                gr = Graphics.FromImage(bmp);
                                gr.Clear(Color.Black);
                            }

                            maxdesc = 0;
                            textToDraw = null;

                            //Instructions to search line for biggest linespace goes here
                            // Point is to draw background at proper height, it should be useful to properly set horizontal align for smaller chars
                            for (int i = rtb.GetFirstCharIndexFromLine(line); line == rtb.GetLineFromCharIndex(i);)
                            {
                                if (i > rtb.TextLength)
                                    break;

                                rtb.Select(i, 1);

                              
                                if (maxSize < gr.MeasureString(rtb.SelectedText, rtb.SelectionFont).Height)
                                    maxSize = gr.MeasureString(rtb.SelectedText, rtb.SelectionFont).Height;

                                if (string.IsNullOrEmpty(textToDraw) == true)
                                {
                                    textToDraw = rtb.SelectedText;
                                    fontToDraw = rtb.SelectionFont;
                                    colorToFill = rtb.SelectionBackColor;
                                    colorToDraw = rtb.SelectionColor;

                                    ++i;
                                    continue;
                                }
                                else if (rtb.SelectedText != "\n" && i < rtb.TextLength && fontToDraw.Name.Equals(rtb.SelectionFont.Name) && fontToDraw.Style.Equals(rtb.SelectionFont.Style) && fontToDraw.Size.Equals(rtb.SelectionFont.Size)
                                    && colorToFill.Equals(rtb.SelectionBackColor) && colorToDraw.Equals(rtb.SelectionColor))
                                {
                                    textToDraw += rtb.SelectedText;
                                    ++i;

                                    if (rtb.GetLineFromCharIndex(i) > line)
                                    {
                                        if (textToDraw.EndsWith(" "))
                                            maxdesc += Convert.ToInt32(gr.MeasureString(textToDraw, fontToDraw).Width) + Convert.ToInt32(gr.MeasureString(" ", fontToDraw).Width);
                                        else
                                            maxdesc += Convert.ToInt32(gr.MeasureString(textToDraw, fontToDraw).Width);// - MeasureLastCharacterOffset(gr,textToDraw,fontToDraw);

                                        if (i >= rtb.TextLength && !(textToDraw.EndsWith(" ")))
                                            maxdesc -= MeasureLastCharacterOffset(gr, textToDraw, fontToDraw);


                                        textToDraw = null;
                                    }

                                    continue;
                                }
                                else
                                {
                                    //float txtw = gr.MeasureString(textToDraw, fontToDraw).Width;

                                    //if (!string.IsNullOrEmpty(textToDraw))
                                    //{
                                    //    char lastletter = textToDraw[textToDraw.Length - 1];
                                    //    float lastw = gr.MeasureString(lastletter.ToString(), fontToDraw).Width;
                                    //    float doubledlastw = gr.MeasureString(lastletter.ToString() + lastletter.ToString(), fontToDraw).Width;
                                    //    float tripledlastw = gr.MeasureString(lastletter.ToString() + lastletter.ToString() + lastletter.ToString(), fontToDraw).Width;

                                    //    maxdesc += Convert.ToInt32(txtw - (2 * lastw - doubledlastw));
                                    //}
                                    //else
                                    //    maxdesc += Convert.ToInt32(txtw);

                                    if (textToDraw.EndsWith(" "))
                                        maxdesc += Convert.ToInt32(gr.MeasureString(textToDraw, fontToDraw).Width) + Convert.ToInt32(gr.MeasureString(" ", fontToDraw).Width);
                                    else
                                        maxdesc += Convert.ToInt32(gr.MeasureString(textToDraw, fontToDraw).Width);// - MeasureLastCharacterOffset(gr,textToDraw,fontToDraw);

                                    if (i >= rtb.TextLength && !(textToDraw.EndsWith(" ")))
                                        maxdesc -= MeasureLastCharacterOffset(gr, textToDraw, fontToDraw);


                                    textToDraw = null;
                                }
                                
                            }

                            // Instructions to initialize x with value corresponding to horizontalAlignment od current line.

                            charpos = rtb.GetFirstCharIndexFromLine(line);
                            rtb.Select(charpos, 1);

                            if (rtb.SelectionAlignment == HorizontalAlignment.Left)
                            {
                                x = -2;
                            }
                            else if (rtb.SelectionAlignment == HorizontalAlignment.Right)
                            {
                                double tmp;
                                tmp = bmp.Width - maxdesc;
                                x = Convert.ToInt32(tmp);
                            }
                            else if (rtb.SelectionAlignment == HorizontalAlignment.Center)
                            {
                                double tmp;
                                tmp = bmp.Width - maxdesc;
                                tmp /= 2.0;
                                x = Convert.ToInt32(tmp);
                            }

                            //fill the gap from 0 to text start with backcolor of first sign
                            colorToFill = rtb.SelectionBackColor;
                            colorToDraw = rtb.SelectionColor;
                            if (line == 0)
                                y -= playbillItem.Offset.Y;
                            gr.FillRectangle(new SolidBrush(colorToFill), new RectangleF(0, y, window.Width, window.Height - y));
                            if (line == 0)
                                y += playbillItem.Offset.Y;

                            x += playbillItem.Offset.X;

                            fontToDraw = null;
                            textToDraw = null;
                            colorToFill = Color.Black;
                            colorToDraw = Color.Black;
                            #endregion

                            while (line == rtb.GetLineFromCharIndex(charpos))
                            {

                                if (charpos > rtb.Text.Length)
                                    break;

                                relY = y + maxSize; // Another try, this time relY is Y value of baseline of biggest characeter in line. So relY is lowest point in line.

                                // Instructions to draw string goes here
                                // It should be modificated to y'in for multi-size characters.
                                // And then for right and center aligned text's.
                                rtb.Select(charpos, 1);

                                if (string.IsNullOrEmpty(textToDraw))
                                {
                                    textToDraw = rtb.SelectedText;
                                    fontToDraw = rtb.SelectionFont;
                                    colorToFill = rtb.SelectionBackColor;
                                    colorToDraw = rtb.SelectionColor;
                                }

                                // Instructions to peek if next char font is the same

                                rtb.Select(++charpos, 1);

                                if (fontToDraw.Name.Equals(rtb.SelectionFont.Name) && fontToDraw.Style.Equals(rtb.SelectionFont.Style) && fontToDraw.Size.Equals(rtb.SelectionFont.Size)
                                    && colorToFill.Equals(rtb.SelectionBackColor) && colorToDraw.Equals(rtb.SelectionColor)
                                    && charpos < rtb.Text.Length && rtb.GetLineFromCharIndex(charpos) == line)
                                {
                                    textToDraw += rtb.SelectedText;
                                    continue;
                                }
                                else
                                {
                                    #region Drawing & changing X


                                    if (true)
                                    {
                                        int firstCharoffset = MeasureFirstCharacterOffset(gr, textToDraw, fontToDraw);
                                        int lastCharoffset = MeasureLastCharacterOffset(gr, textToDraw, fontToDraw);

                                        curSize = gr.MeasureString(textToDraw, fontToDraw).Height;

                                        if (textToDraw.EndsWith(" "))// || textToDraw.EndsWith("\n"))
                                            kerning = Convert.ToInt32(gr.MeasureString(" ", fontToDraw).Width); // Convert.ToInt32(gr.MeasureString(textToDraw, fontToDraw).Width) +
                                        else if (textToDraw.EndsWith("\n"))
                                            kerning = Convert.ToInt32(gr.MeasureString(textToDraw, fontToDraw).Width);// - lastCharoffset;
                                        else
                                            kerning = Convert.ToInt32(gr.MeasureString(textToDraw, fontToDraw).Width); //Convert.ToInt32(MeasureDisplayStringWidth(gr, textToDraw, fontToDraw));//; -lastCharoffset;
                                            //kerning = Convert.ToInt32(MeasureDisplayStringWidth(gr, textToDraw, fontToDraw)) - lastCharoffset;


                                        if (line == 0)
                                            y -= playbillItem.Offset.Y;
                                        
                                        gr.FillRectangle(new SolidBrush(colorToFill), new RectangleF(x, y, window.Width - x, window.Height - y));
                                        
                                        if (line == 0)
                                            y += playbillItem.Offset.Y;


                                        if (x < 0.0) //|| textToDraw.EndsWith(" ")
                                        {
                                            gr.DrawString(textToDraw, fontToDraw, new SolidBrush(colorToDraw), new RectangleF(x, (y + (maxSize - curSize)), (gr.MeasureString(textToDraw, fontToDraw).Width), Convert.ToInt32(maxSize)));
                                        }
                                        else
                                        {
                                            gr.DrawString(textToDraw, fontToDraw, new SolidBrush(colorToDraw), new RectangleF(x - firstCharoffset / (float)1.5, (y + (maxSize - curSize)), (gr.MeasureString(textToDraw, fontToDraw).Width), Convert.ToInt32(maxSize)));
                                        }


                                        x += kerning;

                                    }
                                    else
                                    {
                                        gr.FillRectangle(new SolidBrush(colorToFill), new RectangleF(currentOffset, y, window.Width - currentOffset, window.Height - y));
                                        DrawOnBaseline(textToDraw, gr, fontToDraw, new SolidBrush(colorToDraw), new Point(currentOffset, (int)20));
                                        currentOffset += this.StringLength(gr, textToDraw, fontToDraw);
                                    }

                                    textToDraw = null;
                                    #endregion
                                }
                            }

                            // Instructions to go to new line goes here
                            // Instruction for splitting bitmaps are in try-catch 
                            y += Convert.ToInt32(maxSize);

                            ++line;

                            #region Cut bitmap into pieces
                            try
                            {
                                charpos = rtb.GetFirstCharIndexFromLine(line);
                                rtb.Select(charpos, 1);

                                if (rtb.SelectionAlignment == HorizontalAlignment.Right)
                                    x = bmp.Width;
                                else if (rtb.SelectionAlignment == HorizontalAlignment.Left)
                                    x = 0;

                                if (y + (rtb.SelectionFont.Size * rtb.SelectionFont.FontFamily.GetLineSpacing(rtb.SelectionFont.Style) / rtb.SelectionFont.FontFamily.GetEmHeight(rtb.SelectionFont.Style)) > bmp.Height || scroll == true)
                                {


                                    bmpList.Add(bmp);

                                         //bmp.Save(String.Format("{0}.bmp", line.ToString()), System.Drawing.Imaging.ImageFormat.Bmp);

                                    y = playbillItem.Offset.Y;//0;

                                    gr.Dispose();
                                    bmp = new Bitmap((int)window.Width,
                                                (int)window.Height);

                                    gr = Graphics.FromImage(bmp);
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {


                                bmpList.Add(bmp);

                                // bmp.Save(String.Format("{0}.bmp", line.ToString()), System.Drawing.Imaging.ImageFormat.Bmp);
                                gr.Dispose();
                                bmp = new Bitmap((int)window.Width,
                                                (int)window.Height);

                                gr = Graphics.FromImage(bmp);
                            }
                        }
                            #endregion
                        #endregion
                    }
                }
                else
                {
                    #region Algorithm for scrolling effects

                    #region Calculating of width of the main bmp, that'll contain all text

                    float bmpWidth = 0;

                    rtb.Select(0, 1);

                    fontToDraw = null;
                    textToDraw = rtb.SelectedText;
                    colorToFill = rtb.SelectionBackColor;
                    colorToDraw = rtb.SelectionColor;
                    fontToDraw = rtb.SelectionFont;

                    for (int i = 1; i <= rtb.TextLength;)
                    {
                        rtb.Select(i, 1);

                        if (string.IsNullOrEmpty(textToDraw))
                        {
                            textToDraw = rtb.SelectedText;
                            fontToDraw = rtb.SelectionFont;
                            colorToFill = rtb.SelectionBackColor;
                            colorToDraw = rtb.SelectionColor;
                        }

                        if (fontToDraw.Name.Equals(rtb.SelectionFont.Name)
                            && fontToDraw.Style.Equals(rtb.SelectionFont.Style)
                            && fontToDraw.Size.Equals(rtb.SelectionFont.Size)
                            && colorToFill.Equals(rtb.SelectionBackColor)
                            && colorToDraw.Equals(rtb.SelectionColor)
                            && i + 1 <= rtb.Text.Length)
                        {
                            if (rtb.SelectedText != "\n")
                                textToDraw += rtb.SelectedText;
                            else
                                textToDraw += " ";

                            ++i;
                            continue;
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(textToDraw))
                                ++i;

                            bmpWidth += gr.MeasureString(textToDraw, fontToDraw).Width;
                            textToDraw = null;
                        }
                    }

                    maxSize = 0;
                    for (int i = 0; i <= rtb.TextLength; ++i)
                    {
                        rtb.Select(i, 1);

                        if (maxSize < gr.MeasureString(rtb.SelectedText, rtb.SelectionFont).Height)
                            maxSize = gr.MeasureString(rtb.SelectedText, rtb.SelectionFont).Height;
                   }

                    System.Math.Ceiling(bmpWidth);

                    if (bmpWidth == 0)
                        bmpWidth = 2;

                    bmp = new Bitmap((int)bmpWidth, (int)window.Height);
                    gr = Graphics.FromImage(bmp);

                    #endregion

                    #region Drawing of text in main bmp

                    fontToDraw = null;
                    textToDraw = null;
                    colorToDraw = Color.Black;
                    colorToFill = Color.Black;

                    x = 0;
                    y = (bmp.Height - maxSize) / 2;

                    charpos = 0;

                    while (!(charpos > rtb.TextLength))
                    {
                        rtb.Select(charpos, 1);

                        if (string.IsNullOrEmpty(textToDraw))
                        {
                            textToDraw = (rtb.SelectedText == "\n") ? " " : rtb.SelectedText;
                            fontToDraw = rtb.SelectionFont;
                            colorToFill = rtb.SelectionBackColor;
                            colorToDraw = rtb.SelectionColor;
                        }

                        rtb.Select(++charpos, 1);

                        if (fontToDraw.Name.Equals(rtb.SelectionFont.Name) && fontToDraw.Style.Equals(rtb.SelectionFont.Style) && fontToDraw.Size.Equals(rtb.SelectionFont.Size)
                                && colorToFill.Equals(rtb.SelectionBackColor) && colorToDraw.Equals(rtb.SelectionColor) && charpos + 1 <= rtb.Text.Length)
                        {
                            if (rtb.SelectedText != "\n")
                                textToDraw += rtb.SelectedText;
                            else
                                textToDraw += " ";
                            continue;
                        }
                        else
                        {
                            int firstCharoffset = MeasureFirstCharacterOffset(gr, textToDraw, fontToDraw);
                            int lastCharoffset = MeasureLastCharacterOffset(gr, textToDraw, fontToDraw);

                            if (gr.MeasureString(textToDraw, fontToDraw).Height < maxSize)
                                y = maxSize - gr.MeasureString(textToDraw, fontToDraw).Height;
                            else
                                y = 0;

                            if (textToDraw.EndsWith(" "))
                                kerning = Convert.ToInt32(gr.MeasureString(textToDraw, fontToDraw).Width) + Convert.ToInt32(gr.MeasureString(" ", fontToDraw).Width);
                            else
                                kerning = Convert.ToInt32(MeasureDisplayStringWidth(gr, textToDraw, fontToDraw)) - lastCharoffset;

                            if (x == 0.0 || textToDraw.EndsWith(" "))
                            {

                                if (line == 0)
                                    y -= playbillItem.Offset.Y;
                                gr.FillRectangle(new SolidBrush(colorToFill), new RectangleF(x, 0, Convert.ToInt32(bmp.Width), Convert.ToInt32(bmp.Height)));
                                if (line == 0)
                                    y += playbillItem.Offset.Y;

                                //gr.FillRectangle(new SolidBrush(colorToFill), new RectangleF(x, (0), (kerning), Convert.ToInt32(bmp.Height)));

                                y += playbillItem.Offset.Y;
                                gr.DrawString(textToDraw, fontToDraw, new SolidBrush(colorToDraw), 
                                   new RectangleF(x, Convert.ToInt32(y), (gr.MeasureString(textToDraw, fontToDraw).Width), Convert.ToInt32(maxSize)));
                            }
                            else
                            {
                                gr.FillRectangle(new SolidBrush(colorToFill), new RectangleF(x, 0, (gr.MeasureString(textToDraw, fontToDraw).Width), Convert.ToInt32(bmp.Height)));
                                
                                y += playbillItem.Offset.Y;
                                gr.DrawString(textToDraw, fontToDraw, new SolidBrush(colorToDraw), 
                                    new RectangleF(x - firstCharoffset / (float)1.5, Convert.ToInt32(y), (gr.MeasureString(textToDraw, fontToDraw).Width), Convert.ToInt32(maxSize)));
                            }

                            x += kerning;
                            textToDraw = null;
                            y = playbillItem.Offset.Y;// y = 0;
                        }

                    }
                    #endregion

                    #region cutting main bmp to pieces


                    x = 0;
                    y = 0;
                    int width = (int)window.Width;
                    while (x < bmp.Width)
                    {
                        if (x + window.Width > bmp.Width)
                            width = bmp.Width - x;

                        Bitmap tmp = bmp.Clone(new Rectangle((int)x, 0, width, (int)bmp.Height), bmp.PixelFormat);
                        bmpList.Add(tmp);
                        x += width;
                    }

                    #endregion

                    #endregion


                    // Instructions to summarise conversion. Add new inputBitmaps list, and so on.
                    window.ItemsList[itemNumber].BitmapsList = bmpList;
                    previewWindow.SelectedWindow.Animations[itemNumber].InputBitmaps = bmpList;

                    previewWindow.SelectedWindow.Animate(false, itemNumber);

                    foreach (Bitmap b in bmpList)
                        b.Dispose();
                }


                if (!oldbitmaps && !scroll)
                {
                    // Instructions to summarise conversion. Add new inputBitmaps list, and so on.
                    window.ItemsList[itemNumber].BitmapsList = bmpList;
                    previewWindow.SelectedWindow.Animations[itemNumber].InputBitmaps = bmpList;

                    previewWindow.SelectedWindow.Animate(false, itemNumber);

                    foreach (Bitmap b in bmpList)
                        b.Dispose();
                }

            }
            delete = false;

        }

        private void ShowRTF(object sender, EventArgs e)
        {
            MessageBox.Show(bitmaptextRichTextBox.Rtf.ToString());

        }

        private RichTextBox copyRTB(RichTextBox baseRTB)
        {
            RichTextBox rtb = new RichTextBox();

            rtb.Width = baseRTB.Width;
            rtb.Height = baseRTB.Height;
            rtb.BackColor = baseRTB.BackColor;
            rtb.Font = baseRTB.Font;
            rtb.Rtf = baseRTB.Rtf;
            //rtb.ContentsResized += new System.Windows.Forms.ContentsResizedEventHandler(this.bitmaptextRichTextBox_ContentsResized);

            int i = 0;
            while (rtb.GetFirstCharIndexFromLine(i) != -1)
            {
                if (rtb.GetFirstCharIndexFromLine(i + 1) != -1)
                {
                    rtb.Select(rtb.GetFirstCharIndexFromLine(i),
                               rtb.GetFirstCharIndexFromLine(i + 1) - rtb.GetFirstCharIndexFromLine(i));
                    baseRTB.Select(baseRTB.GetFirstCharIndexFromLine(i),
                                                 baseRTB.GetFirstCharIndexFromLine(i + 1) - baseRTB.GetFirstCharIndexFromLine(i));

                }
                else
                {
                    rtb.Select(rtb.GetFirstCharIndexFromLine(i), rtb.Text.Length);
                    baseRTB.Select(baseRTB.GetFirstCharIndexFromLine(i),
                                                 baseRTB.Text.Length);
                }

                rtb.SelectionAlignment = baseRTB.SelectionAlignment;
                ++i;
            }
            baseRTB.DeselectAll();
            rtb.DeselectAll();

            return rtb;
        }

        private RichTextBox generateRTBfromRTF(string rtf)
        {
            RichTextBox rtb = new RichTextBox();

            rtb.BackColor = Color.Black;
            rtb.Rtf = rtf;

            return rtb;
        }

        private RichTextBox generateRTBfromItem(PlayWindowItem item)
        {
            RichTextBox rtb = new RichTextBox();

            rtb.BackColor = Color.Black;
            rtb.ForeColor = Color.FromName(item.FontColor);
            rtb.Font = new System.Drawing.Font(item.FontName, item.FontSize);
            rtb.Text = item.Text;
            rtb.SelectAll();
            rtb.SelectionBackColor = Color.FromName(item.BgColor);
            
            return rtb;
        }

        private void windowWidthTextBox_Leave(object sender, EventArgs e)
        {
            if (changingTab)
                return;

            if (string.IsNullOrEmpty(((TextBox)sender).Text))
                ((TextBox)sender).Text = "0";

            int tmpValue = int.Parse(((TextBox)sender).Text);
            TreeNode selectedNode = playbillTree.SelectedNode;

            int maxW = CurrentProgram.Width;

            if (tmpValue + int.Parse(windowXTextBox.Text) > maxW && tmpValue >= maxW)
                ((TextBox)sender).Text = (maxW - int.Parse(windowXTextBox.Text)).ToString();
            else if (tmpValue + int.Parse(windowXTextBox.Text) > maxW)
                ((TextBox)sender).Text = maxW.ToString();

            else if (tmpValue % 8 != 0 || tmpValue == 0)
                ((TextBox)sender).Text = (tmpValue + (8 - tmpValue % 8)).ToString();


            if (selectedNode != null)
            {
                int programNumber = selectedNode.Parent.Index;
                uint windowWidth = uint.Parse(((System.Windows.Forms.TextBox)(sender)).Text);

                CurrentWindow.Width = windowWidth;

                previewWindow.SelectedWindow.windowRect.Width = Convert.ToInt32(windowWidth) * (PreviewWindow.LedDiam + PreviewWindow.Space);
                //previewWindow.SelectedWindow.DrawStatic(true, null);


                if (windowsizechanged == true)
                {
                    foreach (TreeNode t in selectedNode.Nodes)
                    {
                        if (t != selectedNode.LastNode)
                        {
                            RegenerateItemBitmaps(t);
                        }
                    }
                    //playbillTree.SelectedNode = selectedNode;
                    windowsizechanged = false;

                    wndchangingItemNumber = previewWindow.SelectedWindow.CurrentAnimationNumber = 0;
                    previewWindow.DrawStaticWindows();
                }
            }
        }

        public void RegenerateItemBitmaps(TreeNode t)
        {
            int currentProgram = previewWindow.ActiveProgram;
            int currentWindow = previewWindow.ActiveWindow;
            int currentAnimation = previewWindow.SelectedWindow.CurrentAnimationNumber;

            previewWindow.ActiveProgram = t.Parent.Parent.Index;
            previewWindow.ActiveWindow = t.Parent.Index;
            previewWindow.SelectedWindow.CurrentAnimationNumber = t.Index;

            if (t.Text == "BitmapText")
                RtbToBitmap(t);
            else if (t.Text == "Bitmap")
            {
                PlayWindowItem item = previewWindow.SelectedWindow.Animations[t.Index].PlaybillItem;
                if (!string.IsNullOrEmpty(item.Text))
                {
                    previewWindow.SelectedWindow.Animations[previewWindow.SelectedWindow.CurrentAnimationNumber].InputBitmaps
                        = new List<Bitmap>() { new Bitmap(item.Text) };
                }

                item.BitmapsList = previewWindow.SelectedWindow.Animations[previewWindow.SelectedWindow.CurrentAnimationNumber].InputBitmaps;
            }
            else if (t.Text == "Animator")
            {

                PlayWindowItem item = previewWindow.SelectedWindow.Animations[t.Index].PlaybillItem;

                List<Bitmap> tmpbmpl = new List<Bitmap>();

                using ( Image gifImg = Image.FromFile(item.Text))
                {
                    FrameDimension dimension = new FrameDimension(gifImg.FrameDimensionsList[0]);

                    for (int i = 0; i < gifImg.GetFrameCount(dimension); i++)
                    {
                        gifImg.SelectActiveFrame(dimension, i);
                        tmpbmpl.Add(new Bitmap(gifImg));
                    }

                    PropertyItem pitem = gifImg.GetPropertyItem(0x5100); // FrameDelay in libgdiplus
                    // Time is in 1/100th of a second
                    int delay = (pitem.Value[0] + pitem.Value[1] * 256) * 10;
                    item.FrameDelay = delay / 10;


                    previewWindow.SelectedWindow.CurrentAnimation.Speed = -1;
                    previewWindow.SelectedWindow.CurrentAnimation.StayTime = short.Parse(delay.ToString());


                    item.BitmapsList = tmpbmpl;
                    previewWindow.SelectedWindow.CurrentAnimation.InputBitmaps
                            = tmpbmpl;
                }
            }
            else if (t.Text == "Clock")
            {
                PlayWindowItem item = previewWindow.SelectedWindow.Animations[t.Index].PlaybillItem;
                GenerateClockBitmap(item);
            }
            else if (t.Text == "Temperature")
            {
                PlayWindowItem item = previewWindow.SelectedWindow.Animations[t.Index].PlaybillItem;
                GenerateTemperatureBitmap(item);
            }

            previewWindow.ActiveProgram = currentProgram;
            previewWindow.ActiveWindow = currentWindow;
            previewWindow.SelectedWindow.CurrentAnimationNumber = currentAnimation;
        }


        private void windowXTextBox_Leave(object sender, EventArgs e)
        {
            if (changingTab)
                return;

            if (string.IsNullOrEmpty(((TextBox)sender).Text))
                ((TextBox)sender).Text = "0";

            int tmpValue = int.Parse(((TextBox)sender).Text);
            TreeNode selectedNode = (playbillTree.SelectedNode.Tag.ToString() == "window") ? playbillTree.SelectedNode : playbillTree.SelectedNode.Parent;

            int oldWidth = CurrentProgram.Width;
            int tmpWidth = tmpValue + int.Parse(windowWidthTextBox.Text);
            bool changed = false;

            if (tmpValue < oldWidth && tmpWidth > oldWidth)
            {
                if (tmpValue % 8 != 0)
                    tmpValue = (tmpValue + (8 - tmpValue % 8));

                windowWidthTextBox.Text = (oldWidth - tmpValue).ToString();
                ((TextBox)sender).Text = tmpValue.ToString();
                changed = true;
            }

            if (tmpValue == oldWidth)
            {
                tmpValue -= 8;

                windowWidthTextBox.Text = (oldWidth - tmpValue).ToString();
                windowXTextBox.Text = tmpValue.ToString();
                changed = true;
            }
            if (tmpValue > oldWidth)
            {
                tmpValue = oldWidth - 8;
                windowWidthTextBox.Text = (oldWidth - tmpValue).ToString();
                windowXTextBox.Text = tmpValue.ToString();
                changed = true;
            }

            if (tmpValue % 8 != 0)
                ((TextBox)sender).Text = (tmpValue + (8 - tmpValue % 8)).ToString();



            if (selectedNode != null)
            {
                int programNumber = selectedNode.Parent.Index;
                uint windowX = uint.Parse(((System.Windows.Forms.TextBox)(sender)).Text);


                if (windowX != CurrentWindow.X)
                {
                    CurrentWindow.X = windowX;


                    previewWindow.SelectedWindow.windowRect.X = Convert.ToInt32(windowX) * (PreviewWindow.LedDiam + PreviewWindow.Space);
                    previewWindow.SelectedWindow.DrawStatic(true, null);

                    if (changed == true)
                    {
                        testPlaybill.ProgramsList[programNumber].WindowsList[selectedNode.Index].Width =
                        ushort.Parse(windowWidthTextBox.Text);

                        previewWindow.SelectedWindow.windowRect.Width = Convert.ToInt32(windowWidthTextBox.Text) * (PreviewWindow.LedDiam + PreviewWindow.Space);
                        previewWindow.SelectedWindow.DrawStatic(true, null);
                    }
                }

                if (windowsizechanged == true)
                {
                    foreach (TreeNode t in selectedNode.Nodes)
                    {
                        if (t != selectedNode.LastNode)
                        {
                            RegenerateItemBitmaps(t);
                            previewWindow.SelectedWindow.DrawStatic(true, null);
                        }
                    }
                    //playbillTree.SelectedNode = selectedNode;
                    windowsizechanged = false;

                    wndchangingItemNumber = previewWindow.SelectedWindow.CurrentAnimationNumber = 0;
                    previewWindow.DrawStaticWindows();
                }

            }
        }

        private void windowYTextBox_Leave(object sender, EventArgs e)
        {
            if (changingTab)
                return;

            if (string.IsNullOrEmpty(((TextBox)sender).Text))
                ((TextBox)sender).Text = "0";

            int tmpValue = int.Parse(((TextBox)sender).Text);
            TreeNode selectedNode = playbillTree.SelectedNode;
            int tmpHeight = tmpValue + int.Parse(windowHeightTextBox.Text);
            int oldHeight = CurrentPlaybill.ProgramsList[(selectedNode != null) ? selectedNode.Parent.Index : 0].Height;
            bool changed = false;

            if (tmpValue < oldHeight && tmpHeight > oldHeight)
            {
                int newHeight = (CurrentProgram.Height) - tmpValue;
                windowHeightTextBox.Text = newHeight.ToString();
                changed = true;
            }
            else if (tmpValue == CurrentProgram.Height)
            {
                --tmpValue;
                windowHeightTextBox.Text = (oldHeight - tmpValue).ToString();
                windowYTextBox.Text = tmpValue.ToString();
                changed = true;
            }

            if (tmpValue > oldHeight)
            {
                tmpValue = oldHeight - 1;
                windowHeightTextBox.Text = (oldHeight - tmpValue).ToString();
                windowYTextBox.Text = tmpValue.ToString();
                changed = true;
            }
            //if (tmpValue % 8 != 0)
            //    ((TextBox)sender).Text = (tmpValue + (8 - tmpValue % 8)).ToString();


            if (selectedNode != null)
            {
                int programNumber = selectedNode.Parent.Index;
                uint windowY = uint.Parse(((System.Windows.Forms.TextBox)(sender)).Text);

                CurrentWindow.Y = windowY;


                previewWindow.SelectedWindow.windowRect.Y = Convert.ToInt32(windowY) * (PreviewWindow.LedDiam + PreviewWindow.Space);
                previewWindow.SelectedWindow.DrawStatic(true, null);

                if (changed == true)
                {
                    testPlaybill.ProgramsList[programNumber].WindowsList[selectedNode.Index].Height =
                        ushort.Parse(windowHeightTextBox.Text);

                    previewWindow.SelectedWindow.windowRect.Height = Convert.ToInt32(windowHeightTextBox.Text) * (PreviewWindow.LedDiam + PreviewWindow.Space);
                    previewWindow.SelectedWindow.DrawStatic(true, null);
                }

                if (windowsizechanged == true)
                {
                    foreach (TreeNode t in selectedNode.Nodes)
                    {
                        if (t != selectedNode.LastNode)
                        {
                            RegenerateItemBitmaps(t);
                            previewWindow.SelectedWindow.DrawStatic(true, null);
                        }
                    }
                    //playbillTree.SelectedNode = selectedNode;
                    windowsizechanged = false;

                    wndchangingItemNumber = previewWindow.SelectedWindow.CurrentAnimationNumber = 0;
                    previewWindow.DrawStaticWindows();
                }
            }
        }

        private void windowHeightTextBox_Leave(object sender, EventArgs e)
        {
            if (changingTab)
                return;

            if (string.IsNullOrEmpty(((TextBox)sender).Text))
                ((TextBox)sender).Text = "0";

            int tmpValue = int.Parse(((TextBox)sender).Text);
            TreeNode selectedNode = playbillTree.SelectedNode;
            int tmpHeight = tmpValue + int.Parse(windowYTextBox.Text);
            int maxHeight = CurrentPlaybill.ProgramsList[(selectedNode != null) ? selectedNode.Parent.Index : 0].Height;

            if (tmpValue == 0)
                ((TextBox)sender).Text = (tmpValue + (8 - tmpValue % 8)).ToString();
            else if (tmpHeight > maxHeight && tmpValue >= maxHeight)
                ((TextBox)sender).Text = (maxHeight - int.Parse(windowYTextBox.Text)).ToString();
            else if (tmpHeight > maxHeight)
                ((TextBox)sender).Text = CurrentProgram.Height.ToString();

            if (selectedNode != null)
            {
                int programNumber = selectedNode.Parent.Index;
                uint windowHeight = uint.Parse(((System.Windows.Forms.TextBox)(sender)).Text);

                CurrentWindow.Height = windowHeight;

                previewWindow.SelectedWindow.windowRect.Height = Convert.ToInt32(windowHeight) * (PreviewWindow.LedDiam + PreviewWindow.Space);
                previewWindow.SelectedWindow.DrawStatic(true, null);


                if (windowsizechanged == true)
                {
                    foreach (TreeNode t in selectedNode.Nodes)
                    {
                        if (t != selectedNode.LastNode)
                        {
                            RegenerateItemBitmaps(t);
                            previewWindow.SelectedWindow.DrawStatic(true, null);
                        }
                    }
                    //playbillTree.SelectedNode = selectedNode;
                    windowsizechanged = false;

                    wndchangingItemNumber = previewWindow.SelectedWindow.CurrentAnimationNumber = 0;
                    previewWindow.DrawStaticWindows();
                }
            }
        }

        private void programWidthTextBox_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(((TextBox)sender).Text))
                ((TextBox)sender).Text = "0";

            int tmpValue = int.Parse(((TextBox)sender).Text);
            if (tmpValue % 8 != 0)
                ((TextBox)sender).Text = (tmpValue + (8 - tmpValue % 8)).ToString();

            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                ushort programWidth = ushort.Parse(((System.Windows.Forms.TextBox)(sender)).Text);

                CurrentProgram.Width = programWidth;
            }
        }

        private void programHeightTextBox_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(((TextBox)sender).Text))
                ((TextBox)sender).Text = "0";

            int tmpValue = int.Parse(((TextBox)sender).Text);
            if (tmpValue % 8 != 0)
                ((TextBox)sender).Text = (tmpValue + (8 - tmpValue % 8)).ToString();

            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                ushort programHeight = ushort.Parse(((System.Windows.Forms.TextBox)(sender)).Text);

                CurrentProgram.Height = programHeight;
            }
        }

        private void windowDivideHorizontalButton_Click(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int programNumber = selectedNode.Parent.Index;

                if (CurrentPlaybill.ProgramsList[programNumber].WindowsList.Count >= 10)
                {
                    MessageBox.Show(messageBoxesHashTable["messageBoxMessage_only10Windows"].ToString(), "LedConfig",
                                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    return;
                }

                uint windowHeight = CurrentWindow.Height;
                uint originalHeight = windowHeight;

                if (windowHeight > 8)
                {
                    //if ((windowHeight / 2) % 8 == 0)
                        windowHeight = windowHeight / 2;
                    //else
                    //    windowHeight -= 8;

                    CurrentWindow.Height = windowHeight;
                    windowHeightTextBox.Text = CurrentWindow.Height.ToString();
                    windowWidthTextBox.Text = CurrentWindow.Width.ToString();
                    windowXTextBox.Text = CurrentWindow.X.ToString();
                    windowYTextBox.Text = CurrentWindow.Y.ToString();

                    previewWindow.SelectedWindow.windowRect = new Rectangle(previewWindow.SelectedWindow.X, previewWindow.SelectedWindow.Y,
                        previewWindow.SelectedWindow.Width, (int)windowHeight * (PreviewWindow.LedDiam + PreviewWindow.Space));
                    previewWindow.SelectedWindow.Redraw();

                    PlayWindow oldWindow = CurrentWindow;

                    playbillTree.SelectedNode = selectedNode.Parent.LastNode;
                    playbillTree.SelectedNode = playbillTree.SelectedNode.Parent; // after creating new window new item is also created and selected node is item, not window and we need it's parent window
                    TreeNode tmpwWindowNode = playbillTree.SelectedNode;
                        


                    TreeNode newWindowNode = (TreeNode)playbillTree.SelectedNode;//.Clone();
                    TreeNode parent = selectedNode.Parent;
                    PlayWindow newWindow = CurrentWindow;

                    //if (newWindowNode.Index > selectedNode.Index + 1)
                    //{
                    //    testPlaybill.ProgramsList[programNumber].WindowsList[playbillTree.SelectedNode.Index].Height = windowHeight;
                    //    testPlaybill.ProgramsList[programNumber].WindowsList[playbillTree.SelectedNode.Index].Y =
                    //        testPlaybill.ProgramsList[programNumber].WindowsList[selectedNode.Index].Y + windowHeight;
                    //    testPlaybill.ProgramsList[programNumber].WindowsList[playbillTree.SelectedNode.Index].X =
                    //        testPlaybill.ProgramsList[programNumber].WindowsList[selectedNode.Index].X;
                    //    testPlaybill.ProgramsList[programNumber].WindowsList[playbillTree.SelectedNode.Index].Width =
                    //        testPlaybill.ProgramsList[programNumber].WindowsList[selectedNode.Index].Width;

                    //    playbillTree.SelectedNode = selectedNode;

                    //    testPlaybill.ProgramsList[programNumber].WindowsList.RemoveAt(tmpwWindowNode.Index);
                    //    PreviewWindow.programsWithWindows[programNumber].RemoveAt(tmpwWindowNode.Index);
                    //    tmpwWindowNode.Remove();

                    //    parent.Nodes.Insert(selectedNode.Index + 1, newWindowNode);
                    //    testPlaybill.ProgramsList[programNumber].WindowsList.Insert(selectedNode.Index + 1, newWindow);


                    //    PreviewWindow.programsWithWindows[programNumber].Insert(selectedNode.Index + 1,
                    //        new WindowOnPreview(previewWindow, Convert.ToInt32(newWindow.X), Convert.ToInt32(newWindow.Y),
                    //            Convert.ToInt32(newWindow.Width), Convert.ToInt32(newWindow.Height),
                    //            PreviewWindow.LedDiam + PreviewWindow.Space));

                    //}
                    //else
                    {
                        CurrentWindow.Height = originalHeight - windowHeight;
                        CurrentWindow.Y = oldWindow.Y + windowHeight;
                        CurrentWindow.X = oldWindow.X;
                        CurrentWindow.Width = oldWindow.Width;


                        windowHeightTextBox.Text = CurrentWindow.Height.ToString();
                        windowWidthTextBox.Text = CurrentWindow.Width.ToString();
                        windowXTextBox.Text = CurrentWindow.X.ToString();
                        windowYTextBox.Text = CurrentWindow.Y.ToString();

                        previewWindow.SelectedWindow.windowRect = new Rectangle(
                            (int)CurrentWindow.X * (PreviewWindow.LedDiam + PreviewWindow.Space),
                            (int)CurrentWindow.Y * (PreviewWindow.LedDiam + PreviewWindow.Space),
                            (int)CurrentWindow.Width * (PreviewWindow.LedDiam + PreviewWindow.Space),
                            (int)CurrentWindow.Height * (PreviewWindow.LedDiam + PreviewWindow.Space));
                        previewWindow.SelectedWindow.Redraw();

                        //playbillTree.SelectedNode = selectedNode;
                    }
                    previewWindow.DrawStaticWindows();
                }

                foreach (TreeNode t in selectedNode.Nodes)
                {
                    if(t.Text != "*new item*")
                        RegenerateItemBitmaps(t);
                    //if (t != selectedNode.LastNode)
                        //playbillTree.SelectedNode = t;
                    //OpenItemTab(item, true);
                }
                //playbillTree.SelectedNode = selectedNode;
            }
        }

        private void windowDivideVerticalButton_Click(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int programNumber = selectedNode.Parent.Index;

                if (CurrentPlaybill.ProgramsList[programNumber].WindowsList.Count >= 10)
                {
                    MessageBox.Show(messageBoxesHashTable["messageBoxMessage_only10Windows"].ToString(), "LedConfig",
                                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    return;
                }

                uint windowWidth = testPlaybill.ProgramsList[programNumber].WindowsList[selectedNode.Index].Width;
                uint originalWidth = windowWidth;

                if (windowWidth > 8)
                {
                    if ((windowWidth / 2) % 8 == 0)
                        windowWidth = windowWidth / 2;
                    else
                        windowWidth = (windowWidth-8) / 2;

                    CurrentWindow.Width = windowWidth;

                    windowHeightTextBox.Text = CurrentWindow.Height.ToString();
                    windowWidthTextBox.Text = CurrentWindow.Width.ToString();
                    windowXTextBox.Text = CurrentWindow.X.ToString();
                    windowYTextBox.Text = CurrentWindow.Y.ToString();

                    previewWindow.SelectedWindow.windowRect = new Rectangle(previewWindow.SelectedWindow.X, previewWindow.SelectedWindow.Y,
                        (int)windowWidth * (PreviewWindow.LedDiam + PreviewWindow.Space), (int)previewWindow.SelectedWindow.Height);
                    previewWindow.SelectedWindow.Redraw();

                    PlayWindow oldWindow = CurrentWindow;

                    playbillTree.SelectedNode = selectedNode.Parent.LastNode;
                    playbillTree.SelectedNode = playbillTree.SelectedNode.Parent; // after creating new window new item is also created and selected node is item, not window and we need it's parent window
                    TreeNode tmpwWindowNode = playbillTree.SelectedNode;



                    TreeNode newWindowNode = (TreeNode)playbillTree.SelectedNode;//.Clone();
                    TreeNode parent = selectedNode.Parent;
                    PlayWindow newWindow = CurrentWindow;

                    //if (newWindowNode.Index > selectedNode.Index + 1)
                    //{
                    //    testPlaybill.ProgramsList[programNumber].WindowsList[playbillTree.SelectedNode.Index].Width = windowWidth;
                    //    testPlaybill.ProgramsList[programNumber].WindowsList[playbillTree.SelectedNode.Index].X =
                    //        testPlaybill.ProgramsList[programNumber].WindowsList[selectedNode.Index].X + windowWidth;
                    //    testPlaybill.ProgramsList[programNumber].WindowsList[playbillTree.SelectedNode.Index].Y =
                    //        testPlaybill.ProgramsList[programNumber].WindowsList[selectedNode.Index].Y;
                    //    testPlaybill.ProgramsList[programNumber].WindowsList[playbillTree.SelectedNode.Index].Height =
                    //        testPlaybill.ProgramsList[programNumber].WindowsList[selectedNode.Index].Height;

                    //    playbillTree.SelectedNode = selectedNode;

                    //    testPlaybill.ProgramsList[programNumber].WindowsList.RemoveAt(tmpwWindowNode.Index);
                    //    PreviewWindow.programsWithWindows[programNumber].RemoveAt(tmpwWindowNode.Index);
                    //    tmpwWindowNode.Remove();

                    //    parent.Nodes.Insert(selectedNode.Index + 1, newWindowNode);
                    //    testPlaybill.ProgramsList[programNumber].WindowsList.Insert(selectedNode.Index + 1, newWindow);


                    //    PreviewWindow.programsWithWindows[programNumber].Insert(selectedNode.Index + 1,
                    //        new WindowOnPreview(previewWindow, Convert.ToInt32(newWindow.X), Convert.ToInt32(newWindow.Y),
                    //            Convert.ToInt32(newWindow.Width), Convert.ToInt32(newWindow.Height),
                    //            PreviewWindow.LedDiam + PreviewWindow.Space));
                    //}
                    //else
                    {
                        CurrentWindow.Height = oldWindow.Height;
                        CurrentWindow.Y = oldWindow.Y;
                        CurrentWindow.X = oldWindow.X + windowWidth;
                        CurrentWindow.Width = originalWidth - windowWidth;

                        windowHeightTextBox.Text = CurrentWindow.Height.ToString();
                        windowWidthTextBox.Text = CurrentWindow.Width.ToString();
                        windowXTextBox.Text = CurrentWindow.X.ToString();
                        windowYTextBox.Text = CurrentWindow.Y.ToString();

                        previewWindow.SelectedWindow.windowRect = new Rectangle(
                            (int)CurrentWindow.X * (PreviewWindow.LedDiam + PreviewWindow.Space),
                            (int)CurrentWindow.Y * (PreviewWindow.LedDiam + PreviewWindow.Space),
                            (int)CurrentWindow.Width * (PreviewWindow.LedDiam + PreviewWindow.Space),
                            (int)CurrentWindow.Height * (PreviewWindow.LedDiam + PreviewWindow.Space));
                        previewWindow.SelectedWindow.Redraw();

                        //playbillTree.SelectedNode = selectedNode;
                    }

                    //playbillTree.SelectedNode = selectedNode;
                    previewWindow.DrawStaticWindows();
                }


                foreach (TreeNode t in selectedNode.Nodes)
                {
                    if (t.Text != "*new item*")
                        RegenerateItemBitmaps(t);
                    //if (t != selectedNode.LastNode)
                    //playbillTree.SelectedNode = t;
                    //OpenItemTab(item, true);
                }
                //playbillTree.SelectedNode = selectedNode;
            }
        }

        private void playbillWidthTextBox_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(((TextBox)sender).Text))
                ((TextBox)sender).Text = "8";

            int tmpValue = int.Parse(((TextBox)sender).Text);
            if (tmpValue % 8 != 0 || tmpValue == 0)
                ((TextBox)sender).Text = (tmpValue + (8 - tmpValue % 8)).ToString();

            //TreeNode selectedNode = playbillTree.SelectedNode;
            //if (selectedNode != null)
            {
                ushort playbillWidth = ushort.Parse(((System.Windows.Forms.TextBox)(sender)).Text);

                testPlaybill.Width = playbillWidth;

                previewWindow.ChangeSize(CurrentPlaybill.Width, CurrentPlaybill.Height);
                //.ProgramsList[selectedNode.Index].Width =
                //  programWidth;
            }
        }

        private void playbillHeightTextBox_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(((TextBox)sender).Text))
                ((TextBox)sender).Text = "8";

            int tmpValue = int.Parse(((TextBox)sender).Text);
            if (tmpValue % 8 != 0 || tmpValue == 0)
                ((TextBox)sender).Text = (tmpValue + (8 - tmpValue % 8)).ToString();

            //TreeNode selectedNode = playbillTree.SelectedNode;
            //if (selectedNode != null)
            {
                ushort playbillHeight = ushort.Parse(((System.Windows.Forms.TextBox)(sender)).Text);

                testPlaybill.Height = playbillHeight;

                previewWindow.ChangeSize(CurrentPlaybill.Width, CurrentPlaybill.Height);
                //.ProgramsList[selectedNode.Index].Width =
                //  programWidth;
            }
        }

        private void playbillColorComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

            switch (playbillColorComboBox.SelectedIndex)
            {

                case 0: // Red
                    {
                        testPlaybill.Color = 1;
                        break;
                    }
                case 1: // Green (Lime)
                    {
                        testPlaybill.Color = 2;
                        break;
                    }
                case 2: // Blue
                    {
                        testPlaybill.Color = 4;
                        break;
                    }
                case 3: // RG
                    {
                        testPlaybill.Color = 3;
                        break;
                    }
                case 4: // RB
                    {
                        testPlaybill.Color = 5;
                        break;
                    }
                case 5: // GB
                    {
                        testPlaybill.Color = 6;
                        break;
                    }
                case 6: // RGB
                    {
                        testPlaybill.Color = 7;
                        break;
                    }
            }
            playbillColorInitialization();

            TreeNode selectedNode = playbillTree.SelectedNode;

            if (previewWindow.ActiveProgram >= PreviewWindow.programsWithWindows.Count)
                previewWindow.ActiveProgram = PreviewWindow.programsWithWindows.Count - 1;
            if (PreviewWindow.programsWithWindows.Count > 0 && PreviewWindow.programsWithWindows[previewWindow.ActiveProgram].Count > 0)
            {
                foreach (WindowOnPreview item in PreviewWindow.programsWithWindows[previewWindow.ActiveProgram])
                {
                    if (item.Animations.Count > 0)
                        item.Animations[item.CurrentAnimationNumber].StopAnimation();
                }

                for (int i = 0; i < PreviewWindow.programsWithWindows.Count; i++)
                {
                    previewWindow.ActiveProgram = i;

                    for (int j = 0; j < PreviewWindow.programsWithWindows[i].Count; j++)
                    {
                        previewWindow.ActiveWindow = j;

                        PlayWindow playbillWindow = CurrentPlaybill.ProgramsList[i].WindowsList[j];
                        WindowOnPreview playWnd = PreviewWindow.programsWithWindows[i][j];

                        //previewWindow.windowRect = new Rectangle(previewWindow.X, previewWindow.Y, previewWindow.Width, previewWindow.Height);

                        playWnd.X = (int)playbillWindow.X * (PreviewWindow.LedDiam + PreviewWindow.Space);
                        playWnd.Y = (int)playbillWindow.Y * (PreviewWindow.LedDiam + PreviewWindow.Space);
                        playWnd.Width = (int)playbillWindow.Width * (PreviewWindow.LedDiam + PreviewWindow.Space);
                        playWnd.Height = (int)playbillWindow.Height * (PreviewWindow.LedDiam + PreviewWindow.Space);
                        playWnd.windowRect = new Rectangle(playWnd.X, playWnd.Y, playWnd.Width, playWnd.Height);

                        if (windowsizechanged)
                        {
                            for (int k = 0; k < playWnd.Animations.Count; k++)
                            {
                                RegenerateItemBitmaps(playbillTree.Nodes[0].Nodes[i].Nodes[j].Nodes[k]);
                                //playbillTree.SelectedNode = playbillTree.Nodes[0].Nodes[i].Nodes[j].Nodes[k];

                            }
                            windowsizechanged = false;
                        }
                    }
                }

                playbillTree.SelectedNode = selectedNode;//playbillTree.Nodes[0];

                //this.Cursor = currentCursor;
            }
        }

        private void beginnerLevelCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (UserLevel == ApplicationUserLevel.Beginner)// && ((CheckBox)sender).Checked == false)
                ((CheckBox)sender).Checked = true;
            else if (((CheckBox)sender).Checked == true)
            {
                if (!changingLevelAtBeginning)
                {
                    DialogResult result = MessageBox.Show(messageBoxesHashTable["messageBoxMessage_changeLevelBeginner"].ToString(), messageBoxesHashTable["messageBoxTitle_changeLevelBeginner"].ToString(), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (result == System.Windows.Forms.DialogResult.Yes)
                    {
                        UserLevel = ApplicationUserLevel.Beginner;
                        advancedLevelCheckBox.Checked = false;
                        expertLevelCheckBox.Checked = false;
                    }
                    else
                        ((CheckBox)sender).Checked = false;
                }
                else
                {
                    UserLevel = ApplicationUserLevel.Beginner;
                    advancedLevelCheckBox.Checked = false;
                    expertLevelCheckBox.Checked = false;
                }
            }

        }

        private void advancedLevelCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (UserLevel == ApplicationUserLevel.Advanced)// && ((CheckBox)sender).Checked == false)
                ((CheckBox)sender).Checked = true;
            else if (((CheckBox)sender).Checked == true)
            {
                UserLevel = ApplicationUserLevel.Advanced;
                beginnerLevelCheckBox.Checked = false;
                expertLevelCheckBox.Checked = false;
            }
        }

        private void expertLevelCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (UserLevel == ApplicationUserLevel.Expert)// && ((CheckBox)sender).Checked == false)
                ((CheckBox)sender).Checked = true;
            else if (((CheckBox)sender).Checked == true)
            {
                DialogResult result = MessageBox.Show(messageBoxesHashTable["messageBoxMessage_changeLevelExpert"].ToString(), messageBoxesHashTable["messageBoxTitle_changeLevelExpert"].ToString(), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    if (accessWindow.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        UserLevel = ApplicationUserLevel.Expert;
                        beginnerLevelCheckBox.Checked = false;
                        advancedLevelCheckBox.Checked = false;
                    }
                }
                else
                    ((CheckBox)sender).Checked = false;
            }
        }

        private void textEffectSpeedTrackBar_Scroll(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                if (((TrackBar)(sender)).Value >= 5)
                {

                    testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Speed =
                            ((int)((TrackBar)(sender)).Value + 90);
                }
                else
                {
                    switch (((TrackBar)(sender)).Value)
                    {
                        case 4: testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Speed = 93; break;
                        case 3: testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Speed = 90; break;
                        case 2: testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Speed = 85; break;
                        case 1: testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Speed = 78; break;
                    }
                }

                if (testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].ItemType == PlayWindowItemType.Text)
                    textEffectSpeedValueLabel.Text = ((TrackBar)(sender)).Value.ToString();
                else if (testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].ItemType == PlayWindowItemType.Bitmap)
                    bitmapEffectSpeedValueLabel.Text = ((TrackBar)(sender)).Value.ToString();
                else if (testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].ItemType == PlayWindowItemType.BitmapText)
                    bitmaptextEffectSpeedValueLabel.Text = ((TrackBar)(sender)).Value.ToString();
            }
        }

        private void tabControl1_DrawItem(object sender, System.Windows.Forms.DrawItemEventArgs e)
        {
            TabControl tabControl = ((TabControl)sender);

            // Get the bounding end of tab strip rectangles.
            Rectangle tabstripEndRect = tabControl.GetTabRect(tabControl.TabPages.Count - 1);
            RectangleF tabstripEndRectF = new RectangleF(tabstripEndRect.X + tabstripEndRect.Width, tabstripEndRect.Y - 5,
         tabControl.Width - (tabstripEndRect.X + tabstripEndRect.Width), tabstripEndRect.Height + 5);

            // First, do the end of the tab strip.
            // If we have an image use it.
            if (this.BackgroundImage != null)
            {
                RectangleF src = new RectangleF(tabstripEndRectF.X + tabControl.Left, tabstripEndRectF.Y + tabControl.Top, tabstripEndRectF.Width, tabstripEndRectF.Height);
                e.Graphics.DrawImage(this.BackgroundImage, tabstripEndRectF, src, GraphicsUnit.Pixel);
            }
            // If we have no image, use the background color.
            else
            {
                using (Brush backBrush = new SolidBrush(tabControl.Parent.BackColor))
                {
                    e.Graphics.FillRectangle(backBrush, tabstripEndRectF);
                }
            }

            // Set up the page and the various pieces.
            TabPage page = tabControl.TabPages[e.Index];
            Brush BackBrush = new SolidBrush(page.BackColor);
            Brush ForeBrush = new SolidBrush(page.ForeColor);
            string TabName = page.Text;

            // Set up the offset for an icon, the bounding rectangle and image size and then fill the background.
            int iconOffset = 0;
            Rectangle tabBackgroundRect = e.Bounds;
            e.Graphics.FillRectangle(BackBrush, tabBackgroundRect);



            // Draw out the label.
            Rectangle labelRect = new Rectangle(tabBackgroundRect.X + iconOffset, tabBackgroundRect.Y + 3,
         tabBackgroundRect.Width - iconOffset, tabBackgroundRect.Height - 3);
            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Center;
            e.Graphics.DrawString(TabName, e.Font, ForeBrush, labelRect, sf);

            //Dispose objects
            sf.Dispose();
            BackBrush.Dispose();
            ForeBrush.Dispose();

            string tabName = this.itemTypeTabControl.TabPages[e.Index].Name;

            Rectangle recTab = e.Bounds;

            recTab = new Rectangle(recTab.X, recTab.Y + 4, recTab.Width, recTab.Height - 4);

            switch (tabName)
            {
                case "textItemTab":
                    e.Graphics.DrawImage(BengiLED_for_C_Power.Properties.Resources.text, recTab);
                    break;
                case "bitmapItemTab":
                    e.Graphics.DrawImage(BengiLED_for_C_Power.Properties.Resources.picture, recTab);
                    break;
                case "bitmaptextItemTab":
                    e.Graphics.DrawImage(BengiLED_for_C_Power.Properties.Resources.text, recTab);
                    break;
                case "clockItemTab":
                    e.Graphics.DrawImage(BengiLED_for_C_Power.Properties.Resources.clock, recTab);
                    break;
                case "temperatureItemTab":
                    e.Graphics.DrawImage(BengiLED_for_C_Power.Properties.Resources.temperature, recTab);
                    break;
                case "animatorItemTab":
                    e.Graphics.DrawImage(BengiLED_for_C_Power.Properties.Resources.animation, recTab);
                    break;
            }

        }

        private void showPreviewButton_Click(object sender, EventArgs e)
        {
            if (previewWindow.Visible)
            {
                previewWindow.Hide();
                previewWindow.StopProgram(previewWindow.ActiveProgram);
            }
            else if (!previewWindow.Visible)
            {
                previewWindow.Show();
                previewWindow.BringToFront();       
            }
        }

        private void showSettingsButton_Click(object sender, EventArgs e)
        {
            if (settingsForm.WindowState == FormWindowState.Minimized)
            {
                settingsForm.WindowState = FormWindowState.Normal;
                settingsForm.BringToFront();
            }
            else if (!settingsForm.Visible)
            {
                //if ((MessageBox.Show(messageBoxesHashTable["messageBoxMessage_changeLevelExpert"].ToString(), messageBoxesHashTable["messageBoxTitle_changeLevelExpert"].ToString(), MessageBoxButtons.YesNo, MessageBoxIcon.Warning)) == System.Windows.Forms.DialogResult.Yes)
                {
                    //if (accessWindow.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        settingsForm.Show();
                        settingsForm.BringToFront();
                    }
                }
            }
        }

        private void useDeaultsForPlaybillCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (CurrentPlaybill == null)
                return;

            if (((CheckBox)sender).Checked == true)
            {
                byte tmpColor = 0;
                ushort tmpWidth = 0;
                ushort tmpHeight = 0;


                ushort.TryParse(ConfigHashtable["Width"].ToString(), out tmpWidth);
                ushort.TryParse(ConfigHashtable["Height"].ToString(), out tmpHeight);
                byte.TryParse(ConfigHashtable["Color"].ToString(), out tmpColor);

                CurrentPlaybill.Width = tmpWidth;
                CurrentPlaybill.Height = tmpHeight;
                CurrentPlaybill.Color = tmpColor;

                //playbillWidthTextBox.Text = ConfigHashtable["Width"].ToString();
                //playbillHeightTextBox.Text = ConfigHashtable["Height"].ToString();

                //playbillTree.SelectedNode = null;
                //playbillTree.SelectedNode = playbillTree.Nodes["mainNode"];

                previewWindow.ChangeSize(CurrentPlaybill.Width, CurrentPlaybill.Height);
                if (playbillTree != null && playbillTree.Nodes.Count > 0 && playbillTree.SelectedNode != null && playbillTree.SelectedNode == playbillTree.Nodes[0])
                    OpenTabForNode(playbillTree.SelectedNode);
                else
                    ChangeSelectedPlaybillColor();

            }

            playbillWidthTextBox.Enabled = !((CheckBox)sender).Checked;
            playbillHeightTextBox.Enabled = !((CheckBox)sender).Checked;
            playbillColorComboBox.Enabled = !((CheckBox)sender).Checked;
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            TreeNode selected = playbillTree.SelectedNode;

            if (selected != null)
            {
                if (selected.Name != "mainNode")
                {
                    DialogResult result = MessageBox.Show(messageBoxesHashTable["messageBoxMessage_deleteItem"].ToString(),
                    messageBoxesHashTable["messageBoxTitle_deleteItem"].ToString(), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (result == System.Windows.Forms.DialogResult.Yes)
                        RemovePlaybillNode(selected);
                }
                else
                {
                    DialogResult result = MessageBox.Show(messageBoxesHashTable["messageBoxMessage_deleteItem"].ToString(),
                    messageBoxesHashTable["messageBoxTitle_deleteItem"].ToString(), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (result == System.Windows.Forms.DialogResult.Yes)
                    {
                        previewWindow.ActiveWindow = 0;
                        previewWindow.ActiveProgram = -1;

                        for (int i = selected.Nodes.Count - 2; i >= 0; i--)
                            RemovePlaybillNode(selected.Nodes[i]);

                        playbillTree.SelectedNode = playbillTree.Nodes[0].Nodes[0];
                        previewWindow.ActiveProgram = 0;
                        playbillTree.SelectedNode = playbillTree.Nodes[0].Nodes[0].Nodes[0];
                        previewWindow.ActiveWindow = 0;
                        playbillTree.SelectedNode = playbillTree.Nodes[0].Nodes[0].Nodes[0].Nodes[0];
                        //previewWindow= 0;

                    }
                }
            }
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            TreeNode selected = playbillTree.SelectedNode;
            if (selected != null)
            {
                if ((string)(selected.Tag) != "item")
                    playbillTree.SelectedNode = selected.LastNode;
                else
                    playbillTree.SelectedNode = selected.Parent.LastNode;
            }
        }

        private void downButton_Click(object sender, EventArgs e)
        {
            if (playbillTree.SelectedNode != null)
            {
                if (playbillTree.SelectedNode.Name != "mainNode" && playbillTree.SelectedNode.Index < playbillTree.SelectedNode.Parent.Nodes.Count - 2)
                {
                    TreeNode nodeToDown = playbillTree.SelectedNode;
                    TreeNode nodeToUp = playbillTree.SelectedNode.NextNode;
                    int index = nodeToDown.Index;


                    playbillTree.SelectedNode = nodeToDown.Parent;

                    if (nodeToUp.Name != "New")
                    {
                        playbillTree.BeginUpdate();

                        playbillTree.SelectedNode.Nodes.RemoveAt(index + 1);
                        playbillTree.SelectedNode.Nodes.RemoveAt(index);
                        playbillTree.SelectedNode.Nodes.Insert(index, nodeToUp);
                        playbillTree.SelectedNode.Nodes.Insert(index + 1, nodeToDown);

                        playbillTree.Focus();
                        playbillTree.EndUpdate();

                        if (playbillTree.SelectedNode.Tag.ToString() == "playbill")
                        {
                            PlayProgram progToDown = testPlaybill.ProgramsList[index];
                            List<WindowOnPreview> programOnPreviewToDown = PreviewWindow.programsWithWindows[index];

                            testPlaybill.ProgramsList[index] = testPlaybill.ProgramsList[index + 1];
                            testPlaybill.ProgramsList[index + 1] = progToDown;

                            PreviewWindow.programsWithWindows[index] = PreviewWindow.programsWithWindows[index + 1];
                            PreviewWindow.programsWithWindows[index + 1] = programOnPreviewToDown;
                        }
                        else if (playbillTree.SelectedNode.Tag.ToString() == "program")
                        {
                            PlayWindow windowToDown = testPlaybill.ProgramsList[playbillTree.SelectedNode.Index].WindowsList[index];
                            WindowOnPreview windowOnPreviewToDown = PreviewWindow.programsWithWindows[playbillTree.SelectedNode.Index][index];

                            testPlaybill.ProgramsList[playbillTree.SelectedNode.Index].WindowsList[index] = testPlaybill.ProgramsList[playbillTree.SelectedNode.Index].WindowsList[index + 1];
                            testPlaybill.ProgramsList[playbillTree.SelectedNode.Index].WindowsList[index + 1] = windowToDown;

                            PreviewWindow.programsWithWindows[playbillTree.SelectedNode.Index][index] = PreviewWindow.programsWithWindows[playbillTree.SelectedNode.Index][index + 1];
                            PreviewWindow.programsWithWindows[playbillTree.SelectedNode.Index][index + 1] = windowOnPreviewToDown;
                        }
                        else if (playbillTree.SelectedNode.Tag.ToString() == "window")
                        {
                            PlayWindowItem itemToDown = testPlaybill.ProgramsList[playbillTree.SelectedNode.Parent.Index].WindowsList[playbillTree.SelectedNode.Index].ItemsList[index];
                            PreviewAnimation animationToDown = PreviewWindow.programsWithWindows[playbillTree.SelectedNode.Parent.Index][playbillTree.SelectedNode.Index].Animations[index];

                            testPlaybill.ProgramsList[playbillTree.SelectedNode.Parent.Index].WindowsList[playbillTree.SelectedNode.Index].ItemsList[index] = testPlaybill.ProgramsList[playbillTree.SelectedNode.Parent.Index].WindowsList[playbillTree.SelectedNode.Index].ItemsList[index + 1];
                            testPlaybill.ProgramsList[playbillTree.SelectedNode.Parent.Index].WindowsList[playbillTree.SelectedNode.Index].ItemsList[index + 1] = itemToDown;

                            PreviewWindow.programsWithWindows[playbillTree.SelectedNode.Parent.Index][playbillTree.SelectedNode.Index].Animations[index] = PreviewWindow.programsWithWindows[playbillTree.SelectedNode.Parent.Index][playbillTree.SelectedNode.Index].Animations[index + 1];
                            PreviewWindow.programsWithWindows[playbillTree.SelectedNode.Parent.Index][playbillTree.SelectedNode.Index].Animations[index + 1] = animationToDown;
                        }
                    }
                }
            }
        }

        private Bitmap DrawMarkFont(string sImageText, int size, Color color)
        {
            Bitmap bmp = new Bitmap((int)CurrentWindow.Width, (int)CurrentWindow.Height);
            using(Graphics gr = Graphics.FromImage(bmp))
                gr.Clear(PreviewWindow.OffLEDColor);
            
            lock (bmp)
            {
                double multiplier = 1;
                int fitwidth = 0, len1 = 0, len2 = 0, leftEdge = 0;
                System.Collections.Hashtable font = new System.Collections.Hashtable();

                switch(size)
                {
                    case 8:
                        font = mark8fontHashTable;
                        fitwidth = 6;
                        break;
                    case 12:
                        font = mark12fontHashTable;
                        fitwidth = 8;
                        break;
                    case 16:
                        font = markBoldfontHashTable;//mark16fontHashTable;
                        fitwidth = 12;
                        break;
                    case 24:
                        font = markBoldfontHashTable;//mark16fontHashTable;//mark12fontHashTable;
                        multiplier = 1.5;
                        fitwidth = 16;
                        break;
                    case 32:
                        font = markBoldfontHashTable;//mark16fontHashTable;
                        multiplier = 2;
                        fitwidth = 24;
                        break;
                }

                for (int i = 0; i < sImageText.Length; i++)
                {
                    if (sImageText[i] == '\r')
                    {
                        i += 2;
                        len1 = len2;
                        len2 = 0;
                    }

                    bool[,] charBits = (bool[,])font[(int)sImageText[i]];
                    if(charBits != null)
                        len2 += charBits.GetLength(1);
                }
                if (len1 == 0)
                    len1 = len2;

                leftEdge = (CurrentWindow.Width - len1 * multiplier < 0) ? 
                    0 : (int)(((CurrentWindow.Width - len1 * multiplier) / 2) / fitwidth) * fitwidth;

                int startX = leftEdge, startY = 0;

                if (!sImageText.Contains("\r\n") && CurrentWindow.Height >= size)
                    startY = (int)(((CurrentWindow.Height - size) / 2));

                for (int i = 0; i < sImageText.Length; i++)
                {
                    if (sImageText[i] == '\r')
                    {
                        i += 2;
                        leftEdge = (CurrentWindow.Width - len2 * multiplier < 0) ?
                            0 : (int)(((CurrentWindow.Width - len2 * multiplier) / 2) / fitwidth) * fitwidth;

                        startX = leftEdge;
                        startY = size;
                    }

                    bool[,] charBits = (bool[,])font[(int)sImageText[i]];
                    if (charBits != null)
                    {
                        for (int x = 0; x < charBits.GetLength(1); x++)
                        {
                            for (int y = 0; y < charBits.GetLength(0); y++)
                            {
                                if (startX + (x * multiplier) >= bmp.Width)
                                {
                                    x = bmp.Width;
                                    y = bmp.Height;
                                    continue;
                                }

                                if (charBits[y, x] == true)
                                {
                                    for (int mulx = (int)(x * multiplier); mulx < (x + 1) * multiplier; mulx++)
                                    {
                                        for (int muly = (int)(y * multiplier); muly < (y + 1) * multiplier; muly++)
                                        {
                                            if (startX + mulx < bmp.Width && startY + muly < bmp.Height)
                                                bmp.SetPixel(startX + mulx, startY + muly, color);
                                        }
                                    }
                                    //bmp.SetPixel(startX + x, startY + y, color);
                                }
                            }
                        }

                        startX += (int)(charBits.GetLength(1) * multiplier);
                    }
                }
            }

            return bmp;
        }

        private Bitmap CreateBitmapImage(string sImageText, Font font, Color color)
        {
            if (string.IsNullOrEmpty(sImageText))
                return null;

            Bitmap objBmpImage = new Bitmap(1, 1);
            int intWidth = 0;
            int intHeight = 0;


            // Create the Font object for the image text drawing
            Font objFont = new Font(font.Name, font.Size, font.Style, GraphicsUnit.Pixel);
            //objFont.Unit = GraphicsUnit.Pixel;
            //new Font("Arial", 20, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel);


            // Create a graphics object to measure the text's width and height
            Graphics objGraphics = Graphics.FromImage(objBmpImage);


            // This is where the bitmap size is determined
            intWidth = (int)objGraphics.MeasureString(sImageText, objFont).Width;
            intHeight = (int)objGraphics.MeasureString(sImageText, objFont).Height;


            // Create the bmpImage again with the correct size for the text and font.

            objBmpImage = new Bitmap(objBmpImage, new Size(intWidth, intHeight));


            // Add the colors to the new bitmap.

            objGraphics = Graphics.FromImage(objBmpImage);


            // Set Background color

            objGraphics.Clear(PreviewWindow.OffLEDColor);

            objGraphics.SmoothingMode = SmoothingMode.None;//HighQuality;

            //objGraphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixel;

            objGraphics.TextRenderingHint = TextRenderingHint.SystemDefault;

            //StringFormat sf = (StringFormat)StringFormat.GenericTypographic.Clone();

            //sf.Alignment = StringAlignment.Center;

            //sf.LineAlignment = StringAlignment.Center;

            //sf.Trimming = StringTrimming.Word;

            //objGraphics.DrawString(sImageText, objFont,
            //   new SolidBrush(color), new Rectangle(0, 0, intWidth, intHeight), sf);


            objGraphics.DrawString(sImageText, objFont,
                //new SolidBrush(Color.FromArgb(102, 102, 102))
                new SolidBrush(color)
                , 0, 0);
            
            objGraphics.Flush();
            objGraphics.Dispose();

            return (objBmpImage);

        }

        private void upButton_Click(object sender, EventArgs e)
        {
            if (playbillTree.SelectedNode != null)
            {
                if (playbillTree.SelectedNode.Name != "mainNode" && playbillTree.SelectedNode.Index > 0)
                {
                    TreeNode nodeToUp = playbillTree.SelectedNode;
                    int index = nodeToUp.Index;
                    TreeNode nodeToDown = playbillTree.SelectedNode.PrevNode;

                    playbillTree.SelectedNode = nodeToUp.Parent;

                    if (nodeToUp.Name != "New")
                    {
                        playbillTree.BeginUpdate();

                        playbillTree.SelectedNode.Nodes.RemoveAt(index);
                        playbillTree.SelectedNode.Nodes.RemoveAt(index - 1);
                        playbillTree.SelectedNode.Nodes.Insert(index - 1, nodeToUp);
                        playbillTree.SelectedNode.Nodes.Insert(index, nodeToDown);

                        playbillTree.Focus();
                        playbillTree.EndUpdate();

                        if (playbillTree.SelectedNode.Tag.ToString() == "playbill")
                        {
                            PlayProgram progToUp = testPlaybill.ProgramsList[index];
                            List<WindowOnPreview> programOnPreviewToUp = PreviewWindow.programsWithWindows[index];

                            testPlaybill.ProgramsList[index] = testPlaybill.ProgramsList[index - 1];
                            testPlaybill.ProgramsList[index - 1] = progToUp;

                            PreviewWindow.programsWithWindows[index] = PreviewWindow.programsWithWindows[index - 1];
                            PreviewWindow.programsWithWindows[index - 1] = programOnPreviewToUp;
                        }
                        else if (playbillTree.SelectedNode.Tag.ToString() == "program")
                        {
                            PlayWindow windowToUp = testPlaybill.ProgramsList[playbillTree.SelectedNode.Index].WindowsList[index];
                            WindowOnPreview windowOnPreviewToUp = PreviewWindow.programsWithWindows[playbillTree.SelectedNode.Index][index];

                            testPlaybill.ProgramsList[playbillTree.SelectedNode.Index].WindowsList[index] = testPlaybill.ProgramsList[playbillTree.SelectedNode.Index].WindowsList[index - 1];
                            testPlaybill.ProgramsList[playbillTree.SelectedNode.Index].WindowsList[index - 1] = windowToUp;

                            PreviewWindow.programsWithWindows[playbillTree.SelectedNode.Index][index] = PreviewWindow.programsWithWindows[playbillTree.SelectedNode.Index][index - 1];
                            PreviewWindow.programsWithWindows[playbillTree.SelectedNode.Index][index - 1] = windowOnPreviewToUp;
                        }
                        else if (playbillTree.SelectedNode.Tag.ToString() == "window")
                        {
                            PlayWindowItem itemToUp = testPlaybill.ProgramsList[playbillTree.SelectedNode.Parent.Index].WindowsList[playbillTree.SelectedNode.Index].ItemsList[index];
                            PreviewAnimation animationToUp = PreviewWindow.programsWithWindows[playbillTree.SelectedNode.Parent.Index][playbillTree.SelectedNode.Index].Animations[index];

                            testPlaybill.ProgramsList[playbillTree.SelectedNode.Parent.Index].WindowsList[playbillTree.SelectedNode.Index].ItemsList[index] = testPlaybill.ProgramsList[playbillTree.SelectedNode.Parent.Index].WindowsList[playbillTree.SelectedNode.Index].ItemsList[index - 1];
                            testPlaybill.ProgramsList[playbillTree.SelectedNode.Parent.Index].WindowsList[playbillTree.SelectedNode.Index].ItemsList[index - 1] = itemToUp;

                            PreviewWindow.programsWithWindows[playbillTree.SelectedNode.Parent.Index][playbillTree.SelectedNode.Index].Animations[index] = PreviewWindow.programsWithWindows[playbillTree.SelectedNode.Parent.Index][playbillTree.SelectedNode.Index].Animations[index - 1];
                            PreviewWindow.programsWithWindows[playbillTree.SelectedNode.Parent.Index][playbillTree.SelectedNode.Index].Animations[index - 1] = animationToUp;
                        }
                    }
                }
            }
        }


        private void OnDrawItem(object sender, DrawItemEventArgs e)
        {
            Graphics g = e.Graphics;
            Pen p = new Pen(Color.Blue);
            g.DrawRectangle(p, ((TabControl)sender).GetTabRect(0));
        }

        private void settingsTabControl_DrawItem(object sender, DrawItemEventArgs e)
        {

        }

        private void DrawTabControlTabs(TabControl tabControl, DrawItemEventArgs e, ImageList images)
        {
            // Get the bounding end of tab strip rectangles.
            Rectangle tabstripEndRect = tabControl.GetTabRect(tabControl.TabPages.Count - 1);
            RectangleF tabstripEndRectF = new RectangleF(tabstripEndRect.X + tabstripEndRect.Width, tabstripEndRect.Y - 5,
         tabControl.Width - (tabstripEndRect.X + tabstripEndRect.Width), tabstripEndRect.Height + 5);

            // First, do the end of the tab strip.
            // If we have an image use it.
            if (tabControl.Parent.BackgroundImage != null)
            {
                RectangleF src = new RectangleF(tabstripEndRectF.X + tabControl.Left, tabstripEndRectF.Y + tabControl.Top, tabstripEndRectF.Width, tabstripEndRectF.Height);
                e.Graphics.DrawImage(tabControl.Parent.BackgroundImage, tabstripEndRectF, src, GraphicsUnit.Pixel);
            }
            // If we have no image, use the background color.
            else
            {
                using (Brush backBrush = new SolidBrush(tabControl.Parent.BackColor))
                {
                    e.Graphics.FillRectangle(backBrush, tabstripEndRectF);
                }
            }

            // Set up the page and the various pieces.
            TabPage page = tabControl.TabPages[e.Index];
            Brush BackBrush = new SolidBrush(page.BackColor);
            Brush ForeBrush = new SolidBrush(page.ForeColor);
            string TabName = page.Text;

            // Set up the offset for an icon, the bounding rectangle and image size and then fill the background.
            int iconOffset = 0;
            Rectangle tabBackgroundRect = e.Bounds;
            e.Graphics.FillRectangle(BackBrush, tabBackgroundRect);

            // If we have images, process them.
            if (images != null)
            {
                // Get sice and image.
                Size size = images.ImageSize;
                Image icon = null;
                if (page.ImageIndex > -1)
                    icon = images.Images[page.ImageIndex];
                else if (page.ImageKey != "")
                    icon = images.Images[page.ImageKey];

                // If there is an image, use it.
                if (icon != null)
                {
                    Point startPoint = new Point(tabBackgroundRect.X + 2 + ((tabBackgroundRect.Height - size.Height) / 2),
                 tabBackgroundRect.Y + 2 + ((tabBackgroundRect.Height - size.Height) / 2));
                    e.Graphics.DrawImage(icon, new Rectangle(startPoint, size));
                    iconOffset = size.Width + 4;
                }
            }

            // Draw out the label.
            Rectangle labelRect = new Rectangle(tabBackgroundRect.X + iconOffset, tabBackgroundRect.Y + 3,
         tabBackgroundRect.Width - iconOffset, tabBackgroundRect.Height - 3);
            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Center;
            e.Graphics.DrawString(TabName, e.Font, ForeBrush, labelRect, sf);

            //Dispose objects
            sf.Dispose();
            BackBrush.Dispose();
            ForeBrush.Dispose();
        }

        private void DrawTabControlTabs(object sender, DrawItemEventArgs e)
        {
            TabControl tabControl = ((TabControl)sender);

            // Get the bounding end of tab strip rectangles.
            //  Rectangle tabstripEndRect = tabControl.GetTabRect(tabControl.TabPages.Count - 1);
            Rectangle tabstripEndRect = tabControl.GetTabRect(e.Index);

            RectangleF tabstripEndRectF = new RectangleF(tabstripEndRect.X + tabstripEndRect.Width, tabstripEndRect.Y - 5,
         tabControl.Width  /*- (tabstripEndRect.X + tabstripEndRect.Width)*/, tabstripEndRect.Height + 5);
            tabstripEndRectF.Width += 1100;
            // First, do the end of the tab strip.
            // If we have an image use it.
            if (this.BackgroundImage != null)
            {
                RectangleF src = new RectangleF(tabstripEndRectF.X + tabControl.Left, tabstripEndRectF.Y + tabControl.Top, tabstripEndRectF.Width, tabstripEndRectF.Height);
                e.Graphics.DrawImage(this.BackgroundImage, tabstripEndRectF, src, GraphicsUnit.Pixel);
            }
            // If we have no image, use the background color.
            else
            {
                using (Brush backBrush = new SolidBrush(tabControl.Parent.BackColor))
                {

                    e.Graphics.FillRectangle(backBrush, tabstripEndRectF);
                }
            }

            // Set up the page and the various pieces.
            TabPage page = tabControl.TabPages[e.Index];
            Brush BackBrush = new SolidBrush(page.BackColor);
            Brush ForeBrush = new SolidBrush(page.ForeColor);
            string TabName = page.Text;

            // Set up the offset for an icon, the bounding rectangle and image size and then fill the background.
            int iconOffset = 0;
            Rectangle tabBackgroundRect = e.Bounds;
            e.Graphics.FillRectangle(BackBrush, tabBackgroundRect);



            // Draw out the label.
            Rectangle labelRect = new Rectangle(tabBackgroundRect.X + iconOffset, tabBackgroundRect.Y + 3,
         tabBackgroundRect.Width - iconOffset, tabBackgroundRect.Height - 3);
            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Center;
            e.Graphics.DrawString(TabName, e.Font, ForeBrush, labelRect, sf);

            //Dispose objects
            sf.Dispose();
            BackBrush.Dispose();
            ForeBrush.Dispose();
        }

        private void effectsTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null && e.Node.Tag.ToString() != "Group")
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;


                int oldEffect = testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Effect;
                testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Effect =
                        int.Parse(e.Node.Tag.ToString());

                if (testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].ItemType == PlayWindowItemType.Bitmap)
                {
                    SetConfigurationSettingText("LedConfiguration/Program/Defaults/Bitmap/Effect", int.Parse(e.Node.Tag.ToString()).ToString());
                    currentBitmapEffectLabel.Text = e.Node.Name;
                }
                else if (testPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].ItemType == PlayWindowItemType.BitmapText)
                {
                    SetConfigurationSettingText("LedConfiguration/Program/Defaults/BitmapText/Effect", int.Parse(e.Node.Tag.ToString()).ToString());
                    currentEffectLabel.Text = e.Node.Name;
                }

                LoadConfiguration();
                
                int effectNumber = CurrentPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Effect;

                if (Enum.GetName(typeof(EffectType), effectNumber).Contains("Scroll"))
                {
                    if (CurrentPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].ItemType == PlayWindowItemType.Bitmap)
                    {
                        bitmapStayTimeUpDown.Minimum = 1;
                        bitmapStayTimeLabel.Visible = false;
                        bitmapRepeatLabel.Visible = true;
                    }
                    else if (CurrentPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].ItemType == PlayWindowItemType.BitmapText)
                    {
                        bitmaptextStayTimeUpDown.Minimum = 1;
                        bitmapTextStayTimeLabel.Visible = false;
                        bitmaptextRepeatLabel.Visible = true;
                    }
                }
                else if (!Enum.GetName(typeof(EffectType), effectNumber).Contains("Scroll"))
                {
                    if (CurrentPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].ItemType == PlayWindowItemType.Bitmap)
                    {
                        bitmapStayTimeUpDown.Minimum = 0;
                        bitmapStayTimeLabel.Visible = true;
                        bitmapRepeatLabel.Visible = false;
                    }
                    else if (CurrentPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].ItemType == PlayWindowItemType.BitmapText)
                    {
                        bitmaptextStayTimeUpDown.Minimum = 0;
                        bitmapTextStayTimeLabel.Visible = true;
                        bitmaptextRepeatLabel.Visible = false;
                    }
                }

                Font tmpFont = (e.Node.NodeFont == null) ? ((TreeView)sender).Font : e.Node.NodeFont;
                //((TreeView)sender).SelectedNode.NodeFont = new Font(tmpFont.Name, tmpFont.Size, tmpFont.Style ^ FontStyle.Bold);
                e.Node.BackColor = Color.SkyBlue;

                //Font tmpFont = (e.Node.NodeFont == null) ? ((TreeView)sender).Font : e.Node.NodeFont;
                //e.Node.NodeFont = new Font(tmpFont, FontStyle.Bold);

                if (Enum.GetName(typeof(EffectType), effectNumber).Contains("Scroll") || Enum.GetName(typeof(EffectType), oldEffect).Contains("Scroll"))
                    RtbToBitmap(playbillTree.SelectedNode);

                previewWindow.SelectedWindow.Animations[itemNumber].Effect = (EffectType)int.Parse(e.Node.Tag.ToString());
                previewWindow.SelectedWindow.Animate(false, itemNumber);
            }
        }

        private void effectsTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            ((TreeView)sender).CollapseAll();
        }

        private void effectsTreeView_NodeMouseHover(object sender, TreeNodeMouseHoverEventArgs e)
        {
            //if(e.Node.Tag.ToString() == "Group")
            //{
            //    if (e.Node.IsExpanded)
            //        e.Node.Collapse();
            //    else
            //        e.Node.Expand();
            //}
        }

        private void bitmaptextRichTextBox_KeyUp(object sender, KeyEventArgs e)
        {

        }

        void regeneratePreviewTimer_Tick(object sender, EventArgs e)
        {
            int selectionIndex = bitmaptextRichTextBox.SelectionStart;
            int selectionLength = bitmaptextRichTextBox.SelectionLength;

            fromTimer = true;
            RtbToBitmap(playbillTree.SelectedNode);
            fromTimer = false;

            bitmaptextRichTextBox.SelectionStart = selectionIndex;
            bitmaptextRichTextBox.SelectionLength = selectionLength;

            regeneratePreviewTimer.Stop();
            regeneratePreviewTimer.Dispose();
            regeneratePreviewTimer = null;
        }

        void testVersionTimer_Tick(object sender, EventArgs e)
        {
            MessageBox.Show("This is test version of the program. You use it on your own risk.", "LedConfig TEST VERSION");
        }

        private void bitmaptextFontColorComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index != -1)
            {
                if (e.Index == ((ComboBox)sender).SelectedIndex)
                {
                    if (bitmapTextSelectedFontColor != -1)
                    {
                        Rectangle oldItemRect = new Rectangle(0, bitmapTextSelectedFontColor * 15, ((ComboBox)sender).DropDownWidth - 2, ((ComboBox)sender).ItemHeight);
                        //e.Graphics.FillRectangle(new SolidBrush(Color.White), oldItemRect);

                        e.Graphics.FillRectangle(new SolidBrush(Color.FromName(((ComboBox)sender).Items[bitmapTextSelectedFontColor].ToString())), oldItemRect);

                    }
                    //e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(150, 0xE1, 0xE5, 0xEE)), e.Bounds);
                    bitmapTextSelectedFontColor = e.Index;
                }
                else
                    e.Graphics.FillRectangle(new SolidBrush(Color.White), e.Bounds);

                e.Graphics.FillRectangle(new SolidBrush(Color.FromName(((ComboBox)sender).Items[e.Index].ToString())), e.Bounds);

                if (e.Index == ((ComboBox)sender).SelectedIndex && ((ComboBox)sender).DroppedDown)
                {
                    HatchBrush selectionBrush = new HatchBrush(HatchStyle.DiagonalBrick, Color.FromArgb(230, 0xE1, 0xE5, 0xEE), Color.FromName(((ComboBox)sender).Items[e.Index].ToString()));

                    e.Graphics.FillRectangle(selectionBrush, e.Bounds);

                    //Pen selectionPen = new Pen(Color.FromArgb(0xE1, 0xE5, 0xEE));
                    //selectionPen.DashStyle = DashStyle.DashDotDot;
                    //selectionPen.Width = 1;
                    //e.Graphics.DrawRectangle(selectionPen, e.Bounds);
                }
            }
        }

        private void bitmaptextBackgroundComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index != -1)
            {
                if (e.Index == ((ComboBox)sender).SelectedIndex)
                {
                    if (bitmapTextSelectedBackgroundColor != -1)
                    {
                        Rectangle oldItemRect = new Rectangle(0, bitmapTextSelectedBackgroundColor * 15, ((ComboBox)sender).DropDownWidth - 2, ((ComboBox)sender).ItemHeight);
                        //e.Graphics.FillRectangle(new SolidBrush(Color.White), oldItemRect);

                        e.Graphics.FillRectangle(new SolidBrush(Color.FromName(((ComboBox)sender).Items[bitmapTextSelectedBackgroundColor].ToString())), oldItemRect);

                    }
                    //e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(150, 0xE1, 0xE5, 0xEE)), e.Bounds);
                    bitmapTextSelectedBackgroundColor = e.Index;
                }
                else
                    e.Graphics.FillRectangle(new SolidBrush(Color.White), e.Bounds);

                e.Graphics.FillRectangle(new SolidBrush(Color.FromName(((ComboBox)sender).Items[e.Index].ToString())), e.Bounds);

                if (e.Index == ((ComboBox)sender).SelectedIndex && ((ComboBox)sender).DroppedDown)
                {
                    HatchBrush selectionBrush = new HatchBrush(HatchStyle.DiagonalBrick, Color.FromArgb(230, 0xE1, 0xE5, 0xEE), Color.FromName(((ComboBox)sender).Items[e.Index].ToString()));

                    e.Graphics.FillRectangle(selectionBrush, e.Bounds);

                    //Pen selectionPen = new Pen(Color.FromArgb(0xE1, 0xE5, 0xEE));
                    //selectionPen.DashStyle = DashStyle.DashDotDot;
                    //selectionPen.Width = 1;
                    //e.Graphics.DrawRectangle(selectionPen, e.Bounds);
                }
            }
        }

        private void clockFontColorComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index != -1)
            {
                if (e.Index == ((ComboBox)sender).SelectedIndex)
                {
                    if (clockFontColor != -1)
                    {
                        Rectangle oldItemRect = new Rectangle(0, clockFontColor * 15, ((ComboBox)sender).DropDownWidth - 2, ((ComboBox)sender).ItemHeight);
                        //e.Graphics.FillRectangle(new SolidBrush(Color.White), oldItemRect);

                        e.Graphics.FillRectangle(new SolidBrush(Color.FromName(((ComboBox)sender).Items[clockFontColor].ToString())), oldItemRect);

                    }
                    //e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(150, 0xE1, 0xE5, 0xEE)), e.Bounds);
                    clockFontColor = e.Index;
                }
                else
                    e.Graphics.FillRectangle(new SolidBrush(Color.White), e.Bounds);

                e.Graphics.FillRectangle(new SolidBrush(Color.FromName(((ComboBox)sender).Items[e.Index].ToString())), e.Bounds);

                if (e.Index == ((ComboBox)sender).SelectedIndex && ((ComboBox)sender).DroppedDown)
                {
                    HatchBrush selectionBrush = new HatchBrush(HatchStyle.DiagonalBrick, Color.FromArgb(230, 0xE1, 0xE5, 0xEE), Color.FromName(((ComboBox)sender).Items[e.Index].ToString()));

                    e.Graphics.FillRectangle(selectionBrush, e.Bounds);

                    //Pen selectionPen = new Pen(Color.FromArgb(0xE1, 0xE5, 0xEE));
                    //selectionPen.DashStyle = DashStyle.DashDotDot;
                    //selectionPen.Width = 1;
                    //e.Graphics.DrawRectangle(selectionPen, e.Bounds);
                }
            }
        }

        private void temperatureFontColorComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index != -1)
            {
                if (e.Index == ((ComboBox)sender).SelectedIndex)
                {
                    if (temperatureFontColor != -1)
                    {
                        Rectangle oldItemRect = new Rectangle(0, temperatureFontColor * 15, ((ComboBox)sender).DropDownWidth - 2, ((ComboBox)sender).ItemHeight);
                        //e.Graphics.FillRectangle(new SolidBrush(Color.White), oldItemRect);

                        e.Graphics.FillRectangle(new SolidBrush(Color.FromName(((ComboBox)sender).Items[temperatureFontColor].ToString())), oldItemRect);

                    }
                    //e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(150, 0xE1, 0xE5, 0xEE)), e.Bounds);
                    temperatureFontColor = e.Index;
                }
                else
                    e.Graphics.FillRectangle(new SolidBrush(Color.White), e.Bounds);

                e.Graphics.FillRectangle(new SolidBrush(Color.FromName(((ComboBox)sender).Items[e.Index].ToString())), e.Bounds);

                if (e.Index == ((ComboBox)sender).SelectedIndex && ((ComboBox)sender).DroppedDown)
                {
                    HatchBrush selectionBrush = new HatchBrush(HatchStyle.DiagonalBrick, Color.FromArgb(230, 0xE1, 0xE5, 0xEE), Color.FromName(((ComboBox)sender).Items[e.Index].ToString()));

                    e.Graphics.FillRectangle(selectionBrush, e.Bounds);

                    //Pen selectionPen = new Pen(Color.FromArgb(0xE1, 0xE5, 0xEE));
                    //selectionPen.DashStyle = DashStyle.DashDotDot;
                    //selectionPen.Width = 1;
                    //e.Graphics.DrawRectangle(selectionPen, e.Bounds);
                }
            }
        }

        private void languageComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index != -1)
            {
                if (e.Index == ((ComboBox)sender).SelectedIndex)
                {
                    if (selectedCountry != -1)
                    {
                        Rectangle oldItemRect = new Rectangle(0, selectedCountry * 15, ((ComboBox)sender).DropDownWidth - 2, ((ComboBox)sender).ItemHeight);
                        e.Graphics.FillRectangle(new SolidBrush(Color.White), oldItemRect);
                        e.Graphics.DrawImage(flagsList.Images[selectedCountry], oldItemRect.Left, oldItemRect.Top + 2);
                        e.Graphics.DrawString(((ComboBox)sender).Items[selectedCountry].ToString(), e.Font, new SolidBrush(Color.Black), new Point(oldItemRect.Left + 18, oldItemRect.Top));
                    }
                    e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(0xE1, 0xE5, 0xEE)), e.Bounds);
                    selectedCountry = e.Index;
                }
                else
                    e.Graphics.FillRectangle(new SolidBrush(Color.White), e.Bounds);

                e.Graphics.DrawImage(flagsList.Images[e.Index], e.Bounds.Left, e.Bounds.Top + 2);
                e.Graphics.DrawString(((ComboBox)sender).Items[e.Index].ToString(), e.Font, new SolidBrush(Color.Black), new Point(e.Bounds.Left + 18, e.Bounds.Top));
            }
        }

        private void poweredByLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Link.LinkData.ToString());
        }

        private void showProgramSettingsButton_Click(object sender, EventArgs e)
        {
            if (programSettingsForm.WindowState == FormWindowState.Minimized)
                programSettingsForm.WindowState = FormWindowState.Normal;
            else
                programSettingsForm.Show();

            programSettingsForm.BringToFront();
        }

        public static DateTime RetrieveLinkerTimestamp()
        {
            string filePath = System.Reflection.Assembly.GetCallingAssembly().Location;
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;
            byte[] b = new byte[2048];
            System.IO.Stream s = null;

            try
            {
                s = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                s.Read(b, 0, 2048);
            }
            finally
            {
                if (s != null)
                {
                    s.Close();
                }
            }

            int i = System.BitConverter.ToInt32(b, c_PeHeaderOffset);
            int secondsSince1970 = System.BitConverter.ToInt32(b, i + c_LinkerTimestampOffset);
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0);
            dt = dt.AddSeconds(secondsSince1970);
            dt = dt.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(dt).Hours);
            return dt;
        }

        private void playbillTree_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            //e.Node.Parent.Collapse(false);
            if (e.Node == playbillTree.Nodes[0])
                return;

            foreach (TreeNode node in e.Node.Parent.Nodes)
                node.Collapse(true);
            //((TreeView)sender).CollapseAll();
        }

        #endregion

        #region Set time

        #region Manually
        #region Set time form Program Page
        private void SendProgramTimeButton_Click(object sender, EventArgs e)
        {
            if (Communication.Type != CommunicationType.None)
            {
                logWindow.Show();
                logWindow.Clearlog();

                DateTime date = new DateTime(dateProgramPicker.Value.Year, dateProgramPicker.Value.Month, dateProgramPicker.Value.Day,
                                             timeProgramPicker.Value.Hour, timeProgramPicker.Value.Minute, timeProgramPicker.Value.Second);

                CommunicationResult retval = CommunicationResult.Failed;
                int attepts = 0;

                logWindow.Message = logMessagesHashTable["logMessage_settingTime"].ToString();

                while (retval != CommunicationResult.Success && attepts < 3)
                {
                    retval = Communication.SetTime(date);
                    ++attepts;
                }

                logWindow.Message = (retval == CommunicationResult.Success) ? logMessagesHashTable["logMessage_settingTimeSuccess"].ToString() : retval.ToString();
                Thread.Sleep(3000);
                logWindow.Hide();
            }
        }

        private void currentProgramTimeCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (programCurrentTimeTimer != null)
            {
                programCurrentTimeTimer.Stop();
                programCurrentTimeTimer.Dispose();
                programCurrentTimeTimer = null;
            }

            if (((CheckBox)sender).Checked == true)
            {
                programCurrentTimeTimer = new System.Windows.Forms.Timer();
                programCurrentTimeTimer.Interval = 500;
                programCurrentTimeTimer.Tick += new EventHandler(programCurrentTimeTimer_Tick);
                programCurrentTimeTimer.Start();
                dateProgramPicker.Enabled = false;
                timeProgramPicker.Enabled = false;
            }
            else
            {
                dateProgramPicker.Enabled = true;
                timeProgramPicker.Enabled = true;
            }
        }

        void programCurrentTimeTimer_Tick(object sender, EventArgs e)
        {
            timeProgramPicker.Value = DateTime.Now;
            dateProgramPicker.Value = DateTime.Now;
        }
        #endregion

        #region Set time from Item Clock page
        private void SendClockTimeButton_Click(object sender, EventArgs e)
        {
            if (Communication.Type != CommunicationType.None)
            {
                logWindow.Show();
                logWindow.Clearlog();

                DateTime date = new DateTime(dateClockPicker.Value.Year, dateClockPicker.Value.Month, dateClockPicker.Value.Day,
                                             timeClockPicker.Value.Hour, timeClockPicker.Value.Minute, timeClockPicker.Value.Second);

                CommunicationResult retval = CommunicationResult.Failed;
                int attepts = 0;

                logWindow.Message = logMessagesHashTable["logMessage_settingTime"].ToString();

                while (retval != CommunicationResult.Success && attepts < 3)
                {
                    retval = Communication.SetTime(date);
                    ++attepts;
                }

                logWindow.Message = (retval == CommunicationResult.Success) ? logMessagesHashTable["logMessage_settingTimeSuccess"].ToString() : retval.ToString();
                Thread.Sleep(3000);
                logWindow.Hide();
            }
        }

        private void currentClockTimeCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (clockCurrentTimeTimer != null)
            {
                clockCurrentTimeTimer.Stop();
                clockCurrentTimeTimer.Dispose();
                clockCurrentTimeTimer = null;
            }

            if (((CheckBox)sender).Checked == true)
            {
                clockCurrentTimeTimer = new System.Windows.Forms.Timer();
                clockCurrentTimeTimer.Interval = 500;
                clockCurrentTimeTimer.Tick += new EventHandler(clockCurrentTimeTimer_Tick);
                clockCurrentTimeTimer.Start();
                dateClockPicker.Enabled = false;
                timeClockPicker.Enabled = false;
            }
            else
            {
                dateClockPicker.Enabled = true;
                timeClockPicker.Enabled = true;
            }
        }

        void clockCurrentTimeTimer_Tick(object sender, EventArgs e)
        {
            timeClockPicker.Value = DateTime.Now;
            dateClockPicker.Value = DateTime.Now;
        }
        #endregion
        #endregion

        #region Automatically
        private void automaticallySetTime()
        {
            if (Communication.Type == CommunicationType.None)
                return;
            else
            {
                logWindow.Message = logMessagesHashTable["logMessage_settingTime"].ToString();

                CommunicationResult retval = CommunicationResult.NoConnection;
                int attempts = 0;

                while (retval != CommunicationResult.Success && attempts < 3)
                {
                    retval = Communication.SetTime(DateTime.Now);
                    ++attempts;
                }

                logWindow.Message = (retval == CommunicationResult.Success) ? logMessagesHashTable["logMessage_settingTimeSuccess"].ToString() : retval.ToString();
                Thread.Sleep(3000);
            }
        }
        #endregion

        #endregion

        private void showScreenBrightnessButton_Click(object sender, EventArgs e)
        {
            if (brightnessForm.WindowState == FormWindowState.Minimized)
            {
                brightnessForm.WindowState = FormWindowState.Normal;
                brightnessForm.BringToFront();
            }
            else if (!brightnessForm.Visible)
            {
                brightnessForm.Show();
                brightnessForm.BringToFront();
            }
        }

        private void dateProgramPicker_ValueChanged(object sender, EventArgs e)
        {
            dayOfWeekProgramLabel.Text = ((DateTimePicker)sender).Value.DayOfWeek.ToString();
        }

        private void dateClockPicker_ValueChanged(object sender, EventArgs e)
        {
            dayOfWeekItemLabel.Text = ((DateTimePicker)sender).Value.DayOfWeek.ToString();
        }

        private void MainWindow_Activated(object sender, EventArgs e)
        {
            //this.BringToFront();
            //this.TopLevel = true;
            //brightnessForm.SendToBack;
            //foreach (Form f in this.OwnedForms)
            //{
            //    if(f.Visible)
            //        f.SendToBack();
            //}
        }

        private void button1_Click(object sender, EventArgs e)
        {
            delete = true;
            TreeNode current = playbillTree.SelectedNode;
            if (current != null)
            {

                RichTextBox rtb = copyRTB(bitmaptextRichTextBox);

                PlayWindow window = CurrentPlaybill.ProgramsList[current.Parent.Parent.Index].WindowsList[current.Parent.Index];

                Bitmap bmp = new Bitmap((int)window.Width,
                                        (int)window.Height);

                List<Bitmap> bmpList = new List<Bitmap>();

                using (Graphics gr = Graphics.FromImage(bmp))
                {
                    rtb.Width = (int)window.Width - window.ItemsList[current.Index].Offset.X;

                    gr.FillRectangle(Brushes.Black, new Rectangle(0, 0, bmp.Width, bmp.Height));

                    gr.SmoothingMode = SmoothingMode.HighSpeed;
                    gr.PageUnit = GraphicsUnit.Pixel;

                    int x = 0; // Variable that contains x value of point from we'll draw.
                    float y = 0; // Variable that contains y value of point from we'll draw.
                    int line = 0; // Variable that contains number of line we redraw
                    int charpos = 0; // Currently drawed character

                    int kerning = 0; // Auxiliary variable that'll help to determine kerning.
                    int linepos = 0; // Auxiliary variable that'll help to determine when perform linebreak.

                    float maxSize = 0; // Auxiliary variable that'll help to calculate relative y, it's biggest linespacing that occurs in line to draw.
                    float relY = y; // Relative y, that'll help to draw multi sized line on the same level.
                    float curSize = 0; // Auxiliary variable that'll help to calculate relative y, it's linespacing of character to draw.

                    float maxdesc = 0;

                    int itemNumber = (current.Tag.ToString() == "item") ? current.Index : 0;
                    previewWindow.SelectedWindow.Animations[itemNumber].Clear();
                    previewWindow.SelectedWindow.Animations[itemNumber].InputBitmaps = null;

                    while (line < bitmaptextRichTextBox.Lines.Length)
                    {
                        linepos = 0;
                        maxdesc = 0;

                        //Instructions to search line for biggest linespace goes here
                        for (int i = 0; i < bitmaptextRichTextBox.Lines[line].Length; ++i)
                        {
                            rtb.Select(rtb.GetFirstCharIndexFromLine(line) + i, 1);
                            if (maxSize < (rtb.SelectionFont.Size * rtb.SelectionFont.FontFamily.GetLineSpacing(rtb.SelectionFont.Style) / rtb.SelectionFont.FontFamily.GetEmHeight(rtb.SelectionFont.Style)))
                                maxSize = (rtb.SelectionFont.Size * rtb.SelectionFont.FontFamily.GetLineSpacing(rtb.SelectionFont.Style) / rtb.SelectionFont.FontFamily.GetEmHeight(rtb.SelectionFont.Style));

                            maxdesc += ((rtb.SelectedText != " ") ? Convert.ToInt32(gr.MeasureString(rtb.SelectedText, rtb.SelectionFont).Width) - (Convert.ToInt32(gr.MeasureString(" ", rtb.SelectionFont).Width) / 2) : Convert.ToInt32(gr.MeasureString(rtb.SelectedText, rtb.SelectionFont).Width));
                        }

                        // Instructions to initialize x with value corresponding to horizontalAlignment od current line.

                        charpos = rtb.GetFirstCharIndexFromLine(line);
                        rtb.Select(charpos, 1);

                        if (rtb.SelectionAlignment == HorizontalAlignment.Left)
                        {
                            x = -3;
                        }
                        else if (rtb.SelectionAlignment == HorizontalAlignment.Right)
                        {
                            charpos = rtb.GetFirstCharIndexFromLine(line) + rtb.Lines[line].Length - 1;
                            rtb.Select(charpos, 1);

                            x = bmp.Width - 1 -
                                ((rtb.SelectedText != " ") ? Convert.ToInt32(gr.MeasureString(rtb.SelectedText, rtb.SelectionFont).Width) - (Convert.ToInt32(gr.MeasureString(" ", rtb.SelectionFont).Width) / 2) : Convert.ToInt32(gr.MeasureString(rtb.SelectedText, rtb.SelectionFont).Width));

                        }
                        else if (rtb.SelectionAlignment == HorizontalAlignment.Center)
                        {
                            x = Convert.ToInt32((bmp.Width - maxdesc) / 2.0);
                        }

                        while (linepos < bitmaptextRichTextBox.Lines[line].Length)
                        {
                            relY = y;

                            // Instructions to draw string goes here
                            // Point is to draw char by char, with all stylisation, and proper char spacing.
                            // It should be modificated to y'in for multi-size characters.
                            // And then for right and center aligned text's.
                            rtb.Select(charpos, 1);

                            curSize = (rtb.SelectionFont.Size * rtb.SelectionFont.FontFamily.GetLineSpacing(rtb.SelectionFont.Style) / rtb.SelectionFont.FontFamily.GetEmHeight(rtb.SelectionFont.Style));


                            if (maxSize > curSize)
                            {
                                // Instructions to calculate relY;
                                // It's probably should be sth like y += maxSize - currsize
                                relY += (curSize - maxSize);
                            }


                            //  gr.FillRectangle(new SolidBrush(rtb.SelectionBackColor), new RectangleF(x, relY, Convert.ToInt32(gr.MeasureString(rtb.SelectedText, rtb.SelectionFont).Width), Convert.ToInt32(gr.MeasureString(rtb.SelectedText, rtb.SelectionFont).Height)));


                            gr.FillRectangle(new SolidBrush(rtb.SelectionBackColor), new RectangleF(x, y, Convert.ToInt32(gr.MeasureString(rtb.SelectedText, rtb.SelectionFont).Width), Convert.ToInt32(maxSize)));
                            gr.DrawString(rtb.SelectedText, rtb.SelectionFont, new SolidBrush(rtb.SelectionColor), new PointF(x, relY));

                            if (rtb.SelectionAlignment == HorizontalAlignment.Right)
                            {
                                if (charpos != rtb.GetFirstCharIndexOfCurrentLine())
                                {
                                    rtb.Select(charpos - 1, 1);
                                    kerning = (rtb.SelectedText != " ") ? Convert.ToInt32(gr.MeasureString(rtb.SelectedText, rtb.SelectionFont).Width) - (Convert.ToInt32(gr.MeasureString(" ", rtb.SelectionFont).Width) / 2) : Convert.ToInt32(gr.MeasureString(rtb.SelectedText, rtb.SelectionFont).Width);
                                }
                                x -= kerning;
                                --charpos;
                            }
                            else if (rtb.SelectionAlignment == HorizontalAlignment.Left || rtb.SelectionAlignment == HorizontalAlignment.Center)
                            {
                                kerning = (rtb.SelectedText != " ") ? Convert.ToInt32(gr.MeasureString(rtb.SelectedText, rtb.SelectionFont).Width) - (Convert.ToInt32(gr.MeasureString(" ", rtb.SelectionFont).Width) / 2) : Convert.ToInt32(gr.MeasureString(rtb.SelectedText, rtb.SelectionFont).Width);
                                x += kerning;
                                ++charpos;
                            }

                            ++linepos;
                        }

                        // Instructions to go to new line goes here
                        // Instruction for splitting bitmaps are in try-catch 
                        y += (rtb.SelectionFont.Size * rtb.SelectionFont.FontFamily.GetLineSpacing(rtb.SelectionFont.Style) / rtb.SelectionFont.FontFamily.GetEmHeight(rtb.SelectionFont.Style))
                            + (rtb.SelectionFont.Size * rtb.SelectionFont.FontFamily.GetCellDescent(rtb.SelectionFont.Style) / rtb.SelectionFont.FontFamily.GetEmHeight(rtb.SelectionFont.Style));

                        ++line;
                        //    x = -3;

                        try
                        {
                            charpos = rtb.GetFirstCharIndexFromLine(line);
                            rtb.Select(charpos, 1);

                            if (rtb.SelectionAlignment == HorizontalAlignment.Right)
                                x = bmp.Width + 3;
                            else if (rtb.SelectionAlignment == HorizontalAlignment.Left)
                                x = -3;

                            if (y + (rtb.SelectionFont.Size * rtb.SelectionFont.FontFamily.GetLineSpacing(rtb.SelectionFont.Style) / rtb.SelectionFont.FontFamily.GetEmHeight(rtb.SelectionFont.Style)) > bmp.Height)
                            {
                                //  bmpList.Add(bmp); Uncomment when shure it's all save & sound.
                                bmp.Save(line.ToString());
                                y = 0;
                            }
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            //  bmpList.Add(bmp); Uncomment when shure it's all save & sound.
                            bmp.Save(line.ToString());
                        }
                    }

                    // Instructions to summarise conversion. Add new inputBitmaps list, and so on.
                }
            }
            delete = false;
        }

        private void programRepeatNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                CurrentPlaybill.ProgramsList[selectedNode.Index].RepeatTimes = (int)((NumericUpDown)sender).Value;
            }
        }

        private void windowWaitingModeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = selectedNode.Index;
                int programNumber = selectedNode.Parent.Index;

                CurrentPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].Waiting = (byte)((ComboBox)sender).SelectedIndex;
            }
        }

        private void bitmapTransparentCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                CurrentPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Transparent = ((CheckBox)sender).Checked;
            }
        }

        private void bitmapTextTransparentCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                CurrentPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Transparent = ((CheckBox)sender).Checked;
            }
        }

        private void clockTransparentCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                CurrentPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Transparent = ((CheckBox)sender).Checked;
            }
        }

        private void temperatureTransparentCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                CurrentPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].Transparent = ((CheckBox)sender).Checked;
            }
        }

        private void playbillWidthTextBox_TextChanged(object sender, EventArgs e)
        {
            windowsizechanged = true;
        }

        private void playbillHeightTextBox_TextChanged(object sender, EventArgs e)
        {
            windowsizechanged = true;
        }

        private void oldAnimationCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            //oldAnimation = ((CheckBox)sender).Checked;
            anotherBitmaps = !((CheckBox)sender).Checked;
        }

        private void MainWindow_Move(object sender, EventArgs e)
        {
            if (PreviewWindow.programsWithWindows == null)
                return;

            //PreviewWindow.programsWithWindows[previewWindow.ActiveProgram][previewWindow.ActiveWindow].CurrentAnimation.PauseAnimation();
            foreach (var p in PreviewWindow.programsWithWindows)
            {
                foreach (WindowOnPreview w in p)
                {
                    foreach (PreviewAnimation a in w.Animations)
                        a.PauseAnimation();
                }
            }
            //PreviewWindow.programsWithWindows[previewWindow.ActiveProgram][previewWindow.ActiveWindow].CurrentAnimation.PauseAnimation();
        }

        private void MainWindow_Resize(object sender, EventArgs e)
        {
            if (PreviewWindow.programsWithWindows == null)
                return;

            foreach (var p in PreviewWindow.programsWithWindows)
            {
                foreach (WindowOnPreview w in p)
                {
                    foreach (PreviewAnimation a in w.Animations)
                        a.PauseAnimation();
                }
            }
        }

        private void MainWindow_ResizeEnd(object sender, EventArgs e)
        {
            if (PreviewWindow.programsWithWindows == null)
                return;

            foreach (var p in PreviewWindow.programsWithWindows)
            {
                foreach (WindowOnPreview w in p)
                {
                    foreach (PreviewAnimation a in w.Animations)
                        a.UnpauseAnimation();
                }
            }
        }

        private void dateTimeFormatComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            TreeNode selectedNode = playbillTree.SelectedNode;
            if (selectedNode != null)
            {
                int windowNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Index : 0;
                int programNumber = (selectedNode.Parent != null && selectedNode.Tag.ToString() == "item") ? selectedNode.Parent.Parent.Index : 0;
                int itemNumber = (selectedNode.Tag.ToString() == "item") ? selectedNode.Index : 0;

                CurrentPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber].DateFormatIndex = ((ComboBox)sender).SelectedIndex;


                GenerateClockBitmap(CurrentPlaybill.ProgramsList[programNumber].WindowsList[windowNumber].ItemsList[itemNumber]);
            }
        }

        private void clockHourCheckBox_EnabledChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked)
            {
                clockMinuteCheckBox.Enabled = true;
            }
            else
            {
                clockMinuteCheckBox.Checked = false;
                clockMinuteCheckBox.Enabled = false;
            }
        }

        private void clockMinuteCheckBox_EnabledChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked)
            {
                clockSecondsCheckBox.Enabled = true;
            }
            else
            {
                clockSecondsCheckBox.Checked = false;
                clockSecondsCheckBox.Enabled = false;
            }
        }

        private void clockMultiLineCheckBox_EnabledChanged(object sender, EventArgs e)
        {
            if (clockMultiLineCheckBox.Enabled == true)
            {
                if ((int.Parse((string)clockFontSizeComboBox.SelectedItem)) * 2 > CurrentWindow.Height)
                {
                    if (clockMultiLineCheckBox.Checked == true)
                        clockMultiLineCheckBox.Checked = false;
                    clockMultiLineCheckBox.Enabled = false;
                }
            }
        }

        private void effectsTreeView_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            if (((TreeView)sender).SelectedNode != null)
            {
                Color tmpColor = Color.Empty;
                if (((TreeView)sender).SelectedNode.BackColor != tmpColor)
                    ((TreeView)sender).SelectedNode.BackColor = tmpColor;
                //Font tmpFont = (((TreeView)sender).SelectedNode.NodeFont == null) ? ((TreeView)sender).Font : ((TreeView)sender).SelectedNode.NodeFont;
                //((TreeView)sender).SelectedNode.NodeFont = new Font(tmpFont, FontStyle.Regular);
            }
        }
    }

    public enum ApplicationUserLevel
    {
        Beginner,
        Advanced,
        Expert
    }
}
