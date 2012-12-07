using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;
using System.Xml;

namespace BengiLED_for_C_Power
{
    public partial class LocalizatorForm : Form
    {
        private List<LocalizationControlsLine> rows;
        private int selectedCountry = -1;
        private ImageList flagsList;

        public LocalizatorForm()
        {
            InitializeComponent();
            this.Width = 830;
            rows = new List<LocalizationControlsLine>();
            flagsList = new ImageList();

            flagsList.ColorDepth = ColorDepth.Depth8Bit;
            flagsList.ImageSize = new Size(16, 12);

            //flagsList.Images.Add("de", BengiLED_for_C_Power.Properties.Resources.German);
            //flagsList.Images.Add("es", BengiLED_for_C_Power.Properties.Resources.Spanish);
            //flagsList.Images.Add("fr", BengiLED_for_C_Power.Properties.Resources.French);
            //flagsList.Images.Add("pl", BengiLED_for_C_Power.Properties.Resources.Polish);
            //flagsList.Images.Add("uk", BengiLED_for_C_Power.Properties.Resources.British_english);

            ResourceManager rm = new ResourceManager("BengiLED_for_C_Power.Properties.Resources",
            Assembly.GetExecutingAssembly());

            foreach (Object obj in languagesComboBox.Items)
            {
                if (obj.ToString() == "English (US)")
                    flagsList.Images.Add(rm.GetObject("American_english") as Image);
                else if (obj.ToString() == "English (UK)")
                    flagsList.Images.Add(rm.GetObject("British_english") as Image);
                else
                    flagsList.Images.Add(rm.GetObject(obj.ToString()) as Image);
            }
            
            rm.ReleaseAllResources();
            languagesComboBox.DropDownHeight = languagesComboBox.Items.Count * 16;
        }

        private void LocalizatorForm_Load(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            
            rows.Clear();
            controlsToLocalizePanel.Controls.Clear();

            //this.Controls.Add(generateLanguageFileButton);
            //this.Controls.Add(languagesComboBox);

            //this.Controls.Clear();

            //this.Controls.Add(generateLanguageFileButton);
            //this.Controls.Add(languagesComboBox);
            languagesComboBox.SelectedIndex = languagesComboBox.Items.IndexOf("English (US)");
            languagesComboBox.SelectedText = languagesComboBox.SelectedItem.ToString();
            selectedCountry = languagesComboBox.Items.IndexOf("English (US)");

            LoadTexts(this.Owner.Owner, this.Owner.Owner.Name);

            foreach (object key in ((MainWindow)Owner.Owner).logMessagesHashTable.Keys)
            {
                rows.Add(new LocalizationControlsLine(key.ToString(), ((MainWindow)Owner.Owner).logMessagesHashTable[key].ToString(), ((MainWindow)Owner.Owner).logWindow.Name));
            }

            foreach (object key in ((MainWindow)Owner.Owner).messageBoxesHashTable.Keys)
            {
                rows.Add(new LocalizationControlsLine(key.ToString(), ((MainWindow)Owner.Owner).messageBoxesHashTable[key].ToString(), ((MainWindow)Owner.Owner).Name));
            }

            foreach (object key in ((MainWindow)Owner.Owner).tooltipsHashTable.Keys)
            {
                rows.Add(new LocalizationControlsLine(key.ToString(), ((MainWindow)Owner.Owner).tooltipsHashTable[key].ToString(), ((MainWindow)Owner.Owner).Name));
            }

            foreach(LocalizationControlsLine row in rows)
            {
                //row.SetRow(this);
                row.SetRow(controlsToLocalizePanel);
            }

            this.Cursor = Cursors.Default;
        }

        private void LoadTexts(Control parentControl, string ownerFormName)
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
                        if(!string.IsNullOrEmpty(tab.Text))
                            rows.Add(new LocalizationControlsLine(tab.Name, tab.Text, ownerFormName));
                        
                        //string localizedText = (rm == null) ? null : (string)LangHash[string.Format("{0}.Text", tab.Name)];
                        //rm.GetString(string.Format("{0}.Text", tab.Name));
                        //if (localizedText != null)
                        //  (tab).Text = localizedText;

                        //Recursive call to translate all controls in that tab
                        LoadTexts((Control)tab, ownerFormName);
                    }
                }
                else
                {
                    int number;

                    if (!string.IsNullOrEmpty(c.Text) && !(c is TextBox) && !(c is RichTextBox) && !(c is ComboBox) && !(c is NumericUpDown) && !(c is DateTimePicker)
                        && !(int.TryParse(c.Text, out number)) && !(c is LinkLabel) && (c.Name != "previewZoomLabel"))
                        rows.Add(new LocalizationControlsLine(c.Name, c.Text, ownerFormName));
                        
                    
                    if (c.Controls.Count > 0)
                        LoadTexts(c, ownerFormName);
                }
            }

            if (parentControl is Form)
            {
                foreach (Form f in ((Form)parentControl).OwnedForms)
                {
                    LoadTexts((Control)f, f.Name);
                }
            }
        }

        private void languagesComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index != -1)
            {
                if (e.Index == ((ComboBox)sender).SelectedIndex)
                {
                    if (selectedCountry != -1)
                    {
                        Rectangle oldItemRect = new Rectangle(0, selectedCountry * 15, ((ComboBox)sender).DropDownWidth-2, ((ComboBox)sender).ItemHeight);
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

        private void generateLanguageFileButton_Click(object sender, EventArgs e)
        {
            GenerateLanguageFile();
            MessageBox.Show("Language generation completed!", "LedConfig Localizator");
        }

        private void GenerateLanguageFile()
        {
            string selectedLanguage = languagesComboBox.SelectedItem.ToString();

            if (selectedLanguage == "English (US)")
                selectedLanguage = "American_english";
            else if (selectedLanguage == "English (UK)")
                selectedLanguage = "British_english";

            StreamWriter fileStream = File.CreateText(string.Format("{0}/Languages/{1}.xml", MainWindow.programSettingsFolder, selectedLanguage));
            XmlTextWriter languageFile = new XmlTextWriter(fileStream);

            languageFile.Indentation = 2;
            languageFile.IndentChar = ' ';
            languageFile.Formatting = Formatting.Indented;


            languageFile.WriteStartDocument();
            languageFile.WriteStartElement("Language");
            languageFile.WriteAttributeString("name", selectedLanguage);
            //languageFile.WriteAttributeString("culture", imageList1.Images[languagesComboBox.SelectedIndex].Tag.ToString());

            foreach (LocalizationControlsLine line in rows)
            {
                languageFile.WriteStartElement("LocaleResource"); 
                if(line.RowGroupBox.Text.Substring(line.RowGroupBox.Text.IndexOf("-> ") + 3).Contains("logMessage_"))
                    languageFile.WriteAttributeString("name", line.RowGroupBox.Text.Substring(line.RowGroupBox.Text.IndexOf("-> ") + 3));
                else if (line.RowGroupBox.Text.Substring(line.RowGroupBox.Text.IndexOf("-> ") + 3).Contains("messageBoxMessage_")
                    || line.RowGroupBox.Text.Substring(line.RowGroupBox.Text.IndexOf("-> ") + 3).Contains("messageBoxTitle_"))
                    languageFile.WriteAttributeString("name", line.RowGroupBox.Text.Substring(line.RowGroupBox.Text.IndexOf("-> ") + 3));
                else if (line.RowGroupBox.Text.Substring(line.RowGroupBox.Text.IndexOf("-> ") + 3).Contains("tooltipMessage_")
                    || line.RowGroupBox.Text.Substring(line.RowGroupBox.Text.IndexOf("-> ") + 3).Contains("tooltipTitle_"))
                    languageFile.WriteAttributeString("name", line.RowGroupBox.Text.Substring(line.RowGroupBox.Text.IndexOf("-> ") + 3));
                else
                    languageFile.WriteAttributeString("name", string.Format("{0}.Text", line.RowGroupBox.Text.Substring(line.RowGroupBox.Text.IndexOf("-> ") + 3)));
                languageFile.WriteAttributeString("value", line.LocalizedText.Text);
                languageFile.WriteEndElement();
            }

            languageFile.WriteEndElement();
            languageFile.WriteEndDocument();

            languageFile.Flush();
            languageFile.Close();
            
        }

        private void LocalizatorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();

            if (e.CloseReason == CloseReason.UserClosing)
                e.Cancel = true;
        }

        private void localizationInfoLabel_Click(object sender, EventArgs e)
        {

        }
    }

    public class LocalizationControlsLine
    {
        public Label ControlName;
        public TextBox EnglishText;
        public TextBox LocalizedText;
        public GroupBox RowGroupBox;
        public string ParentFormName;

        private static int nextRowY = 20;
        private static int rowsCount = 0;

        public LocalizationControlsLine(string controlName, string englishText, string parentFormName)
        {
            //ControlName = new Label();
            //ControlName.Height = 20;
            ParentFormName = parentFormName;

            EnglishText = new TextBox();
            EnglishText.Text = englishText;
            EnglishText.Height = 20;
            EnglishText.Width = 350;
            EnglishText.Multiline = true;
            EnglishText.ScrollBars = ScrollBars.Vertical;
            EnglishText.ReadOnly = true;

            LocalizedText = new TextBox();
            LocalizedText.Text = englishText;
            LocalizedText.Height = 20;
            LocalizedText.Width = 350;
            LocalizedText.Multiline = true;
            LocalizedText.ScrollBars = ScrollBars.Vertical;

            RowGroupBox = new GroupBox();
            RowGroupBox.Text = string.Format("{0} -> {1}", parentFormName, controlName);
            RowGroupBox.Height = 45;
            RowGroupBox.Width = EnglishText.Width + LocalizedText.Width + 60;

            RowGroupBox.Controls.Add(EnglishText);
            RowGroupBox.Controls.Add(LocalizedText);

        }

        public void SetRow(Control parent)
        {
            EnglishText.Left = 15;
            LocalizedText.Left = EnglishText.Left + EnglishText.Width + 20;
            EnglishText.Top = 15;
            LocalizedText.Top = 15;

            RowGroupBox.Left = 5;//15;
            RowGroupBox.Top = nextRowY;
            nextRowY += RowGroupBox.Height + 20;

            parent.Controls.Add(RowGroupBox);
        }
    }
}
