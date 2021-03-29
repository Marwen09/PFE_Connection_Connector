using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;

namespace IntegrationObjects.SIOTHConnectorName.Helper
{
    public class IOHelper
    {
        public static IOHelper AgentConfig = new IOHelper();
        public  string Topic { get; set; }
        public  string CommunicationChannel { get; set; }
        public  string ZMQAddress { get; set; }
        public  string DataTransferMode { get; set; }
        public  object Agent_Type { get; set; }
        public  string Agent_SchemaID { get; set; }

        public   dataExtractionProperties dataExtractionProperties = new dataExtractionProperties();
        public   Dictionary<string, object> TagsMapping { get; set; }
     
        public  List<ZMQPublisherList> PublisherList { get; set; }
        public  string ZMQListeningOnRequest { get; set; }
        public  string SIOTHLogZMQAddress { get; set; }
        public  bool ZMQSecurity { get; set; }
        public int Status_Timeout { get; set; }
        public int ConsumerRate { get; set; }
       // public Dictionary<string, Dictionary<string, string>> FieldsMappings { get; set; }
        public List<FieldsMappings> FieldsMappings = new List<FieldsMappings>();
        [DataMember]
        public Mapping Mapping { get; set; }

        public static IOHelper LoadConfiguration(string mappingFilePath, out string loadconfigError)
        {
    
            try
            {
               
                loadconfigError = string.Empty;
                IOHelper AgentConfig;
                string JsonConfiguration = string.Empty;
                using (var reader = new StreamReader(mappingFilePath))
                {
                    JsonConfiguration = reader.ReadToEnd();
                }
                return AgentConfig= JsonConvert.DeserializeObject < IOHelper > (JsonConfiguration);
                
            }
            catch (Exception ex)
            {
                loadconfigError = ex.Message;
                return null;
            }
           



        }
       
        public static void LoadIniFileConfiguration()
        {
           // throw new NotImplementedException();
        }
    }
    public class Marwen
    {
        public string TagName { get; set; }
        public string Access_Right { get; set; }
        public string Type { get; set; }
        public int Instance { get; set; }
        public List<string> Properties = new List<string>();
        public List<string> PropertiesTypes = new List<string>(); 
        public List<string> PropertiesAccessRight = new List<string>();

        public Marwen() { }
    }

}
