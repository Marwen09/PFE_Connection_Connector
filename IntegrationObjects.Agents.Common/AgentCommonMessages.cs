using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace IntegrationObjects.Agents.Common
{
    public class AgentCommonMessages
    {
        public static readonly string ConnectToDatabseSuccessful = "Successfuly connected To Database";
        public static readonly string ConnectToDatabseFailed = "Connect To Database Failed";
        public const string InitializingIniConfigurationSuccessful = "Initializing INI configuration was successful";
        public const string InitializingIniConfigurationFailed = "Initializing INI configuration failed";
        public const string GCFalied = "Error occured when Performing Garbage Collector Work, Exception : ";
        public static readonly string serviceProcessName = Process.GetCurrentProcess().ProcessName;
        public static string LogInitializationFailed(string strError)
        {
            return $"Log initialization failed: {strError}";
        }
    }
}
