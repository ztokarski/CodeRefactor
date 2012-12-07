using System;
using System.Collections.Generic;
using System.Text;

namespace BengiLED_for_C_Power
{
    #region Communication class
    public static class Communication
    {
        #region Private fields
        private static CommunicationType type = CommunicationType.None;
        private static bool communicationInProgress = false;
        private static int cardID = 255;
        #endregion
        
        #region Properties
        /// <summary>
        /// Get or set communication type.
        /// </summary>
        public static CommunicationType Type
        {
            get { return type; }
            set { type = value; }
        }

        /// <summary>
        /// Get or set information about communication operation in progress.
        /// </summary>
        public static bool CommunicationInProgress
        {
            get { return communicationInProgress; }
            set { communicationInProgress = value; }
        }

        /// <summary>
        /// Controller ID.
        /// </summary>
        public static int CardID
        {
            get { return cardID; }
            set { cardID = value; }
        }

        /// <summary>
        /// Three bytes that must be at the beginning of data package in communication.
        /// </summary>
        public static byte[] CommunicationHeader
        {
            get { return new byte[3] { 0x68, 0x32, Convert.ToByte(CardID) }; }
        }
        #endregion

        #region Methods

        #region Connection methods

        #region Send
        /// <summary>
        /// Sends data using current communication device.
        /// </summary>
        /// <param name="dataPackage">Array of bytes with data to send.</param>
        /// <returns>Result of sending operation.</returns>
        public static CommunicationResult Send(byte[] dataPackage)
        {
            CommunicationResult retval = CommunicationResult.Failed;
            
            switch (Type)
            {
                case CommunicationType.None:
                    retval = CommunicationResult.NoConnection;
                    break;
                case CommunicationType.Serial:
                    retval = SerialCommunication.Send(dataPackage);
                    break;
                case CommunicationType.Network:
                    retval = NetworkCommunication.Send(dataPackage);
                    break;
                default:
                    retval = CommunicationResult.Failed;
                    break;
            }
            return retval;
        }
        #endregion

        #region Recieve
        /// <summary>
        /// Receives response package from current communication device.
        /// </summary>
        /// <param name="waitTime">Time (in miliseconds) to wait before reading from the device.</param>
        /// <param name="responsePackage">Array to put response bytes package in.</param>
        /// <param name="responseLength">Length of the response package.</param>
        /// <returns>Result of receiving operation.</returns>
        public static CommunicationResult Receive(int waitTime, out byte[] responsePackage, out int responseLength)
        {
            CommunicationResult retval = CommunicationResult.Failed;
            responsePackage = new byte[1024];
            responseLength = 0;
            
            switch (Type)
            {
                case CommunicationType.None:
                    retval = CommunicationResult.NoConnection;
                    break;
                case CommunicationType.Serial:
                    //System.Threading.Thread.Sleep(SerialCommunication.WaitTimeMultiplikator * waitTime);
                    retval = SerialCommunication.Receive(out responsePackage, out responseLength);
                    break;
                case CommunicationType.Network:
                    System.Threading.Thread.Sleep(waitTime);
                    retval = NetworkCommunication.Receive(out responsePackage, out responseLength);
                    break;
                default:
                    retval = CommunicationResult.Failed;
                    break;
            }

            return retval;
        }
        #endregion

        #region ExecuteCommand
        /// <summary>
        /// Executes command by sending request and receiving response.
        /// </summary>
        /// <param name="requestPackage">Command package to send.</param>
        /// <param name="waitTime">Time (in miliseconds) to wait before reading response.</param>
        /// <param name="responsePackage">Array to put response data in.</param>
        /// <param name="desiredReponseLength">Length of the command response package without errors.</param>
        /// <returns>Result of command execution.</returns>
        public static CommunicationResult ExecuteCommand(byte[] requestPackage, int waitTime, out byte[] responsePackage, int desiredReponseLength)
        {
            CommunicationResult retval = CommunicationResult.Success;
            responsePackage = new byte[1];

            // check if there is another communication operation in progress
            if (CommunicationInProgress)
                retval = CommunicationResult.AnotherOperationInProgress;
            else
            {
                CommunicationInProgress = true;

                #region Send request package
                // send data and get communication result
                retval = Send(requestPackage);
                #endregion
                
                // read response only if there was no error
                if (retval == CommunicationResult.Success)
                {
                    #region Receive response package

                    // variable to put actual response length in
                    int responseLength = 0;

                    // receive response
                    retval = Receive(waitTime, out responsePackage, out responseLength);
                    
                    #endregion

                    // parse response only if there was no error
                    if (retval == CommunicationResult.Success)
                    {
                        #region Check response package
                        // check length of the response package only if desiredReponseLength is known (greater than 0)
                        if (desiredReponseLength > 0 && responseLength > desiredReponseLength)
                            retval = CommunicationResult.ResponseTooLong;
                        else if (desiredReponseLength > 0 && responseLength < desiredReponseLength)
                            retval = CommunicationResult.ResponseTooShort;
                        else // if response has desired length we should check the checksum
                            retval = CheckChecksum(responsePackage);
                        #endregion
                    }
                }

                CommunicationInProgress = false;
            }

            return retval;
        }
        #endregion

        #endregion

        #region Checksum and results' checking methods
        #region GenerateChecksum
        /// <summary>
        /// Generates checksum bytes from data package.
        /// </summary>
        /// <param name="package">Array of data bytes.</param>
        /// <returns>Array of two checksum bytes in reverse order.</returns>
        public static byte[] GenerateChecksum(byte[] package)
        {
            byte[] retval = new byte[2];
            int checksum = 0;
            byte tmpchecksum = 0;
            
            foreach (byte b in package)
                checksum += b;

            // we have to divide checksum into 2 bytes in reverse order
            tmpchecksum = Convert.ToByte(checksum >> 8);
            retval[0] = (Convert.ToByte(checksum - (tmpchecksum << 8)));
            retval[1] = tmpchecksum;

            return retval;
        }
        #endregion

        #region CheckChecksum
        /// <summary>
        /// Checks if received data contains proper checksum.
        /// </summary>
        /// <param name="package">Package of data bytes received from current communication device.</param>
        /// <returns>True if the checksum is good, false if checksum is bad.</returns>
        public static CommunicationResult CheckChecksum(byte[] package)
        {
            CommunicationResult retval = CommunicationResult.Success;
            byte[] properChecksum;
            byte[] packageChecksum; 
            
            // get checksum bytes from data package
            packageChecksum = new byte[] { package[package.Length-2], package[package.Length-1] };
            // fill those bytes in packeg with 0 to pass package without checksum to checksum generating method
            package[package.Length-2] = package[package.Length-1] = 0;
            // generate checksum from data package
            properChecksum = GenerateChecksum(package);

            // compare checksum received with package and cheksum generated from package after receiving
            if ((packageChecksum[0] != properChecksum[0]) || (packageChecksum[1] != properChecksum[1]))
                retval = CommunicationResult.BadChecksum;

            return retval;
        }
        #endregion
        #endregion

        #region Controller commands

        #region TestController
        /// <summary>
        /// Test communication with controller.
        /// </summary>
        /// <returns>Result of communication test.</returns>
        public static CommunicationResult TestController()
        {
            CommunicationResult retval = CommunicationResult.Success;
            byte[] commandHeader = new byte[] { 0x01, 0x01, 0x00 };
            List<byte> requestPackage = new List<byte>();
            int desiredResponseLength = 263;
            int waitTime = 100;
            byte[] responsePackage = new byte[1];

            #region Generate request package

            int tmpID = Communication.CardID;
            //Communication.CardID = 255;

            // add communication header
            requestPackage.AddRange(CommunicationHeader);
            // add command header
            requestPackage.AddRange(commandHeader);
            // add command data - 255 bytes with values from 1 to 255
            for (int i = 1; i < 256; i++)
                requestPackage.Add((byte)i);
            // add checksum bytes
            requestPackage.AddRange(GenerateChecksum(requestPackage.ToArray()));

            #endregion

            #region Execute command

            // execute command: send -> wait -> receive -> check response
            retval = ExecuteCommand(requestPackage.ToArray(), waitTime, out responsePackage, desiredResponseLength);

            #endregion

            if (retval == CommunicationResult.Success)
            {
                #region Parse package

                // data of response package should be the same as in the request package (255 bytes with values from 1 to 255)
                for (int i = (CommunicationHeader.Length + commandHeader.Length); i < desiredResponseLength - 2; i++)
                {
                    if (requestPackage[i] != responsePackage[i])
                    {
                        retval = CommunicationResult.ResponseParsingFailed;
                        break;
                    }
                }


                // try get actual controller's ID
                int actualIdIndex = CommunicationHeader.Length - 1;

                if (responsePackage[actualIdIndex] != requestPackage[actualIdIndex]) // MARK-XX controllers send response with their actual ID
                    Communication.CardID = responsePackage[actualIdIndex];
                else // Lumen's controllers send back 255 instead of their own ID
                {
                    // try to get Lumen's controller ID
                    int tmpBaudrateIndex = 0;
                    retval = GetIdAndBaudrate(out tmpBaudrateIndex);
                }

                #endregion
            }

            //if (retval == CommunicationResult.BadChecksum) //...
            //    retval = CommunicationResult.Success;

            return retval;
        }
        #endregion


        #region Sending and operating files

        #region Deleting files and formatting controller and checking free space

        #region GetFreeSpace
        /// <summary>
        /// Gets controller's free space.
        /// </summary>
        /// <param name="freeSpace">Amount of space that is available in controller's memory.</param>
        /// <returns>Result of getting free space operation.</returns>
        public static CommunicationResult GetFreeSpace(out long freeSpace)
        {
            CommunicationResult retval = CommunicationResult.Success;
            byte[] commandHeader = new byte[] { 0x29, 0x01, 0x01 };
            List<byte> requestPackage = new List<byte>();
            int desiredResponseLength = 12;
            int waitTime = 50;
            byte[] responsePackage = new byte[1];
            freeSpace = 0;

            #region Generate request package

            // add communication header
            requestPackage.AddRange(CommunicationHeader);
            // add command header
            requestPackage.AddRange(commandHeader);
            // no command data to add

            // add checksum bytes
            requestPackage.AddRange(GenerateChecksum(requestPackage.ToArray()));

            #endregion

            #region Execute command

            // execute command: send -> wait -> receive -> check response
            retval = ExecuteCommand(requestPackage.ToArray(), waitTime, out responsePackage, desiredResponseLength);
            if (retval != CommunicationResult.Success)
                return retval;

            #endregion

            #region Parse package

            // calculate free space from package
            int dataStart = CommunicationHeader.Length + commandHeader.Length;
            try
            {
                freeSpace = responsePackage[dataStart++]
                            + responsePackage[dataStart++] * 256
                            + responsePackage[dataStart++] * 65536
                            + responsePackage[dataStart++] * 16777216;
            }
            catch
            {
                retval = CommunicationResult.ResponseParsingFailed;
            }
            #endregion

            return retval;
        }
        #endregion

        #region DeleteFile
        /// <summary>
        /// Deletes file(s) by name.
        /// </summary>
        /// <param name="fileName">File name. Use "*.*" to delete all files.</param>
        /// <returns>Result of deleting file(s) operation.</returns>
        public static CommunicationResult DeleteFile(string fileName)
        {
            CommunicationResult retval = CommunicationResult.Success;

            byte[] commandHeader = new byte[] { 0x2C, 0x01 };
            List<byte> requestPackage = new List<byte>();
            int desiredResponseLength = 8;
            int waitTime = 150;
            byte[] responsePackage = new byte[1];

            #region Generate request package

            byte[] nameByteArray = new byte[fileName.Length];

            // add communication header
            requestPackage.AddRange(CommunicationHeader);
            // add command header
            requestPackage.AddRange(commandHeader);
            // add command data - file name bytes
            System.Text.Encoding.ASCII.GetBytes(fileName.ToCharArray(), 0, fileName.Length, nameByteArray, 0);
            requestPackage.AddRange(nameByteArray);
            // add 0 - end of file name
            requestPackage.Add(0);

            // add checksum bytes
            requestPackage.AddRange(GenerateChecksum(requestPackage.ToArray()));

            #endregion

            #region Execute command

            // execute command: send -> wait -> receive -> check response
            retval = ExecuteCommand(requestPackage.ToArray(), waitTime, out responsePackage, desiredResponseLength);
            if (retval != CommunicationResult.Success)
                return retval;

            #endregion

            #region Parse package


            int dataStart = CommunicationHeader.Length + commandHeader.Length;

            if (responsePackage[dataStart] != 1)
                retval = CommunicationResult.DeletingFileFailed;

            #endregion


            return retval;
        }
        
        #endregion
    
        #region Format
        /// <summary>
        /// Formats controller.
        /// </summary>
        /// <returns>Result of controller formatting.</returns>
        public static CommunicationResult Format()
        {
            CommunicationResult retval = CommunicationResult.Success;
            byte[] commandHeader = new byte[] { 0x26, 0x01, 0x01 };
            List<byte> requestPackage = new List<byte>();
            int desiredResponseLength = 8;
            int waitTime = 400;
            byte[] responsePackage = new byte[1];

            #region Generate request package

            // add communication header
            requestPackage.AddRange(CommunicationHeader);
            // add command header
            requestPackage.AddRange(commandHeader);
            // add 0
            requestPackage.Add(0x00);

            // add checksum bytes
            requestPackage.AddRange(GenerateChecksum(requestPackage.ToArray()));

            #endregion

            #region Execute command

            // execute command: send -> wait -> receive -> check response
            retval = ExecuteCommand(requestPackage.ToArray(), waitTime, out responsePackage, desiredResponseLength);
            if (retval != CommunicationResult.Success)
                return retval;

            #endregion

            #region Parse package

            // command header bytes of response package should be the same as in the request package
            for (int i = (CommunicationHeader.Length); i < desiredResponseLength - 2; i++)
            {
                if (requestPackage[i] != responsePackage[i])
                {
                    retval = CommunicationResult.ResponseParsingFailed;
                    break;
                }
            }

            #endregion

            return retval;
        }
        #endregion

        #endregion

        #region Creating files

        #region CreateFile
        /// <summary>
        /// Creates file in controller to enable sending file's content to controller's memory.
        /// </summary>
        /// <param name="sourceName">Path and name of source file.</param>
        /// <param name="targetName">Name of target file.</param>
        /// <param name="fileSize">Size of the file.</param>
        /// <returns>Creating file operation result.</returns>
        public static CommunicationResult CreateFile(string sourceName, string targetName, out int fileSize)
        {
            CommunicationResult retval = CommunicationResult.Success;
            byte[] commandHeader = new byte[] { 0x30, 0x01, 0x77, 0x62 };
            List<byte> requestPackage = new List<byte>();
            int desiredResponseLength = -1;// 263;
            int waitTime = 150;
            byte[] responsePackage = new byte[1];

            #region Generate request package

            System.IO.FileInfo fi = new System.IO.FileInfo(sourceName);
            DateTime modDate = fi.LastWriteTime;
            byte[] nameByteArray = new byte[targetName.Length]; 
            fileSize = (int)fi.Length;

            // add communication header
            requestPackage.AddRange(CommunicationHeader);
            // add command header
            requestPackage.AddRange(commandHeader);
            // add command data: date, 0, filesize, filename, 0

            // add modyfication date
            requestPackage.AddRange(new byte[] { (byte)modDate.Hour, (byte)modDate.Minute, (byte)modDate.Second, (byte)(modDate.Year - 2000), (byte)modDate.Month, (byte)modDate.Day });
            // end of date array
            requestPackage.Add(0);
            // add file size bytes
            requestPackage.AddRange(BitConverter.GetBytes(fileSize));
            // file name
            System.Text.Encoding.ASCII.GetBytes(targetName.ToCharArray(), 0, targetName.Length, nameByteArray, 0);
            requestPackage.AddRange(nameByteArray);
            // end of name char array
            requestPackage.Add(0);

            // add checksum bytes
            requestPackage.AddRange(GenerateChecksum(requestPackage.ToArray()));

            #endregion

            #region Execute command

            // execute command: send -> wait -> receive -> check response
            retval = ExecuteCommand(requestPackage.ToArray(), waitTime, out responsePackage, desiredResponseLength);
            if (retval != CommunicationResult.Success)
                return retval;

            #endregion

            #region Parse package

            int dataStart = CommunicationHeader.Length + 2;
            if (responsePackage[dataStart] != 0x01)
                retval = CommunicationResult.CreatingFileFailed;

            #endregion

            return retval;
        }
        #endregion

        #region SendFileContent
        /// <summary>
        /// Sends file's content to controller's memory.
        /// </summary>
        /// <param name="sourceName">Name of source file.</param>
        /// <param name="pb">ProgressBar object reference to write progress on.</param>
        /// <param name="fileChecksum">Checksum of the file content.</param>
        /// <returns>Sending file's content operation result.</returns>
        public static CommunicationResult SendFileContent(string sourceName, ref System.Windows.Forms.ProgressBar pb, out int fileChecksum)
        {
            CommunicationResult retval = CommunicationResult.Success;
            byte[] commandHeader = new byte[] { 0x32, 0x01 };
            List<byte> requestPackage = new List<byte>();
            int desiredResponseLength = -1;//263;
            int waitTime = 150;
            byte[] responsePackage = new byte[1];
            
            fileChecksum = 0;
            System.IO.FileStream fs = System.IO.File.OpenRead(sourceName);
            int nDatLen = 0;
            int writeBufferSize = 230;
            byte[] writeBuffer = new byte[writeBufferSize];


            // read data from file in loop
            nDatLen = fs.Read(writeBuffer, 0, writeBufferSize);
            while (nDatLen > 0)
            {
                // inform user about sending progress
                MainWindow.WriteProgress(ref pb, nDatLen);

                #region Generate request package

                // add communication header
                requestPackage.AddRange(CommunicationHeader);
                // add command header
                requestPackage.AddRange(commandHeader);
                // add command data: file part size, file part content
                
                // size
                requestPackage.AddRange(BitConverter.GetBytes((Int16)nDatLen));
                // content
                for (int i = 0; i < nDatLen; i++)
                {
                    requestPackage.Add(writeBuffer[i]);
                    fileChecksum += writeBuffer[i];
                }
                
                // add checksum bytes
                requestPackage.AddRange(GenerateChecksum(requestPackage.ToArray()));

                #endregion


                #region Execute command

                // execute command: send -> wait -> receive -> check response
                retval = ExecuteCommand(requestPackage.ToArray(), waitTime, out responsePackage, desiredResponseLength);

                #endregion

                if (retval == CommunicationResult.Success)
                {
                    #region Parse package

                    int dataStart = CommunicationHeader.Length + commandHeader.Length;
                    if (responsePackage[dataStart] != 0x01)
                    {
                        retval = CommunicationResult.WritingFileFailed;
                        break;
                    }

                    #endregion
                }
                else
                {
                    //retval = CommunicationResult.Success;
                    break;
                }

                // prepare to next loop step
                requestPackage.Clear();
                nDatLen = fs.Read(writeBuffer, 0, writeBufferSize);
            }

            // close file
            fs.Close();

            return retval;
        }
        #endregion
        
        #region CloseFile
        /// <summary>
        /// Closes file in controller to complete file transfer.
        /// </summary>
        /// <param name="fileSize">Size of file to be closed.</param>
        /// <param name="fileChecksum">Checksum of file's content.</param>
        /// <returns>Closing file operation result.</returns>
        public static CommunicationResult CloseFile(int fileSize, int fileChecksum)
        {
            CommunicationResult retval = CommunicationResult.Success;
            byte[] commandHeader = new byte[] { 0x33, 0x01 };
            List<byte> requestPackage = new List<byte>();
            int desiredResponseLength = -1;// 263;
            int waitTime = (int)(((fileSize / 4096) + 1) * 200 + 100);
            byte[] responsePackage = new byte[1];

            #region Generate request package

            if (fileChecksum > 0xFFFF)
                fileChecksum = fileChecksum % 0x010000;

            // add communication header
            requestPackage.AddRange(CommunicationHeader);
            // add command header
            requestPackage.AddRange(commandHeader);

            // add file checksum
            requestPackage.AddRange(BitConverter.GetBytes((Int16) fileChecksum));
            // ? - zero
            requestPackage.Add(0);

            // add checksum bytes
            requestPackage.AddRange(GenerateChecksum(requestPackage.ToArray()));

            #endregion

            #region Execute command

            // execute command: send -> wait -> receive -> check response
            retval = ExecuteCommand(requestPackage.ToArray(), waitTime, out responsePackage, desiredResponseLength);
            if (retval != CommunicationResult.Success)
                return retval;

            #endregion

            #region Parse package

            int dataStart = CommunicationHeader.Length + commandHeader.Length;
            if (responsePackage[dataStart] != 0x01)
                retval = CommunicationResult.ClosingFileFailed;

            #endregion

            return retval;
        }
        #endregion
        
        #region UploadFile
        /// <summary>
        /// Uploads specified file to controllers memory - creates file, sends it's content and closes it.
        /// </summary>
        /// <param name="sourceName">Source file path and name.</param>
        /// <param name="targetName">Target file name.</param>
        /// <param name="pb">ProgressBar object reference to write progress on.</param>
        /// <returns>Uploading file result.</returns>
        public static CommunicationResult UploadFile(string sourceName, string targetName, ref System.Windows.Forms.ProgressBar pb)
        {
            CommunicationResult retval = CommunicationResult.Success;
            int fileSize = 0;
            int fileChecksum = 0;

            // create file
            retval = CreateFile(sourceName, targetName, out fileSize); 

            // write file content
            if (retval == CommunicationResult.Success)
                retval = SendFileContent(sourceName, ref pb, out fileChecksum);

            // close file
            if (retval == CommunicationResult.Success)
                retval = CloseFile(fileSize, fileChecksum);

            if (retval != CommunicationResult.Success)
                System.Windows.Forms.MessageBox.Show(retval.ToString());

            return retval;
        }
        #endregion

        #endregion

        #endregion

        #region Restarting controller hardware and application

        #region RestartApp
        /// <summary>
        /// Restarts controller application.
        /// </summary>
        /// <returns>Result of restarting controller application.</returns>
        public static CommunicationResult RestartApp()
        {
            CommunicationResult retval = CommunicationResult.Success;
            byte[] commandHeader = new byte[] { 0xFE, 0x01 };
            List<byte> requestPackage = new List<byte>();
            int desiredResponseLength = -1;//11;
            int waitTime = 100;
            byte[] responsePackage = new byte[1];

            #region Generate request package

            // add communication header
            requestPackage.AddRange(CommunicationHeader);
            // add command header
            requestPackage.AddRange(commandHeader);
            // add command text
            requestPackage.AddRange(new byte[] { 0x41, 0x50, 0x50, 0x21 }); // "APP!"

            // add checksum bytes
            requestPackage.AddRange(GenerateChecksum(requestPackage.ToArray()));

            #endregion

            #region Execute command

            // execute command: send -> wait -> receive -> check response
            retval = ExecuteCommand(requestPackage.ToArray(), waitTime, out responsePackage, desiredResponseLength);
            if (retval != CommunicationResult.Success)
                return retval;

            #endregion

            #region Parse package

            // command header bytes of response package should be the same as in the request package
            int dataStart = CommunicationHeader.Length + commandHeader.Length;
            if (requestPackage[dataStart] == 0x00)
                retval = CommunicationResult.RestartApplicationFailed;
            //if(requestPackage.
            //for (int i = (CommunicationHeader.Length); i < desiredResponseLength - 2; i++)
            //{
            //    if (requestPackage[i] != responsePackage[i])
            //    {
            //        retval = CommunicationResult.ResponseParsingFailed;
            //        break;
            //    }
            //}

            #endregion

            return retval;
        }
        #endregion

        #region RestartHardware
        /// <summary>
        /// Restarts controller.
        /// </summary>
        /// <returns>Result of restarting controller.</returns>
        public static CommunicationResult RestartHardware()
        {
            CommunicationResult retval = CommunicationResult.Success;
            byte[] commandHeader = new byte[] { 0x2D, 0x01 };
            List<byte> requestPackage = new List<byte>();
            int desiredResponseLength = 8;
            int waitTime = 700;
            byte[] responsePackage = new byte[1];

            #region Generate request package

            // add communication header
            requestPackage.AddRange(CommunicationHeader);
            // add command header
            requestPackage.AddRange(commandHeader);
            // add 0
            requestPackage.Add(0x00);

            // add checksum bytes
            requestPackage.AddRange(GenerateChecksum(requestPackage.ToArray()));

            #endregion

            #region Execute command

            // execute command: send -> wait -> receive -> check response
            retval = ExecuteCommand(requestPackage.ToArray(), waitTime, out responsePackage, desiredResponseLength);
            if (retval != CommunicationResult.Success)
                return retval;

            #endregion

            #region Parse package

            // one byte of response should be set to 1
            int dataStart = CommunicationHeader.Length + commandHeader.Length;

            if (responsePackage[dataStart] != 1)
                retval = CommunicationResult.RestartHardwareFailed;
            
            #endregion

            return retval;
        }
        #endregion

        #region EndSendingFiles
        /// <summary>
        /// 
        /// </summary>
        /// <returns>Result of ending transmission of files.</returns>
        public static CommunicationResult EndSendingFiles()
        {
            CommunicationResult retval = CommunicationResult.Success;
            byte[] commandHeader = new byte[] { 0x2D, 0x01 };
            List<byte> requestPackage = new List<byte>();
            int desiredResponseLength = 8;
            int waitTime = 700;
            byte[] responsePackage = new byte[1];

            #region Generate request package

            // add communication header
            requestPackage.AddRange(CommunicationHeader);
            // add command header
            requestPackage.AddRange(commandHeader);
            // add 1
            requestPackage.Add(0x01);

            // add checksum bytes
            requestPackage.AddRange(GenerateChecksum(requestPackage.ToArray()));

            #endregion

            #region Execute command

            // execute command: send -> wait -> receive -> check response
            retval = ExecuteCommand(requestPackage.ToArray(), waitTime, out responsePackage, desiredResponseLength);
            if (retval != CommunicationResult.Success)
                return retval;

            #endregion

            #region Parse package

            // one byte of response should be set to 1
            int dataStart = CommunicationHeader.Length + commandHeader.Length;

            if (responsePackage[dataStart] != 1)
                retval = CommunicationResult.RestartHardwareFailed;

            #endregion

            return retval;
        }
        #endregion

        #endregion

        #region Controller configuration

        #region ID and RS232 baudrate

        #region GetIdAndBaudrate
        public static CommunicationResult GetIdAndBaudrate(out int baudrateIndex)
        { 
            CommunicationResult retval = CommunicationResult.Success;

            byte[] commandHeader = new byte[] { 0x3E, 0x01, 0x01 };
            List<byte> requestPackage = new List<byte>();
            int desiredResponseLength = 10;
            int waitTime = 150;
            byte[] responsePackage = new byte[1];

            baudrateIndex = 0;

            #region Generate request package

            // add communication header
            requestPackage.AddRange(CommunicationHeader);
            // add command header
            requestPackage.AddRange(commandHeader);

            // add command data - 0x00 for ID and 0xFF for baudrate
            requestPackage.AddRange(new byte[] { 0x00, 0xFF });

            // add checksum bytes
            requestPackage.AddRange(GenerateChecksum(requestPackage.ToArray()));

            #endregion

            #region Execute command

            // execute command: send -> wait -> receive -> check response
            retval = ExecuteCommand(requestPackage.ToArray(), waitTime, out responsePackage, desiredResponseLength);
            if (retval != CommunicationResult.Success)
                return retval;

            #endregion

            #region Parse package

            int dataStart = CommunicationHeader.Length + commandHeader.Length;

            if (responsePackage[dataStart - 1] != 1)
                retval = CommunicationResult.IdGettingFailed;
            else
            {
                Communication.CardID = responsePackage[dataStart++];
                baudrateIndex = responsePackage[dataStart];
            }

            #endregion


            return retval;
        }
        #endregion

        #region SetIdAndBaudrate
        public static CommunicationResult SetIdAndBaudrate(int newID, int baudrateIndex)
        {
            CommunicationResult retval = CommunicationResult.Success;

            byte[] commandHeader = new byte[] { 0x3E, 0x01, 0x00 };
            List<byte> requestPackage = new List<byte>();
            int desiredResponseLength = 10;
            int waitTime = 150;
            byte[] responsePackage = new byte[1];

            //SerialCommunication.BaudrateToChange = baudrateIndex;
            SerialCommunication.BaudrateToChange = baudrateIndex;
            //SerialCommunication.CurrentSerialPort.Baudrate = SerialCommunication.BaudrateToChange;
            //SerialCommunication.ControllerSerialPort.BaudRate = SerialCommunication.CurrentSerialPort.Baudrate;
            

            #region Generate request package

            // add communication header
            requestPackage.AddRange(CommunicationHeader);
            // add command header
            requestPackage.AddRange(commandHeader);

            // add command data - new ID and baudrate
            requestPackage.AddRange(new byte[] { (byte)newID, (byte)baudrateIndex });

            // add checksum bytes
            requestPackage.AddRange(GenerateChecksum(requestPackage.ToArray()));

            #endregion

            #region Execute command

            // execute command: send -> wait -> receive -> check response
            retval = ExecuteCommand(requestPackage.ToArray(), waitTime, out responsePackage, desiredResponseLength);
            if (retval != CommunicationResult.Success)
            {
                Communication.CardID = newID;
                return retval;
            }

            #endregion

            #region Parse package

            Communication.CardID = newID;

            int dataStart = CommunicationHeader.Length + commandHeader.Length;

            if (responsePackage[dataStart - 1] != 1)
                retval = CommunicationResult.IdSettingFailed;
            else
            {
                Communication.CardID = responsePackage[dataStart++];
                if (baudrateIndex != responsePackage[dataStart])
                    retval = CommunicationResult.IdSettingFailed;
            }

            #endregion


            return retval;
        }
        #endregion

        #endregion

        #region Time

        #region GetTime
        /// <summary>
        /// Gets actual controller time.
        /// </summary>
        /// <param name="controllerDate">DateTime variable to put controller's time in.</param>
        /// <returns>Result of getting time operation.</returns>
        public static CommunicationResult GetTime(out DateTime controllerDate)
        {
            CommunicationResult retval = CommunicationResult.Success;

            byte[] commandHeader = new byte[] { 0x47, 0x01, 0x01 };
            List<byte> requestPackage = new List<byte>();
            int desiredResponseLength = -1;// 16;
            int waitTime = 100;
            byte[] responsePackage = new byte[1];
            controllerDate = DateTime.Now;

            #region Generate request package

            // add communication header
            requestPackage.AddRange(CommunicationHeader);
            // add command header
            requestPackage.AddRange(commandHeader);
            // no more command data for this command

            // add checksum bytes
            requestPackage.AddRange(GenerateChecksum(requestPackage.ToArray()));

            #endregion

            #region Execute command

            // execute command: send -> wait -> receive -> check response
            retval = ExecuteCommand(requestPackage.ToArray(), waitTime, out responsePackage, desiredResponseLength);
            if (retval != CommunicationResult.Success)
                return retval;

            #endregion

            #region Parse package

            // set values of DateTime with proper bytes
            int dateIndex = (CommunicationHeader.Length + commandHeader.Length);


            try
            {
                controllerDate = new DateTime(2000 + responsePackage[dateIndex + 6], responsePackage[dateIndex + 5], responsePackage[dateIndex + 4],
                    responsePackage[dateIndex + 2], responsePackage[dateIndex + 1], responsePackage[dateIndex + 0]);
            }
            catch
            {
                controllerDate = DateTime.Now;
                retval = CommunicationResult.TimeGettingFailed;
            }

            #endregion


            return retval;
        }
        #endregion

        #region SetTime
        /// <summary>
        /// Sets new time to the controller.
        /// </summary>
        /// <param name="dateToSet">Date and time to set to the controller.</param>
        /// <returns>Result of setting time operation.</returns>
        public static CommunicationResult SetTime(DateTime dateToSet)
        {
            CommunicationResult retval = CommunicationResult.Success;
            byte[] commandHeader = new byte[] { 0x47, 0x01, 0x00 };
            List<byte> requestPackage = new List<byte>();
            int desiredResponseLength = -1;//10;
            int waitTime = 100;
            byte[] responsePackage = new byte[1];

            #region Generate request package

            // add communication header
            requestPackage.AddRange(CommunicationHeader);
            // add command header
            requestPackage.AddRange(commandHeader);
            // add command data - 255 bytes with values from 1 to 255
            requestPackage.Add((byte)dateToSet.Second);
            requestPackage.Add((byte)dateToSet.Minute);
            requestPackage.Add((byte)dateToSet.Hour);
            requestPackage.Add((byte)dateToSet.DayOfWeek);
            requestPackage.Add((byte)dateToSet.Day);
            requestPackage.Add((byte)dateToSet.Month);
            requestPackage.Add((byte)(dateToSet.Year - 2000));
            // add checksum bytes
            requestPackage.AddRange(GenerateChecksum(requestPackage.ToArray()));

            #endregion

            #region Execute command

            // execute command: send -> wait -> receive -> check response
            retval = ExecuteCommand(requestPackage.ToArray(), waitTime, out responsePackage, desiredResponseLength);
            if (retval != CommunicationResult.Success)
                return retval;

            #endregion

            #region Parse package

            // data bytes from response package should be the same as request bytes at the same index
            //for (int i = (CommunicationHeader.Length + commandHeader.Length); i < desiredResponseLength - 2; i++)
            for (int i = (CommunicationHeader.Length + commandHeader.Length); i < responsePackage.Length - 2; i++)
            {
                if (requestPackage[i] != responsePackage[i])
                {
                    retval = CommunicationResult.TimeSettingFailed;
                    break;
                }
            }

            #endregion

            return retval;
        }
        #endregion

        #endregion

        #region Brightness

        #region GetBrightness
        /// <summary>
        /// Gets controller's brightness.
        /// </summary>
        /// <param name="brightnessValues">Array to put brightness values in.</param>
        /// <returns>Getting brightness result.</returns>
        public static CommunicationResult GetBrightness(out byte[] brightnessValues)
        {
            CommunicationResult retval = CommunicationResult.Success;
            byte[] commandHeader = new byte[] { 0x46, 0x01, 0x01 };
            List<byte> requestPackage = new List<byte>();
            int desiredResponseLength = 32;
            int waitTime = 100;
            byte[] responsePackage = new byte[1];
            brightnessValues = new byte[6];

            #region Generate request package

            // add communication header
            requestPackage.AddRange(CommunicationHeader);
            // add command header
            requestPackage.AddRange(commandHeader);
            // no data

            // add checksum bytes
            requestPackage.AddRange(GenerateChecksum(requestPackage.ToArray()));

            #endregion

            #region Execute command

            // execute command: send -> wait -> receive -> check response
            retval = ExecuteCommand(requestPackage.ToArray(), waitTime, out responsePackage, desiredResponseLength);

            #endregion


            if (retval == CommunicationResult.Success)
            {
                #region Parse package

                // get values of brightness
                int dataStart = (CommunicationHeader.Length + commandHeader.Length);
                // 0-4
                brightnessValues[0] = responsePackage[dataStart];
                dataStart += 4;
                // 4-8
                brightnessValues[1] = responsePackage[dataStart];
                dataStart += 4;
                // 8-12
                brightnessValues[2] = responsePackage[dataStart];
                dataStart += 4;
                // 12-16
                brightnessValues[3] = responsePackage[dataStart];
                dataStart += 4;
                // 16-20
                brightnessValues[4] = responsePackage[dataStart];
                dataStart += 4;
                // 20-24
                brightnessValues[5] = responsePackage[dataStart];
                dataStart += 4;

                #endregion
            }

            return retval;
        }
        #endregion

        #region SetBrightness
        /// <summary>
        /// Sets controller's brightness.
        /// </summary>
        /// <param name="brightnessValues">Array of six brightness values.</param>
        /// <returns>Setting brightness result.</returns>
        public static CommunicationResult SetBrightness(byte[] brightnessValues)
        {
            CommunicationResult retval = CommunicationResult.Success;
            byte[] commandHeader = new byte[] { 0x46, 0x01, 0x00 };
            List<byte> requestPackage = new List<byte>();
            int desiredResponseLength = 14;
            int waitTime = 100;
            byte[] responsePackage = new byte[1];

            #region Generate request package

            // add communication header
            requestPackage.AddRange(CommunicationHeader);
            // add command header
            requestPackage.AddRange(commandHeader);
            // add command data - 4 bytes for each value from brighnessValues array
            for (int i = 0; i < 24; i++)
                requestPackage.Add(brightnessValues[i / 4]);
            // add checksum bytes
            requestPackage.AddRange(GenerateChecksum(requestPackage.ToArray()));

            #endregion

            #region Execute command

            // execute command: send -> wait -> receive -> check response
            retval = ExecuteCommand(requestPackage.ToArray(), waitTime, out responsePackage, desiredResponseLength);
            if (retval != CommunicationResult.Success)
                return retval;

            #endregion

            #region Parse package

            // data of response package should be the same as in first 6 bytes of request package data 
            for (int i = (CommunicationHeader.Length + commandHeader.Length); i < desiredResponseLength - 2; i++)
            {
                if (requestPackage[i] != responsePackage[i])
                {
                    retval = CommunicationResult.BrightnessSettingFailed;
                    break;
                }
            }

            #endregion

            return retval;
        }
        #endregion

        #endregion


        #region Version
        public static CommunicationResult GetVersion(out string version)
        {
            CommunicationResult retval = CommunicationResult.Success;
            byte[] commandHeader = new byte[] { 0x2E, 0x01, 0x00 };
            List<byte> requestPackage = new List<byte>();
            int desiredResponseLength = -1;// 263;
            int waitTime = 100;
            byte[] responsePackage = new byte[1];

            bool tmp = false;

            version = "---";

            #region Generate request package

            // add communication header
            requestPackage.AddRange(CommunicationHeader);
            // add command header
            requestPackage.AddRange(commandHeader);
            // no data

            // add checksum bytes
            requestPackage.AddRange(GenerateChecksum(requestPackage.ToArray()));

            #endregion

            #region Execute command

            // execute command: send -> wait -> receive -> check response
            retval = ExecuteCommand(requestPackage.ToArray(), waitTime, out responsePackage, desiredResponseLength);

            #endregion

            if (retval == CommunicationResult.Success)
            {
                if (tmp)
                {
                    responsePackage = new byte[]{ 0xE8, 0x32, 0x01, 0x2E, 0x01, 0x01, 0x4D, 192, 0x15, 0x08, 0x0B, 0x00, 0x08, 0x04
                                , 0x61, 0x02};
                }
                
                #region Parse package

                int dataStart = (CommunicationHeader.Length + commandHeader.Length);

                if (responsePackage[dataStart - 1] != 1)
                    retval = CommunicationResult.VersionGettingFailed;
                else
                { 
                    // check if controller is from MARK-XX series
                    if ((ControllerType)responsePackage[dataStart] == ControllerType.MARK_XX)
                    {
                        //model = (ControllerType)responsePackage[++dataStart];
                        MainWindow.controllerSettings.Series = (ControllerType)responsePackage[dataStart];
                        MainWindow.controllerSettings.Type = (ControllerType)responsePackage[++dataStart];

                        version = string.Format("{0}-{1}-{2} {3}.{4}", responsePackage[dataStart + 1], responsePackage[dataStart + 2],
                            responsePackage[dataStart + 3] + 2000, responsePackage[dataStart + 6], responsePackage[dataStart + 5]);

                    }
                    else
                    {
                        MainWindow.controllerSettings.Series = ControllerType.C_PowerX200;
                        MainWindow.controllerSettings.Type = (ControllerType)responsePackage[dataStart];
                        version = string.Format("{0}.{1} - {2}.{3} - {4}.{5}", responsePackage[dataStart+3], responsePackage[dataStart+2],
                            responsePackage[dataStart+1] / 16, responsePackage[dataStart+1] % 16, responsePackage[dataStart+7], responsePackage[dataStart+6]);
                    }
                }
            
                #endregion
            }


            return retval;
        }
        #endregion

        #region IP

        #region GetIPConfiguration
        public static CommunicationResult GetIPConfiguration(out System.Net.IPAddress[] ipAdresses, out int ipPort)
        {
            CommunicationResult retval = CommunicationResult.Success;
            byte[] commandHeader = new byte[] { 0x3C, 0x01, 0x01 };
            List<byte> requestPackage = new List<byte>();
            int desiredResponseLength = -1;//263;
            int waitTime = 100;
            byte[] responsePackage = new byte[1];
            ipAdresses = new System.Net.IPAddress[4];
            ipPort = 0;

            #region Generate request package

            // add communication header
            requestPackage.AddRange(CommunicationHeader);
            // add command header
            requestPackage.AddRange(commandHeader);
            // no data

            // add checksum bytes
            requestPackage.AddRange(GenerateChecksum(requestPackage.ToArray()));

            #endregion

            #region Execute command

            // execute command: send -> wait -> receive -> check response
            retval = ExecuteCommand(requestPackage.ToArray(), waitTime, out responsePackage, desiredResponseLength);

            #endregion

            if (retval == CommunicationResult.Success)
            {
                #region Parse package

                int dataStart = (CommunicationHeader.Length + commandHeader.Length);
                // controller ip address
                ipAdresses[0] = new System.Net.IPAddress(new byte[] { responsePackage[dataStart++], responsePackage[dataStart++], responsePackage[dataStart++], responsePackage[dataStart++] });
                // gateway
                ipAdresses[1] = new System.Net.IPAddress(new byte[] { responsePackage[dataStart++], responsePackage[dataStart++], responsePackage[dataStart++], responsePackage[dataStart++] });
                // subnet mask
                ipAdresses[2] = new System.Net.IPAddress(new byte[] { responsePackage[dataStart++], responsePackage[dataStart++], responsePackage[dataStart++], responsePackage[dataStart++] });
                // controller ip port
                ipPort = responsePackage[dataStart++] * 256 + responsePackage[dataStart++];
                //BitConverter.ToInt32(new byte[] { responsePackage[dataStart++], responsePackage[dataStart++] }, 0);
                // passcode
                ipAdresses[3] = new System.Net.IPAddress(new byte[] { responsePackage[dataStart++], responsePackage[dataStart++], responsePackage[dataStart++], responsePackage[dataStart++] });

                #endregion
            }

            return retval;
        }
        #endregion

        #region SetIPConfiguration
        public static CommunicationResult SetIPConfiguration(System.Net.IPAddress[] ipAdresses, int ipPort)
        {
            CommunicationResult retval = CommunicationResult.Success;
            byte[] commandHeader = new byte[] { 0x3C, 0x01, 0x00 };
            List<byte> requestPackage = new List<byte>();
            int desiredResponseLength = -1;//263;
            int waitTime = 100;
            byte[] responsePackage = new byte[1];

            #region Generate request package

            // add communication header
            requestPackage.AddRange(CommunicationHeader);
            // add command header
            requestPackage.AddRange(commandHeader);

            // controller ip
            requestPackage.AddRange(ipAdresses[0].GetAddressBytes());
            // gateway
            requestPackage.AddRange(ipAdresses[1].GetAddressBytes());
            // subnet mask
            requestPackage.AddRange(ipAdresses[2].GetAddressBytes());
            // controller port
            requestPackage.Add(BitConverter.GetBytes((Int16)ipPort)[1]);
            requestPackage.Add(BitConverter.GetBytes((Int16)ipPort)[0]);
            // passcode
            requestPackage.AddRange(ipAdresses[3].GetAddressBytes());

            // add checksum bytes
            requestPackage.AddRange(GenerateChecksum(requestPackage.ToArray()));

            #endregion

            #region Execute command

            // execute command: send -> wait -> receive -> check response
            retval = ExecuteCommand(requestPackage.ToArray(), waitTime, out responsePackage, desiredResponseLength);

            #endregion

            if (retval == CommunicationResult.Success)
            {
                #region Parse package

                int dataStart = (CommunicationHeader.Length + commandHeader.Length) - 1;

                if (responsePackage[dataStart] != 1)
                    retval = CommunicationResult.IPSettingFailed;

                #endregion
            }

            return retval;
        }

        #endregion

        #endregion

        #region Lumen screen configuration


        public static CommunicationResult SetScreenConfig(byte[] configBytes)
        {
            CommunicationResult retval = CommunicationResult.Success;
            byte[] commandHeader = new byte[] { 0x3A, 0x01};
            List<byte> requestPackage = new List<byte>();
            int desiredResponseLength = -1;// 32;
            int waitTime = 100;
            byte[] responsePackage = new byte[1];

            #region Generate request package

            // add communication header
            requestPackage.AddRange(CommunicationHeader);
            // add command header
            requestPackage.AddRange(commandHeader);
            // add command data

            if (MainWindow.controllerSettings.Type == ControllerType.C_Power1200 || MainWindow.controllerSettings.Type == ControllerType.C_Power2200 || MainWindow.controllerSettings.Type == ControllerType.C_Power3200)
            {
                #region 1200/2200/3200
                requestPackage.AddRange(new byte[]
                {
                    0x1F, 0x02, 0xFF, 0x00, 0x01, 0x01, 0x01, 0x01, 0x01, 0x02,
                    0x01, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x81, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00
                });

                for (int i = 0; i < 11; i++)
                    requestPackage[i + 7] = configBytes[i];

                requestPackage[19] = configBytes[11];
                requestPackage[24] = configBytes[12];
                #endregion
            }
            else if (MainWindow.controllerSettings.Type == ControllerType.C_Power4200 || MainWindow.controllerSettings.Type == ControllerType.C_Power5200)
            {
                #region 4200/5200

                int dataStart = requestPackage.Count;

                requestPackage.AddRange(new byte[] {
                    0xEF, 0x00, 0x02, 0x08, 0x00, 0x01, 0x10, 0x00, 0x01, 
                    0x00, 0x01, 0x01, 0x01, 0x00, 0x00, 0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 
                    0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x8A, 0x01, 0x67, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                    0x00, 0x00 });

                
                // hide scan 
                requestPackage[dataStart] = configBytes[0];
                // column order, data polarity, OE polarity, line adjust, color order
                requestPackage[dataStart+1] = configBytes[1];
                // scan mode, line change space, signal reverse 
                requestPackage[dataStart+2] = configBytes[2];
                // module size, line reverse, line change direction
                requestPackage[dataStart+3] = configBytes[3];
                // clock mode, timing trimming, pulse trimming
                requestPackage[dataStart+8] = configBytes[4];
                // output board
                requestPackage[dataStart+9] = configBytes[5];
                #endregion
            }

            // add checksum bytes
            requestPackage.AddRange(GenerateChecksum(requestPackage.ToArray()));

            #endregion

            #region Execute command

            // execute command: send -> wait -> receive -> check response
            retval = ExecuteCommand(requestPackage.ToArray(), waitTime, out responsePackage, desiredResponseLength);

            #endregion


            if (retval == CommunicationResult.Success)
            {
                #region Parse package

                // get values of brightness
                int dataStart = (CommunicationHeader.Length + commandHeader.Length);

                if (responsePackage[dataStart] != 1)
                    retval = CommunicationResult.ConfigurationSettingFailed;

                #endregion
            }

            return retval;
        }

        public static CommunicationResult GetCpowerConfig(out byte[] configBytes)
        {
            CommunicationResult retval = CommunicationResult.Success;
            byte[] commandHeader = new byte[] { 0x3B, 0x01, 0x03 };
            List<byte> requestPackage = new List<byte>();
            int desiredResponseLength = -1;// 32;
            int waitTime = 100;
            byte[] responsePackage = new byte[1];

            configBytes = new byte[16];
            
            #region Generate request package

            // add communication header
            requestPackage.AddRange(CommunicationHeader);
            // add command header
            requestPackage.AddRange(commandHeader);
            // no data

            // add checksum bytes
            requestPackage.AddRange(GenerateChecksum(requestPackage.ToArray()));

            #endregion

            #region Execute command

            // execute command: send -> wait -> receive -> check response
            retval = ExecuteCommand(requestPackage.ToArray(), waitTime, out responsePackage, desiredResponseLength);

            #endregion


            if (retval == CommunicationResult.Success)
            {
                #region Parse package

                int dataStart = (CommunicationHeader.Length + commandHeader.Length);

                if (responsePackage[dataStart - 1] == 1)
                {

                    if (MainWindow.controllerSettings.Type == ControllerType.C_Power1200 || MainWindow.controllerSettings.Type == ControllerType.C_Power2200 || MainWindow.controllerSettings.Type == ControllerType.C_Power3200)
                    {
                        #region 1200/2200/3200
                        for (int i = 0; i < 11; i++)
                            configBytes[i] = responsePackage[i + dataStart + 2];

                        configBytes[12] = responsePackage[20];
                        configBytes[13] = responsePackage[25];
                        #endregion
                    }
                    else if (MainWindow.controllerSettings.Type == ControllerType.C_Power4200 || MainWindow.controllerSettings.Type == ControllerType.C_Power5200)
                    {
                        #region 4200/5200
                        configBytes = new byte[6];

                        // hide scan 
                        configBytes[0] = responsePackage[dataStart];
                        // column order, data polarity, OE polarity, line adjust, color order
                        configBytes[1] = responsePackage[dataStart+1];
                        // scan mode, line change space, signal reverse 
                        configBytes[2] = responsePackage[dataStart+2];
                        // module size, line reverse, line change direction
                        configBytes[3] = responsePackage[dataStart+3];
                        // clock mode, timing trimming, pulse trimming
                        configBytes[4] = responsePackage[dataStart+8];
                        // output board
                        configBytes[5] = responsePackage[dataStart+9];
                        #endregion
                    }
                }
                else
                    retval = CommunicationResult.ConfigurationGettingFailed;

                #endregion
            }

            return retval;
        }

        #endregion
        
        #region Methods for MARK-XX controllers

        #region GetMarkConfig
        public static CommunicationResult GetMarkConfig(bool testOffline, out int autoscan, out int[] configListsValues,
            out  int[] configMaxValues, out int[] configHeightMaxValues, out int[][] configWidthMaxValues, out byte[] oldValues)
        {
            CommunicationResult retval = CommunicationResult.Success;
            byte[] commandHeader = new byte[] { 0x3B, 0x01, 0x00 };
            List<byte> requestPackage = new List<byte>();
            int desiredResponseLength = -1;//26;
            int waitTime = 700;
            byte[] responsePackage = new byte[1];

            autoscan = 0;
            configListsValues = new int[12];
            configMaxValues = new int[14];
            configWidthMaxValues = new int[2][];
            configHeightMaxValues = new int[2];
            List<int> maxWidthValuesList = new List<int>();
            oldValues = new byte[20];

            if (!testOffline)
            {
                #region Generate request package

                // add communication header
                requestPackage.AddRange(CommunicationHeader);
                // add command header
                requestPackage.AddRange(commandHeader);
                // no more command data for this command

                // add checksum bytes
                requestPackage.AddRange(GenerateChecksum(requestPackage.ToArray()));

                #endregion

                #region Execute command
                // execute command: send -> wait -> receive -> check response
                retval = ExecuteCommand(requestPackage.ToArray(), waitTime, out responsePackage, desiredResponseLength);
                if (retval != CommunicationResult.Success)
                    return retval;

                #endregion
            }
            else
            {
                
                    responsePackage = new byte[]{

                        0xE8, 0x32, 0x01, 0x3B, 0x01, 0x01, 0x01, 0x07, 0xFF, 0x01, 0x01, 0x00, 0x00, 0x00, 0x02
, 0x00, 0x00, 0x01, 0x01, 0x01, 0x00, 0x20, 0x10, 0x01, 0xFF, 0x00, 0x0C, 0x03, 0x02, 0x02, 0x00
, 0x00, 0x00, 0x08, 0x78, 0x3C, 0x30, 0x20, 0x10, 0xA0, 0x50, 0x40, 0x38, 0x2C, 0x24, 0x7D, 0x06


//                        0xE8, 0x32, 0x01, 0x3B, 0x01, 0x01, 
//                        0x01, 0xFF, 0x01, 
//                    0xFF, 0x01, 0x00, 0x00, 0x00, 0x07, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 
//0x20, 0x10, 0x01, 0xFF, 0x00, 
//// granice
//0x0C, 0x03, 0x02, 0x02, 0x00, 
//0x04, // WP
//0x06, // WM

//0x08, 0x78, 0x3C, 0x30, 0x20, 
//0x10, 0xA0, 0x50, 0x40, 0x38, 0x2C, 0x24, 0x20, 0x1C, 0x00, 0x00, 0x00, 0x00, 
//54,21,64,1,0,13,
//0xB5, 0x07
                //        0xE8, 0x32, 0x01, 0x3B, 0x01, 0x01, 0x01, 0xFF, 0x01, 
                //    0x01, 0x01, 0x55, 0x01, 0x00, 0x01, 
                //0x00, 0x00, 0x01, 0x01, 0xFF, 0x00, 0x20, 0x10, 0x01, 0xFF, 0x00, 
                
                //5,3,2,3,0,  8,  48,24,16,12//,  0, 0, 0, 0 
                //,
                //16,  96,48,32,24, 16,16,12,12
                
                //, 0xBA, 0x05
                    };

//                0xE8, 0x32, 0x01, 0x3B, 0x01, 0x01, 0x01, 0xFF, 0x01, 
//                0xFF, 0x00, 0x00, 0x02, 0x00, 0x07, 0x00, 0xFF, 0x01, 0x00, 0x00, 0x00, 0x20, 0x08, 0x01, 0xFF, 0x00, 0x0C, 0x03, 0x02, 0x02, 0x00
//, 0x08, 0x78, 0x3C, 0x30, 0x20, 0x00, 0x00, 0x00, 0x00, 0x10, 0xA0, 0x50, 0x40, 0x38, 0x2C, 0xF8, 0x00, 0x8A
//                , 0xCE, 0x09

                //responsePackage = new byte[]{0xE8, 0x32, 0x01, 0x3B, 0x01, 0x01, 0x01, 0xFF, 0x01, 0x01, 0x01, 0x00, 0x00, 0x00, 0x03, 
                //0x00, 0x01, 0x01, 0x01, 0x01, 0x00, 0x20, 0x10, 0x01, 0xFF, 0x00, 0x08, 0x03, 0x02, 0x03, 0x02, 
                //0x10, 0xA0, 0x50, 0x40, 0x38, 0x2C, 0x24, 0x20, 0x1C, 0x08, 0x03, 0x02, 0x03, 0x02, 0xBA, 0x05};

                //{0xE8, 0x32, 0x01, 0x3B, 0x01, 0x1F, 0x01, 0xFF, 0x01, 0x01, 0x00, 0x00, 0x00, 0x03,
                //    0x00, 0x01, 0x01, 0x01, 0x01, 0x00, 0x10, 0x0C, 0x01, 0x00, 0x00, 0x08, 0x03, 0x02, 0x02, 0x02,
                //    0x10, 0xA0, 0x50, 0x40, 0x38, 0x2C, 0x24, 0x20, 0x1C, 0x08, 0x03, 0x02, 0x02, 0x02, 0xC5, 0x04};
                // set index to start looking for data at
            }

            #region Parse package
            try
            {

                int dataStart = (CommunicationHeader.Length + commandHeader.Length);// + 1);// +3;

                for (int i = 0; i < 20; i++)
                    oldValues[i] = responsePackage[dataStart + i];

                //oldValues[0] = responsePackage[dataStart++]; // reserved
                //oldValues[1] = responsePackage[dataStart++]; // reserved
                //oldValues[2] = responsePackage[dataStart++]; // reserved
                dataStart++;
                dataStart++;
                dataStart++;

                configListsValues[8] = responsePackage[dataStart++];// K  // cloning
                configListsValues[4] = responsePackage[dataStart++];// E  // check compatibility
                autoscan = responsePackage[dataStart++]; // autoscan
                configListsValues[7] = responsePackage[dataStart++];// H  // quality

                //oldValues[3] = responsePackage[dataStart++]; // reserved
                dataStart++;

                configListsValues[6] = responsePackage[dataStart++];// G  // mode blanking
                configListsValues[3] = responsePackage[dataStart++];// D  // scan mode
                configListsValues[9] = responsePackage[dataStart++];// L 
                configListsValues[5] = responsePackage[dataStart++];// F  // skip headers
                configListsValues[0] = responsePackage[dataStart++]; // A // color
                configListsValues[11] = responsePackage[dataStart++];// P

                //oldValues[4] = responsePackage[dataStart++]; // reserved
                dataStart++;

                configListsValues[1] = (responsePackage[dataStart++] / 8); // B // height
                configListsValues[2] = (responsePackage[dataStart++]);// C  // width

                //oldValues[5] = responsePackage[dataStart++]; // reserved
                //oldValues[6] = responsePackage[dataStart++]; // reserved
                dataStart++;
                dataStart++;

                configListsValues[10] = responsePackage[dataStart++];// M
                

                configMaxValues[0] = responsePackage[dataStart++]; // WG
                configMaxValues[1] = responsePackage[dataStart++]; // WH
                configMaxValues[2] = responsePackage[dataStart++]; // WA
                configMaxValues[3] = responsePackage[dataStart++]; // WL
                configMaxValues[4] = responsePackage[dataStart++]; // WK
                configMaxValues[5] = responsePackage[dataStart++]; // WP
                configMaxValues[6] = responsePackage[dataStart++]; // WM
                
                //configMaxValues[5] = responsePackage[dataStart++]; // WB

                maxWidthValuesList.Clear();
                configHeightMaxValues[0] = responsePackage[dataStart++]; // WB COLOR
                int colorHeightCount = configHeightMaxValues[0] / 2;
                while (maxWidthValuesList.Count < colorHeightCount) // WC COLOR
                {
                    if (maxWidthValuesList.Count > 0 && maxWidthValuesList[maxWidthValuesList.Count - 1] < responsePackage[dataStart])
                    {
                        retval = CommunicationResult.BadDataTryAgain;
                        break;
                    }
                    else
                        maxWidthValuesList.Add(responsePackage[dataStart++]); // WC COLOR
                }

                configWidthMaxValues[0] = maxWidthValuesList.ToArray(); // WC COLOR

                maxWidthValuesList.Clear();
                configHeightMaxValues[1] = responsePackage[dataStart++]; // WB MONO
                int monoHeightCount = configHeightMaxValues[1] / 2;
                while (maxWidthValuesList.Count < monoHeightCount && dataStart < responsePackage.Length - 2) // WC MONO
                {
                    if (maxWidthValuesList.Count > 0 && maxWidthValuesList[maxWidthValuesList.Count - 1] < responsePackage[dataStart])
                    {
                        retval = CommunicationResult.BadDataTryAgain;
                        break;
                    }
                    else
                        maxWidthValuesList.Add(responsePackage[dataStart++]); // WC MONO
                }

                configWidthMaxValues[1] = maxWidthValuesList.ToArray(); // WC MONO

                //configMaxValues[6] = responsePackage[dataStart++];///16; // WX - W16
                //configMaxValues[7] = responsePackage[dataStart++];///16; // WX - W32
                //configMaxValues[8] = responsePackage[dataStart++];///16; // WX - W48
                //configMaxValues[9] = responsePackage[dataStart++];///16; // WX - W64
                //configMaxValues[10] = responsePackage[dataStart++];///16; // WX - W80
                //configMaxValues[11] = responsePackage[dataStart++];///16; // WX - W96
                //configMaxValues[12] = responsePackage[dataStart++];///16; // WX - W112
                //configMaxValues[13] = responsePackage[dataStart++];///16; // WX - W128
            }
            catch
            {
                retval = CommunicationResult.ResponseParsingFailed;
            }

            #endregion

            return retval;
        }

        #endregion

        #region SetMarkConfig
        public static CommunicationResult SetMarkConfig(bool autoscan, int[] configListsValues, byte[] oldValues)
        {
            CommunicationResult retval = CommunicationResult.Success;
            byte[] commandHeader = new byte[] { 0x3A, 0x01 };
            List<byte> requestPackage = new List<byte>();
            int desiredResponseLength = -1;
            int waitTime = 700;
            byte[] responsePackage = new byte[1];

            autoscan = false;

            #region Generate request package

            // add communication header
            requestPackage.AddRange(CommunicationHeader);
            // add command header
            requestPackage.AddRange(commandHeader);
            // add command data
            //requestPackage.AddRange(new byte[]{ 0x01, 0xFF, 0x01}); // reserved values


            requestPackage.Add(oldValues[0]); // reserved
            requestPackage.Add(oldValues[1]); // reserved
            requestPackage.Add(oldValues[2]); // reserved


            requestPackage.Add((byte)((configListsValues[8] != -1) ? configListsValues[8] : oldValues[3])); // K
            requestPackage.Add((byte)((configListsValues[4] != -1) ? configListsValues[4] : oldValues[4])); // E
            requestPackage.Add(0); //oldValues[5]); // autoscan            
            requestPackage.Add((byte)((configListsValues[7] != -1) ? configListsValues[7] : oldValues[6])); // H

            //requestPackage.Add((byte)configListsValues[8]); // K
            //requestPackage.Add((byte)configListsValues[4]); // E
            //requestPackage.Add(Convert.ToByte(autoscan)); // autoscan
            //requestPackage.Add((byte)configListsValues[7]); // H

            requestPackage.Add(oldValues[7]); // reserved

            requestPackage.Add((byte)((configListsValues[6] != -1) ? configListsValues[6] : oldValues[8])); // G
            requestPackage.Add((byte)((configListsValues[3] != -1) ? configListsValues[3] : oldValues[9])); // D
            requestPackage.Add((byte)((configListsValues[9] != -1) ? configListsValues[9] : oldValues[10])); // L
            requestPackage.Add((byte)((configListsValues[5] != -1) ? configListsValues[5] : oldValues[11])); // F
            requestPackage.Add((byte)((configListsValues[0] != -1) ? configListsValues[0] : oldValues[12])); // A
            requestPackage.Add((byte)((configListsValues[11] != -1) ? configListsValues[11] : oldValues[13])); // P

            //requestPackage.Add((byte)configListsValues[6]); // G
            //requestPackage.Add((byte)configListsValues[3]); // D
            //requestPackage.Add((byte)configListsValues[9]); // L
            //requestPackage.Add((byte)configListsValues[5]); // F
            //requestPackage.Add((byte)configListsValues[0]); // A
            //requestPackage.Add((byte)configListsValues[11]); // P


            requestPackage.Add(oldValues[14]); // reserved


            requestPackage.Add((byte)((configListsValues[1] != -1) ? configListsValues[1] : oldValues[15])); // B
            requestPackage.Add((byte)((configListsValues[2] != -1) ? configListsValues[2] : oldValues[16])); // C

            //requestPackage.Add((byte)(configListsValues[1]/* % 256*/)); // B
            ////requestPackage.Add((byte)(configListsValues[1] / 256)); // B
            //requestPackage.Add((byte)(configListsValues[2]/* % 256*/)); // C
            ////requestPackage.Add((byte)(configListsValues[2] / 256)); // C


            requestPackage.Add(oldValues[17]); // reserved
            requestPackage.Add(oldValues[18]); // reserved


            requestPackage.Add((byte)((configListsValues[10] != -1) ? configListsValues[10] : oldValues[19])); // C

            //requestPackage.Add((byte)configListsValues[10]); // M

            //while (requestPackage.Count < 20)
            //    requestPackage.Add(0);

            // add checksum bytes
            requestPackage.AddRange(GenerateChecksum(requestPackage.ToArray()));

            #endregion

            #region Execute command

            // execute command: send -> wait -> receive -> check response
            retval = ExecuteCommand(requestPackage.ToArray(), waitTime, out responsePackage, desiredResponseLength);
            if (retval != CommunicationResult.Success)
                return retval;

            #endregion

            #region Parse package
            

            #endregion

            return retval;
        }

        #endregion

        #region SetMarkAutoscan
        public static CommunicationResult SetMarkAutoscan(bool testOffline, byte color, byte moduleType, byte fromValue, byte autoscanValue,
                out bool ready, out int[] autoscanListsValues, out int[] configDeviceInfo)
        {
            CommunicationResult retval = CommunicationResult.Success;
            byte[] commandHeader = new byte[] { 0x3A, 0x01, 0x01};
            List<byte> requestPackage = new List<byte>();
            int desiredResponseLength = -1;//8;
            int waitTime = 500;
            byte[] responsePackage = new byte[1];

            autoscanListsValues = new int[4] { 0, 0, 0xFF, 0xFF };
            configDeviceInfo = new int[5];
            ready = false;

            if (!testOffline)
            {
                #region Generate request package

                // add communication header
                requestPackage.AddRange(CommunicationHeader);
                // add command header
                requestPackage.AddRange(commandHeader);
                // add zeros to fill package
                requestPackage.Add(0);
                // add color byte
                requestPackage.Add(color);
                // add module type byte
                requestPackage.Add(moduleType);
                // add rewrite fromValue byte
                requestPackage.Add(fromValue);
                // add autoscan byte
                requestPackage.Add(autoscanValue);

                // add checksum bytes
                requestPackage.AddRange(GenerateChecksum(requestPackage.ToArray()));

                #endregion

                #region Execute command

                // execute command: send -> wait -> receive -> check response
                retval = ExecuteCommand(requestPackage.ToArray(), waitTime, out responsePackage, desiredResponseLength);
                if (retval != CommunicationResult.Success)
                    return retval;

                #endregion
            }
            else
            {
                responsePackage = new byte[] { 0xE8, 0x32, 0x01, 0x3A, 0x01, 0x01, 0x20, 0x10, 0x00, 0x02, 0x55, 
                    0x07, 0x1F, 0x08, 0x02, 0x85, 0x01, 0x00, 0x84, 0x02 };

            }

            #region Parse package

            int dataStart = CommunicationHeader.Length + commandHeader.Length - 1;

            if (fromValue != 1) // expect short answer if fromValue is not 1 ("from playbill" is not checked)
            {
                if (responsePackage[dataStart] != 1)
                    retval = CommunicationResult.AutoscanFailed;
            }
            else // expect long answer if fromValue is 1  ("from playbill" is checked)
            {
                dataStart++; // long answer starts one byte later than short

                autoscanListsValues[0] = responsePackage[dataStart++]; // B
                autoscanListsValues[1] = responsePackage[dataStart++]; // C
                autoscanListsValues[2] = responsePackage[dataStart++]; // D
                autoscanListsValues[3] = responsePackage[dataStart++]; // HUB 

                //autoscanListsValues[0] = responsePackage[dataStart++]; // A
                //autoscanListsValues[1] = responsePackage[dataStart++]; // B
                //autoscanListsValues[2] = responsePackage[dataStart++]; // C
                //autoscanListsValues[3] = responsePackage[dataStart++]; // D
                //autoscanListsValues[4] = responsePackage[dataStart++]; // HUB

                if (responsePackage[dataStart++] != 0x55)
                    ready = false;
                else
                    ready = true;

                configDeviceInfo[0] = responsePackage[dataStart++]; // equipment connected
                configDeviceInfo[1] = responsePackage[dataStart++] + 1; // memory
                configDeviceInfo[2] = responsePackage[dataStart++] + (responsePackage[dataStart++] << 8); // voltage
                configDeviceInfo[3] = responsePackage[dataStart++] + (responsePackage[dataStart++] << 8); // temperature
                configDeviceInfo[4] = responsePackage[dataStart++]; // brightness
            }

            #endregion

            return retval;
        }

        #endregion
        
        #region GetMarkAutoscan
        public static CommunicationResult GetMarkAutoscan(bool testOffline,
            out bool ready, out int[] autoscanListsValues, out int[] configDeviceInfo)
        {
            CommunicationResult retval = CommunicationResult.Success;
            byte[] commandHeader = new byte[] { 0x3A, 0x01, 0x01 };
            List<byte> requestPackage = new List<byte>();
            int desiredResponseLength = -1;//26;
            int waitTime = 700;
            byte[] responsePackage = new byte[1];

            autoscanListsValues = new int[4];
            configDeviceInfo = new int[5];
            ready = false;

            if (!testOffline)
            {
                #region Generate request package

                // add communication header
                requestPackage.AddRange(CommunicationHeader);
                // add command header
                requestPackage.AddRange(commandHeader);
                // add 5 zeros to fill package
                requestPackage.AddRange(new byte[] { 0, 0, 0, 0 });
                // add autoscan value byte - check if controller is busy
                requestPackage.Add(0x55);

                // add checksum bytes
                requestPackage.AddRange(GenerateChecksum(requestPackage.ToArray()));

                #endregion

                #region Execute command

                // execute command: send -> wait -> receive -> check response
                retval = ExecuteCommand(requestPackage.ToArray(), waitTime, out responsePackage, desiredResponseLength);
                if (retval != CommunicationResult.Success)
                    return retval;

                #endregion
            }
            else
            {
                responsePackage = new byte[] { 0xE8, 0x32, 0x01, 0x3A, 0x01, 0x01, 0x20, 0x10, 0x32, 0x07, 
                    0x55, 
                    0x07, 0x1F, 0x08, 0x02, 0x85, 0x01, 0x00, 0x84, 0x02 };
                
            }
            #region Parse package
            try
            {
                
                // set index to start looking for data at
                int dataStart = (CommunicationHeader.Length + commandHeader.Length);

                autoscanListsValues[0] = responsePackage[dataStart++]; // B
                autoscanListsValues[1] = responsePackage[dataStart++]; // C
                autoscanListsValues[2] = responsePackage[dataStart++]; // D
                autoscanListsValues[3] = responsePackage[dataStart++]; // HUB 
                    
                //autoscanListsValues[0] = responsePackage[dataStart++]; // A
                //autoscanListsValues[1] = responsePackage[dataStart++]; // B
                //autoscanListsValues[2] = responsePackage[dataStart++]; // C
                //autoscanListsValues[3] = responsePackage[dataStart++]; // D
                //autoscanListsValues[4] = responsePackage[dataStart++]; // HUB

                if (responsePackage[dataStart++] != 0x55)
                    ready = false;
                else
                    ready = true;

                configDeviceInfo[0] = responsePackage[dataStart++]; // equipment connected
                configDeviceInfo[1] = responsePackage[dataStart++] +1; // memory
                configDeviceInfo[2] = responsePackage[dataStart++] + (responsePackage[dataStart++] << 8); // voltage
                configDeviceInfo[3] = responsePackage[dataStart++] + (responsePackage[dataStart++] << 8); // temperature
                configDeviceInfo[4] = responsePackage[dataStart++]; // brightness

            }
            catch
            {
                retval = CommunicationResult.ResponseParsingFailed;
            }

            #endregion

            return retval;
        }

        #endregion
        
        #endregion

        #endregion
        
        #endregion

        #endregion

    }
    #endregion

    #region Communication enums
    
    #region CommunicationType
    /// <summary>
    /// Available communication types.
    /// </summary>
    public enum CommunicationType
    { 
        /// <summary>
        /// The connection was not established.
        /// </summary>
        None,
        /// <summary>
        /// Connection by RS232 (serial) port.
        /// </summary>
        Serial,
        /// <summary>
        /// Connection by network.
        /// </summary>
        Network
    }
    #endregion

    #region CommunicationResult
    /// <summary>
    /// Available results of communication operation.
    /// </summary>
    public enum CommunicationResult
    {
        /// <summary>
        /// Communication success.
        /// </summary>
        Success = 1,
        /// <summary>
        /// Communication failed, but the reason is unknown.
        /// </summary>
        Failed = 0,
        /// <summary>
        /// Connection is not established.
        /// </summary>
        NoConnection = -1,
        /// <summary>
        /// Failed opening connection.
        /// </summary>
        ConnectionOpenFailed = -2,
        /// <summary>
        /// Failed closing connection.
        /// </summary>
        ConnectionCloseFailed = -3,
        /// <summary>
        /// For some reason response package is longer than expected.
        /// </summary>
        ResponseTooLong = -4,
        /// <summary>
        /// For some reason response package is shorter than expected.
        /// </summary>
        ResponseTooShort = -5,
        /// <summary>
        /// The checksum does not fit the package.
        /// </summary>
        BadChecksum = -6,
        /// <summary>
        /// Response has other value than it should have.
        /// </summary>
        ResponseParsingFailed = -7,
        /// <summary>
        /// New time was not set to the controller
        /// </summary>
        TimeSettingFailed = -8,
        /// <summary>
        /// There is a problem with parsing received time bytes.
        /// </summary>
        TimeGettingFailed = -9,
        /// <summary>
        /// Received data is not correct, try getting that data again.
        /// </summary>
        BadDataTryAgain = -10,
        /// <summary>
        /// Creating file in controller failed.
        /// </summary>
        CreatingFileFailed = -11,
        /// <summary>
        /// Writing file content to controller failed.
        /// </summary>
        WritingFileFailed = -12,
        /// <summary>
        /// Closing file in controller failed.
        /// </summary>
        ClosingFileFailed = -13,
        /// <summary>
        /// Request writing failed.
        /// </summary>
        RequestWritingFailed = -14,
        /// <summary>
        /// Response reading has failed.
        /// </summary>
        ResponseReadingFailed = -15,
        /// <summary>
        /// Restarting controller failed.
        /// </summary>
        RestartHardwareFailed = -16,
        /// <summary>
        /// Restarting application failed
        /// </summary>
        RestartApplicationFailed = -17,
        /// <summary>
        /// Setting brighness has failed.
        /// </summary>
        BrightnessSettingFailed = -18,
        /// <summary>
        /// Autoscan enabling operation failed.
        /// </summary>
        AutoscanFailed = -19,
        /// <summary>
        /// Deleting file(s) operation failed.
        /// </summary>
        DeletingFileFailed = -20,
        /// <summary>
        /// Getting controller ID and RS232 baudrate failed.
        /// </summary>
        IdGettingFailed = -21,
        /// <summary>
        /// Setting new controller ID and RS232 baudrate failed.
        /// </summary>
        IdSettingFailed = -22,
        /// <summary>
        /// Another communication operation is in progress.
        /// </summary>
        AnotherOperationInProgress = -23,
        /// <summary>
        /// Setting IP configuration failed.
        /// </summary>
        IPSettingFailed = -24,
        /// <summary>
        /// Getting controller type and version information failed
        /// </summary>
        VersionGettingFailed = -25,
        /// <summary>
        /// Setting configuration operation failed.
        /// </summary>
        ConfigurationSettingFailed = -26,
        /// <summary>
        /// Getting configuration failed.
        /// </summary>
        ConfigurationGettingFailed = -27
    }
    #endregion

    #endregion
}
