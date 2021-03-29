using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace IntegrationObjects.Agents.Common
{
   public class WorkerFilesManager
    {

        /// <summary>
        /// The configuration instance of the  Gateway GUI Config
        /// </summary>
        public static WorkerIniConfiguration _attConfigMBAgent;

        /// <summary>
        /// The configuration instance of the  Gateway GUI Config
        /// </summary>
        //public static WorkerIniConfiguration _attConfigDBAgent;

        #region Attributes

        #region LogConfig

        /// <summary>
        /// The default log level 
        /// </summary>
        private static string _attStrLogLevel;

        /// <summary>
        /// The Log file max size
        /// </summary>
        private static int _attILogFileMaxSize;

        /// <summary>
        /// Archives or not the last log
        /// </summary>
        private static string _attStrArchiveLastLog;

        /// <summary>
        /// The name of the log file
        /// </summary>
        private static string _attStrLogFileName;

        /// <summary>
        /// The port which will receive log messages
        /// </summary>
        private static int _iLogPort;

        #endregion

        #endregion

        #region Setters and getters
        /// <summary>
        /// Gets and Sets the default log level
        /// </summary>
        public static string LogLevel
        {
            get { return _attStrLogLevel; }
            set
            {
                _attStrLogLevel = value;
                _attConfigMBAgent.IniWriteValue("LogSetting", "LogLevel", value);
            }
        }

        /// <summary>
        /// Gets and Sets The maximum number of log messages that will be displayed
        /// </summary>
        public static int LogFileMaxSize
        {
            get { return _attILogFileMaxSize; }
            set
            {
                _attILogFileMaxSize = value;
                _attConfigMBAgent.IniWriteValue("LogSetting", "LogFileMaxSize", value.ToString());
            }
        }



        /// <summary>
        /// Gets and Sets if the end user wants to archive the last log
        /// </summary>
        public static string ArchiveLastLog
        {
            get
            {
                return _attStrArchiveLastLog;
            }

            set
            {
                _attStrArchiveLastLog = value;
                _attConfigMBAgent.IniWriteValue("LogSetting", "ArchiveLastLog", value);
            }
        }

        /// <summary>
        /// Gets and Sets the name of the log file
        /// </summary>
        public static string LogFileName
        {
            get { return _attStrLogFileName; }
            set
            {
                _attConfigMBAgent.IniWriteValue("LogSetting", "FileName", value);
                _attStrLogFileName = value;
            }
        }

        /// <summary>
        /// Gets and Sets The port which will receive log messages
        /// </summary>
        public static int LogPort
        {
            get { return _iLogPort; }
            set { _iLogPort = value; }
        }
        #endregion

        #region InitializeIniFiles
        public static void InitializeIniFiles()
        {
            string pathIni = AppDomain.CurrentDomain.BaseDirectory + "Config.ini";
            if (!File.Exists(pathIni))
            {
                CreateIniFile(pathIni);
            }
            else
            {
                _attConfigMBAgent = new WorkerIniConfiguration(pathIni);
            }
        }

        public static void CreateIniFile(string strFileName)
        {
            _attConfigMBAgent = new WorkerIniConfiguration(strFileName);
            try
            {
                _attConfigMBAgent.IniWriteValue("FileLogConfiguration", "AutoAppend", "true");
                _attConfigMBAgent.IniWriteValue("FileLogConfiguration", "BufferSize", "100");
                _attConfigMBAgent.IniWriteValue("FileLogConfiguration", "FileName", Process.GetCurrentProcess().ProcessName);
                _attConfigMBAgent.IniWriteValue("FileLogConfiguration", "MaximumFiles", "0");
                _attConfigMBAgent.IniWriteValue("FileLogConfiguration", "LogFileMaxSize", "10");
                _attConfigMBAgent.IniWriteValue("FileLogConfiguration", "Level", "Error");
                _attConfigMBAgent.IniWriteValue("FileLogConfiguration", "AutoSaveTimeOut", "10");
                _attConfigMBAgent.IniWriteValue("WorkerConfiguration", "ReconnectionPeriod", "60");
                _attConfigMBAgent.IniWriteValue("WorkerConfiguration", "ConsumePeriod", "1");
                _attConfigMBAgent.IniWriteValue("WorkerConfiguration", "PublishPeriod", "1000");


            }
            catch (Exception)
            {
                // ignored
            }

        }

        #endregion 
    }
}
