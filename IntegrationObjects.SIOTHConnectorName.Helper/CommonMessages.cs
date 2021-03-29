using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationObjects.SIOTHConnectorName.Helper
{
    public static class CommonMessages
    {
        public static string LoadInifileConfig="Load InifileConfig Succeeded";
        public static string IsPublisherWorker= "Publisher Worker";
        public static string IsConsumerWorker="Cosumer is working";
        public static string LoadConfigFileSucceeded= "Load Config File Succeeded";
        public static string LoadConfigFileFailed="Load Config File Failed";

        public static string InitializeZMQConnectionSucc { get; set; }

        public static string InitializeZMQConnectionFailed(string strError)
        {
            throw new NotImplementedException();
        }

        public static object ZMQTopicList(object zMQAddress, string v)
        {
            throw new NotImplementedException();
        }
    }
}
