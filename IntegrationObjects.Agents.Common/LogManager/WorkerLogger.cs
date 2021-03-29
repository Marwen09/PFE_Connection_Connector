
using IntegrationObjects.Logger.SDK;
using IntegrationObjects.SIOTHAPI.Messaging.PubSubPattern;
using IntegrationObjects.SIOTHConnectorName.Helper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace IntegrationObjects.Agents.Common
{
   public class WorkerLogger
    {
        #region "Attributes"

        /// <summary>
        /// Defines the temporary variable which would be used in order to store the current path of the application.
        /// </summary>
        public static IOLog myLog;

        private static ZMQPublisher ZMQLogPublisher;

        /// <summary>
        /// The file automatic append
        /// </summary>
        public static Boolean fileAutoAppend = true;
        /// <summary>
        /// The file buffer size
        /// </summary>
        public static int fileBufferSize = 100;
        /// <summary>
        /// The file file name
        /// </summary>
        public static String fileFileName = "ServiceLogs";
        /// <summary>
        /// The maximum files
        /// </summary>
        public static int maximumFiles;

        public static void Control(object connectToMongo)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The file extension
        /// </summary>
        public static String fileExtension = "log";
        /// <summary>
        /// The file header file
        /// </summary>
        public static String fileHeaderFile = "     " + Process.GetCurrentProcess().ProcessName + "  ";

        /// <summary>
        /// The file level
        /// </summary>
        public static MessageType fileLevel = MessageType.Control;
        private static readonly string LOG_ID = "Worker";
        private static string AgentName;
        #endregion

        #region "Methods"
        public static bool InitializeLogger(string ServiceName, out string strError, string _AgentName)
        {

            strError = String.Empty;
            if (IOLog.DefinedLogs != null && IOLog.DefinedLogs.ContainsKey(LOG_ID))
                return true;

            try
            {
                fileAutoAppend = Boolean.Parse(WorkerFilesManager._attConfigMBAgent.IniReadValue("FileLogConfiguration", "AutoAppend"));
                fileBufferSize = Int32.Parse(WorkerFilesManager._attConfigMBAgent.IniReadValue("FileLogConfiguration", "BufferSize"));
                maximumFiles = Int32.Parse(WorkerFilesManager._attConfigMBAgent.IniReadValue("FileLogConfiguration", "MaximumFiles"));
                fileLevel = (MessageType)Enum.Parse(typeof(MessageType), WorkerFilesManager._attConfigMBAgent.IniReadValue("FileLogConfiguration", "Level"));
                fileExtension = "log";
                fileHeaderFile = "                   " + ServiceName + " " + _AgentName + "              " + Environment.NewLine +
                                 "==                      Version : 1.0.0                        ==" + Environment.NewLine +
                                 "==                   Build : " + DateCompiled().ToString("yyyyMMdd") + "                        ==" + Environment.NewLine +
                                 "==           Copyright © 2021 Integration Objects         ==";

                FileLogConfiguration file = new FileLogConfiguration();
                file.AutoAppend = fileAutoAppend;
                file.BufferSize = fileBufferSize;
                file.FileName = ServiceName;
                ServiceName = ServiceName;
                file.MaximumFiles = maximumFiles;
                file.HeaderFile = fileHeaderFile;
                file.Level = fileLevel;
                file.AutoSaveTimeOut = 10;
                AgentName = _AgentName;
                file.FolderPath = AppDomain.CurrentDomain.BaseDirectory;


                myLog = new IOLog("IOLOG062011123MBO2010", null, file, null, true, LOG_ID);
                //  myLog = new IOLog("IOLOG062011123MBO2010", null, file, null, true );

                return true;
            }
            catch (Exception objException)
            {
                strError = AgentExceptions.GetExceptionMessage(objException);
                return false;
            }
        }
        //public static bool InitializeLogger(string serviceName, out string strError, string _AgentName)
        //{

        //    strError = String.Empty;
        //    if (IOLog.DefinedLogs != null && IOLog.DefinedLogs.ContainsKey(LOG_ID))
        //        return true;

        //    try
        //    {

        //        fileAutoAppend = Boolean.Parse(WorkerFilesManager._attConfigDBAgent.IniReadValue("FileLogConfiguration", "AutoAppend"));
        //        fileBufferSize = Int32.Parse(WorkerFilesManager._attConfigDBAgent.IniReadValue("FileLogConfiguration", "BufferSize"));
        //        maximumFiles = Int32.Parse(WorkerFilesManager._attConfigDBAgent.IniReadValue("FileLogConfiguration", "MaximumFiles"));
        //        fileLevel = (MessageType)Enum.Parse(typeof(MessageType), WorkerFilesManager._attConfigDBAgent.IniReadValue("FileLogConfiguration", "Level"));
        //        fileExtension = "log";
        //        fileHeaderFile = "                   " + _AgentName + "              " + Environment.NewLine +
        //                         "==                      Version : 1.0.0                        ==" + Environment.NewLine +
        //                         "==                   Build : " + DateCompiled().ToString("yyyyMMdd") + "                        ==" + Environment.NewLine +
        //                         "==           Copyright © 2020 Integration Objects         ==";

        //        FileLogConfiguration file = new FileLogConfiguration();
        //        file.AutoAppend = fileAutoAppend;
        //        file.BufferSize = fileBufferSize;
        //        file.FileName = serviceName;
        //        file.MaximumFiles = maximumFiles;
        //        file.HeaderFile = fileHeaderFile;
        //        file.Level = fileLevel;
        //        file.AutoSaveTimeOut = 10;
        //        AgentName = _AgentName;
        //        file.FolderPath = AppDomain.CurrentDomain.BaseDirectory;

        //        //myLog = new IOLog("IOLOG062011123MBO2010", null, file, null, true, LOG_ID);
        //        //  myLog = new IOLog("IOLOG062011123MBO2010", null, file, null, true );
        //        IOLog.TraceSystemInformations();
        //        return true;
        //    }
        //    catch (Exception objException)
        //    {
        //        strError = AgentExceptions.GetExceptionMessage(objException);
        //        return false;
        //    }
        //}
        public static void TraceLog(MessageType enumMessageType, string strText)
        {
            try
            {
                string strError;
                IOLog.TraceLog(AgentName, strText, enumMessageType);
                string loglevel = string.Empty;
                switch (enumMessageType)
                {
                    case MessageType.Control:
                        loglevel = "Control";
                        break;
                    case MessageType.Error:
                        loglevel = "Error";
                        break;
                    case MessageType.Warning:
                        loglevel = "Warning";
                        break;
                    case MessageType.Inform:
                        loglevel = "Inform";
                        break;
                    case MessageType.Debug:
                        loglevel = "Debug";
                        break;

                }
                if (enumMessageType == MessageType.Control || enumMessageType == MessageType.Error)
                {
                    SIOTHLogMessage logMessage = new SIOTHLogMessage() { LogDate = DateTime.Now, LogLevel = loglevel, LogSource = AgentName, Message = strText };
                    if (ZMQLogPublisher != null)
                    {
                        ZMQLogPublisher.PublishData(JsonConvert.SerializeObject(logMessage), "SIOTH##LogTopic", out strError);

                        if (!string.IsNullOrEmpty(strError))
                        {
                            WorkerLogger.TraceLog(MessageType.Error, "Error occurred while publishing log message To SIOTH Logging System");
                        }
                    }

                }

            }
            catch
            {
                // ignored
            }
        }
        //public static void TraceLog(MessageType enumMessageType, string strText)
        //{
        //    try
        //    {
        //        IOLog.TraceLog("", strText, enumMessageType);               
        //    }
        //    catch
        //    {
        //        // ignored
        //    }
        //}
        public static void InitializeSIOTHLoggingPublisher(string SIOTHLogZMQAddress, bool UseSecurity)
        {
            try
            {
                ZMQLogPublisher = new ZMQPublisher();
                string strError;
                ZMQLogPublisher.InitialiseLogggerPublisher(SIOTHLogZMQAddress, out strError,1000,UseSecurity);
                if (!string.IsNullOrEmpty(strError))
                {
                    IOLog.TraceLog(AgentName, "Error occurred while Initializing Logging Publisher",MessageType.Error);
                }
            }
            catch (Exception ex )
            {
                
            }
        }
        public static DateTime DateCompiled()
        {
            try
            {
                string filePath = Assembly.GetCallingAssembly().Location;
                const int cPeHeaderOffset = 60;
                const int cLinkerTimestampOffset = 8;
                byte[] b = new byte[2048];
                Stream s = null;

                try
                {
                    using (s = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        s.Read(b, 0, 2048);
                    }
                }
                finally
                {
                    if (s != null)
                    {
                        s.Close();
                    }
                }

                int i = BitConverter.ToInt32(b, cPeHeaderOffset);
                int secondsSince1970 = BitConverter.ToInt32(b, i + cLinkerTimestampOffset);
                DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                dt = dt.AddSeconds(secondsSince1970);
                dt = dt.ToLocalTime();
                return dt;
            }
            catch
            {
                return DateTime.Now;
            }
        }
        //public static void TraceLog(MessageType enumMessageType, string strText,bool ToBeArchived)
        //{
        //    try
        //    {
        //        string strError;
        //        //Always log message to File
        //        IOLog.TraceLog(AgentName, strText, enumMessageType);
        //        if (ToBeArchived)
        //        {
        //            // Publish message To Kafka Topic
        //            string loglevel = string.Empty;
        //            switch (enumMessageType)
        //            {
        //                case MessageType.Control:
        //                    loglevel = "Control";
        //                    break;
        //                case MessageType.Error:
        //                    loglevel = "Error";
        //                    break;
        //                case MessageType.Warning:
        //                    loglevel = "Warning";
        //                    break;
        //                case MessageType.Inform:
        //                    loglevel = "Inform";
        //                    break;
        //                case MessageType.Debug:
        //                    loglevel = "Debug";
        //                    break;

        //            }
        //            SIOTHLogMessage logMessage = new SIOTHLogMessage() { LogDate = DateTime.Now, LogLevel = loglevel, LogSource = AgentName, Message = strText };

        //            ZMQLogPublisher.PublishData(JsonConvert.SerializeObject(logMessage), "SIOTH##LogTopic", out strError);

        //            if (!string.IsNullOrEmpty(strError))
        //            {
        //                IOLog.TraceLog(AgentName, "Error occurred while publishing log message To SIOTH Logging System", MessageType.Error);
        //            }
        //            //UtilClass.CreateLogMessageObject(loglevel, strText, AgentName, DateTime.Now);
        //        }
                
        //    }
        //    catch
        //    {
        //        // ignored
        //    }
        //}
        //public static void TraceLog(MessageType messageType, string Message, string Source)
        //{
        //    try
        //    {
        //        IOLog.TraceLog($"[{Source}]", Message, messageType);
        //    }
        //    catch
        //    {
        //        // ignored
        //    }
        //}
        //public static void TraceLog(MessageType enumMessageType, string strText, bool ToBeArchived, string Source)
        //{
        //    try
        //    {
        //        string strError;
        //        //Always log message to File
        //        IOLog.TraceLog($"[{Source}]", strText, enumMessageType);
        //        if (ToBeArchived)
        //        {
        //            // Publish message To Kafka Topic
        //            string loglevel = string.Empty;
        //            switch (enumMessageType)
        //            {
        //                case MessageType.Control:
        //                    loglevel = "Control";
        //                    break;
        //                case MessageType.Error:
        //                    loglevel = "Error";
        //                    break;
        //                case MessageType.Warning:
        //                    loglevel = "Warning";
        //                    break;
        //                case MessageType.Inform:
        //                    loglevel = "Inform";
        //                    break;
        //                case MessageType.Debug:
        //                    loglevel = "Debug";
        //                    break;

        //            }
        //            SIOTHLogMessage logMessage = new SIOTHLogMessage() { LogDate = DateTime.Now, LogLevel = loglevel, LogSource = AgentName, Message = strText };

        //            ZMQLogPublisher.PublishData(JsonConvert.SerializeObject(logMessage), "SIOTH##LogTopic", out strError);

        //            if (!string.IsNullOrEmpty(strError))
        //            {
        //                IOLog.TraceLog(AgentName, "Error occurred while publishing log message To SIOTH Logging System", MessageType.Error);
        //            }
        //            //UtilClass.CreateLogMessageObject(loglevel, strText, AgentName, DateTime.Now);
        //        }

        //    }
        //    catch
        //    {
        //        // ignored
        //    }
        //}
        #endregion

        #region DHIBI Enhancements
        internal static void SendSIOTHLogMessage(string loglevel, string strText)
        {
            string strError;
            SIOTHLogMessage logMessage = new SIOTHLogMessage() { LogDate = DateTime.Now, LogLevel = loglevel, LogSource = AgentName, Message = strText };

            ZMQLogPublisher.PublishData(JsonConvert.SerializeObject(logMessage), "SIOTH##LogTopic", out strError);

            if (!string.IsNullOrEmpty(strError))
            {
                IOLog.TraceLog(AgentName, "Error occurred while publishing log message To SIOTH Logging System", MessageType.Error);
            }
        }
        public static void Inform(string Message, bool UseSIOTHLogging = false)
        {
            IOLog.TraceLog(AgentName, Message, MessageType.Inform);
            if (UseSIOTHLogging && ZMQLogPublisher !=null)
            {
                SendSIOTHLogMessage("Inform", Message);
            }
        }
        public static void Warning(string Message, bool UseSIOTHLogging = false)
        {
            IOLog.TraceLog(AgentName, Message, MessageType.Warning);
            if (UseSIOTHLogging && ZMQLogPublisher != null)
            {
                SendSIOTHLogMessage("Warning", Message);
            }
        }
        public static void Debug(string Message, bool UseSIOTHLogging = false)
        {
            IOLog.TraceLog(AgentName, Message, MessageType.Debug);
            if (UseSIOTHLogging && ZMQLogPublisher != null)
            {
                SendSIOTHLogMessage("Debug", Message);
            }
        }
        public static void Control(string Message, bool UseSIOTHLogging = false)
        {
            IOLog.TraceLog(AgentName, Message, MessageType.Control);
            if (UseSIOTHLogging && ZMQLogPublisher != null)
            {
                SendSIOTHLogMessage("Control", Message);
            }
        }
        public static void Exception(Exception Ex, bool UseSIOTHLogging = false)
        {
            var stack = new StackTrace(Ex, true);
            var line = stack.GetFrame(0).GetFileLineNumber();
            var FileName = stack.GetFrame(0).GetFileName();
            IOLog.TraceLog(AgentName, $"[{FileName}_{stack.GetFrame(0).GetMethod().Name}][L_{line}][Message : {Ex.Message}]", MessageType.Error);
            if (UseSIOTHLogging && ZMQLogPublisher != null)
            {
                SendSIOTHLogMessage("Error", Ex.Message);
            }
        }
        public static void Error(string Message, bool UseSIOTHLogging = false)
        {
            IOLog.TraceLog(AgentName, Message, MessageType.Error);
            if (UseSIOTHLogging)
            {
                SendSIOTHLogMessage("Error", Message);
            }
        }
        #endregion
    }
}
