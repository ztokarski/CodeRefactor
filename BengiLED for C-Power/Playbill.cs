using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Windows.Forms;

namespace BengiLED_for_C_Power
{
    [Serializable()]
    public class Playbill
    {
        #region Private fields
        private string fileName = "playbill.lpp";
        private ushort width = 96;
        private ushort height = 16;
        private byte color = 1;
        private bool sendLPT = false;
        #endregion

        #region Properties
        public byte[] FileHeader
        {
            get { return new byte[]{ 0x4C, 0x4C, 0x00, 0x01 }; }
        }
        public string FileName
        {
            get
            {
                if (fileName == string.Empty)
                    fileName = "playbill.lpp";

                return fileName;
            }
            set
            {
                if (value == string.Empty)
                    fileName = "playbill.lpp";
                else
                    fileName = value;
            }
        }

        public ushort Width
        {
            get { return width; }
            set 
            { 
                width = value;
                if(this.ProgramsList != null)
                {
                    foreach (PlayProgram p in this.ProgramsList)
                    {
                        p.Width = value;
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
                if (this.ProgramsList != null)
                {
                    foreach (PlayProgram p in this.ProgramsList)
                    {
                        p.Height = value;
                    }
                }
            }
        }

        public byte Color
        {
            get { return color; }
            set
            {
                color = value;
                if (this.ProgramsList != null)
                {
                    foreach (PlayProgram p in this.ProgramsList)
                    {
                        p.Color = value;
                    }
                }
            }
        }

        public bool SendLPT
        {
            get { return sendLPT; }
            set
            {
                // check if desired value is false
                if (value == false)
                {
                    // check if there are programs that has ltp
                    foreach (PlayProgram p in ProgramsList)
                    {
                        // if yes, playbill's sendLPT should keep it's value (true)
                        if (p.LTP) 
                        {
                            // change the value to be set to true and break from the loop
                            value = true; 
                            break;
                        }
                    }
                }

                // set value
                sendLPT = value;
            }
        }

        public int FilesToSend
        {
            get
            {
                return 1 + ProgramsList.Count + ((SendLPT) ? 1 : 0);
            }
        }

        public List<PlayProgram> ProgramsList;
        #endregion

        #region Methods
        public Playbill()
        {
            ProgramsList = new List<PlayProgram>();
        }

        public Playbill(ushort width, ushort height, byte color)
        {
            Width = width;
            Height = height;
            Color = color;

            ProgramsList = new List<PlayProgram>();
        }

        ~Playbill()
        {
            ProgramsList.Clear();
        }

        public void AddProgram(string programName)
        {
            //ProgramsList.Add(new PlayProgram(programName));
            ProgramsList.Add(new PlayProgram(programName, this.Width, this.Height, this.Color));
        }
        public void AddProgram(string programName, ushort width, ushort height, byte color)
        {
            ProgramsList.Add(new PlayProgram(programName, this.Width, this.Height, this.Color));
        }

        private string GenerateShortFileName(string longFileName)
        {
            string retval = longFileName.ToUpper();
            
            for(int i = 0; i < retval.Length; i++)
            {
                if(retval[i] == ' ' || retval[i] == '.' || retval[i] == '\"' || retval[i] == '\\' ||  retval[i] == '/' || retval[i] == '['
                    || retval[i] == ']' ||    retval[i] == ':' || retval[i] == ';' || retval[i] == '=' || retval[i] == ',' )    
                {
                    retval = retval.Remove(i);
                }
            }

            if (retval.Length <= 8)
            {
                if (retval.Length != longFileName.Length)
                {
                    if(retval.Length >= 7)
                        retval = retval.Remove(6, retval.Length - 6);

                    retval = string.Format("{0}~{1}", retval, 1);
                }
            }
            else
            {
                retval = retval.Remove(6, retval.Length - 6);
                retval = string.Format("{0}~{1}", retval, 1);
            }

            return retval;
        }

        public long SaveToFile(string fileName, int cardID)
        {
            string tempDirectory = string.Format("{0}/temp", MainWindow.programSettingsFolder); //"temp";
            long filesSize = 0; // used for computing the size of all files
            FileInfo fi;
            FileName = fileName;
            int programNumber = 0;

            List<byte> playbillFileBytes = new List<byte>();


            // file header
            playbillFileBytes.AddRange(FileHeader);
            // programs count
            playbillFileBytes.AddRange(BitConverter.GetBytes((Int16)this.ProgramsList.Count));
            // width
            playbillFileBytes.AddRange(BitConverter.GetBytes(this.Width));
            // height
            playbillFileBytes.AddRange(BitConverter.GetBytes(this.Height));
            // color
            playbillFileBytes.Add(this.Color);
            // fill empty bytes of 16-byte line with 0
            while(playbillFileBytes.Count % 16 != 0)
                playbillFileBytes.Add(0x00);


            while (!Directory.Exists(string.Format("{0}", tempDirectory)))
                Directory.CreateDirectory(tempDirectory);

            // part created to saving an lpt datafile.
            byte first = 0x00, second = 0x00;
            first = (ProgramsList.Count > byte.MaxValue) ? byte.MaxValue : (byte)ProgramsList.Count;
            second = (first == byte.MaxValue) ? (byte)(ProgramsList.Count - byte.MaxValue) : (byte)0;
            byte[] head = { 0x4C, 0x54, 0x00, 0x01, first, second, 0x00 }; // line in api, 2 bytes for File ID, 2 for format version, 2 for record number, last is reservation
            byte[] body = new byte[ProgramsList.Count * 7];
            int recordnumber = 0, bodyPosition = 0; // an auxiliary variable used to determine if numbers stored in first and second bytes are correct.
            //sendLPT = false;

            foreach (PlayProgram program in ProgramsList)
            {
                string programNameForController = //string.Format("{0}.lpb", program.Name);
                    string.Format("{0}{1}.lpb", cardID.ToString("X4"), programNumber.ToString("0000"));
                
                while (!File.Exists(string.Format("{0}/{1}", tempDirectory, programNameForController)))
                {
                    program.SaveToFile(programNameForController); // here was ++programNumber
                }

                try
                {
                    fi = new FileInfo(string.Format("{0}/{1}", tempDirectory, programNameForController));
                    filesSize += fi.Length;
                }
                catch
                {
                    MessageBox.Show("Saving program file failed");
                }
                
                //program.SaveToFile(string.Format("program{0}.lpb", programNumber)); // here was ++programNumber
                
                byte[] programFileNameBytes = new byte[programNameForController.Length];
                
                System.Text.Encoding.ASCII.GetBytes(programNameForController.ToCharArray(), 0, programNameForController.Length, programFileNameBytes, 0);
                playbillFileBytes.AddRange(programFileNameBytes);
                //playbillFileBytes.Add(0x01);
                // fill empty bytes of 16-byte line with 0
                while (playbillFileBytes.Count % 16 != 0)
                    playbillFileBytes.Add(0x00);

                
                if (SendLPT == true && program.LTP == true) // if LTP == true, then this program have a time-limit settings.
                {
                    //sendLPT = true;

                    // format is -> prg number (2), week (1), begin min (1), begin h(1), end min (1), end h(1)
                    body[bodyPosition++] = ((programNumber) > byte.MaxValue) ? byte.MaxValue : (byte)(programNumber);
                    body[bodyPosition++] = (body[bodyPosition-1] == byte.MaxValue) ? (byte)(programNumber - byte.MaxValue) : (byte)0x00;
                    
                    for(int i = 0; i < program.WeekDays.Length; ++i)
                        body[bodyPosition] += (program.WeekDays[i] == true) ? (byte)(1<<i) : (byte)(0<<i);

                    body[++bodyPosition] = program.BeginMinute;
                    body[++bodyPosition] = program.BeginHour;
                    body[++bodyPosition] = program.EndMinute;
                    body[++bodyPosition] = program.EndHour;

                    ++recordnumber;
                    ++bodyPosition;
                }

                ++programNumber;
            }
            if (recordnumber != ((int)(first) + (int)(second)))
            {
                head[4] = (recordnumber > byte.MaxValue) ? byte.MaxValue : (byte)recordnumber;
                head[5] = (first == byte.MaxValue) ? (byte)(recordnumber - byte.MaxValue) : (byte)0;
            }

            if(sendLPT)
                SaveLPT(head, body, tempDirectory);

            // Regular playbill file save.

            System.IO.FileStream playbillFile = new System.IO.FileStream(string.Format("{0}/{1}", tempDirectory, FileName), System.IO.FileMode.Create);
            playbillFile.Write(playbillFileBytes.ToArray(), 0, playbillFileBytes.Count);
            playbillFile.Close();

            fi = new FileInfo(string.Format("{0}/{1}", tempDirectory, FileName));
            filesSize += fi.Length;


            if (sendLPT)
            {
                fi = new FileInfo(string.Format("{0}/{1}", tempDirectory, "playbill.lpt"));
                filesSize += fi.Length;
            }

            //long freespace = SerialCommunication.GetControllerFreeSpace();
            //if (freespace - filesSize < 0)
            //{
            //    MessageBox.Show("Wolne: " + freespace + "\nWielkośc plików : " + filesSize);
            //}

            return (long)(filesSize);
        }

        public void SaveProgramsToFile(System.IO.Stream streamfile)
        {
            //foreach (List<WindowOnPreview> program in PreviewWindow.programsWithWindows)
            //{
            //    foreach (WindowOnPreview window in program)
            //    {
            //        foreach (PreviewAnimation animation in window.Animations)
            //        {
            //            if (animation.PlaybillItem.ItemType == PlayWindowItemType.BitmapText
            //                || animation.PlaybillItem.ItemType == PlayWindowItemType.Bitmap
            //                || animation.PlaybillItem.ItemType == PlayWindowItemType.Animator)
            //            {
            //                animation.PlaybillItem.BitmapsForScreen = animation.InputBitmaps;
            //            }
            //        }
            //    }
            //}
            
            BinaryFormatter formatter = new BinaryFormatter();

            formatter.Serialize(streamfile, this);

        }

        public static Playbill LoadProgramsFromFile(System.IO.Stream streamfile)
        {
            BinaryFormatter formatter = new BinaryFormatter();

            return (Playbill)formatter.Deserialize(streamfile);
        }

        public void SaveLPT(byte[] head, byte[] body, string directory)
        {
           // bool response = false;

            string fileName = string.Format("{0}/playbill.lpt", directory);

            byte[] buff = new byte[7 + ((head[4] + head[5])*7)]; // a writed buffer should had a lenght of head (7) + fixed body (programs that use LPT * 7)
            int auxiliary = 0;
            for (int i = 0; i < head.Length; ++i)
                buff[i] = head[i];
            for (int i = head.Length; i < buff.Length; ++i)
            {
                buff[i] = body[auxiliary++];
            }

            try
            {
                System.IO.FileStream fs = new System.IO.FileStream(fileName, System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite);
                System.IO.BinaryWriter bw = new System.IO.BinaryWriter(fs);
                bw.Write(buff);
                bw.Close();
        //        response = true;
            }
            catch (Exception)
            {    
                // Console.WriteLine(ex.Message);     
            }

               // return response;
        }

        #region CheckColor
        /// <summary>
        /// Checks if color is available in current playbill depending on playbill's color.
        /// </summary>
        /// <param name="colorName">Name of color to check.</param>
        /// <returns>True if the color is available or false if it is not available.</returns>
        public bool CheckColor(string colorName)
        {
            bool retval = true;

            System.Collections.BitArray colorBits = new System.Collections.BitArray(System.BitConverter.GetBytes(this.Color));
            System.Drawing.Color colorToCheck = System.Drawing.Color.FromName(colorName);

            if (colorToCheck.R == 255 && !colorBits[0])
                return false;
            if (colorToCheck.G == 255 && !colorBits[1])
                return false;
            if (colorToCheck.B == 255 && !colorBits[2])
                return false;

            return retval;
        }

        #endregion

        #endregion
    }
}
