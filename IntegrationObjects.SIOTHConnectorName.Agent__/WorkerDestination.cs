using IntegrationObjects.Agents.Common;
using IntegrationObjects.Logger.SDK;
using IntegrationObjects.SIOTHAPI.Messaging.PubSubPattern;
using IntegrationObjects.SIOTHAPI.Messaging.ReqRespPattern;
using IntegrationObjects.SIOTHConnectorName.Helper;
using JUST;
using System;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using System.Threading;

namespace IntegrationObjects.SIOTHConnectorName.Agent
{
    public class WorkerDestination : WorkerManager
    {
        #region Attributes
        private readonly ZMQSubscriber ZMQSubscriber;
       private Dictionary<string, Dictionary<string, string>> MappingList = new Dictionary<string, Dictionary<string, string>>();
        #endregion
        #region Constructor
        public WorkerDestination()
        {
            try
            {
                WorkerLogger.Debug("Start Initializing ZMQ Consumer.");
                ZMQSubscriber = new ZMQSubscriber();

                Dictionary<string, List<string>> DicZMQTopic = new Dictionary<string, List<string>>();

                foreach (ZMQPublisherList item in IOHelper.AgentConfig.PublisherList)
                {
                    DicZMQTopic.Add(item.ZMQAddress, item.TopicList);
                   // WorkerLogger.Control(CommonMessages.ZMQTopicList(item.ZMQAddress, string.Join(",", item.TopicList.ToArray())));
                }
                string strError = string.Empty;
                ZMQSubscriber.InitialiseMultipleSubscriber(DicZMQTopic, out strError, 100, 1000, IOHelper.AgentConfig.ZMQSecurity);

                if (!string.IsNullOrEmpty(strError))
                {
                    WorkerLogger.Error(CommonMessages.InitializeZMQConnectionFailed(strError));
                }
                else
                {
                    WorkerLogger.Control(CommonMessages.InitializeZMQConnectionSucc);
                }
                
           //     LoadMappingConfig();

            }
            catch (Exception Ex0)
            {
                //WorkerLogger.Exception(Ex0);
                WorkerLogger.TraceLog(MessageType.Error, Ex0.Message);
            }
        }
        #endregion
        #region Methods
        internal void StartWritingData(object obj)
        {
            try
            {
               
                Thread ArchiverThread = new Thread(WriteDataToDestination)
                {
                    IsBackground = true,
                    Name = "Thread to archiver incoming Data from SIOTH To /./././."
                };
                ArchiverThread.Start();
            }
            catch (Exception Ex0)
            {
                throw;
            }
            
           
        }
        public void SetMappingList()
        {
            foreach (FieldsMappings map in IOHelper.AgentConfig.FieldsMappings)
            {
                Dictionary<string, string> Map = new Dictionary<string, string>();
                Map.Add("TagName", map.Mapping.TagName);
                Map.Add("Value", map.Mapping.Value);
                Map.Add("TimeStamp", map.Mapping.TimeStamp);
                MappingList.Add(map.SchemaID, Map);
            }
       
  
        }
        private void WriteDataToDestination(object obj)
        {
          //To Check Device Status
            Thread StatusThread = new Thread(CheckSyncHostStatus);
            StatusThread.Name = "Thread To Check Device Status";
            StatusThread.IsBackground = true;
            StatusThread.Start();

            try
            {
                string ConsumeErrorMsg = string.Empty;
                string error;
                string transformerSchemaId = "{\"SchemaId\" : \"#valueof($.SchemaId)\"}";
                //string item;
                Dictionary<string, string> SchemaToUse = new Dictionary<string, string>();
              
                
                   SetMappingList();
         
                while (true)
                {
                   
                    string result =ZMQSubscriber.ReceiveDataFromSubscriberList(out error);
                
                        if (!string.IsNullOrEmpty(result) && string.IsNullOrEmpty(error))
                        {
                            string resSchemaId = JsonTransformer.Transform(transformerSchemaId, result);
                            var payloadSchemaId = Newtonsoft.Json.JsonConvert.DeserializeObject<Payload>(resSchemaId);
                            if (MappingList.ContainsKey(payloadSchemaId.SchemaId))
                            {
                                MappingList.TryGetValue(payloadSchemaId.SchemaId, out SchemaToUse);
                                string transformer = "{\"Iterator\": {    \"#loop($.Payload)\": {    \"TagName\":\"#currentvalueatpath($." + SchemaToUse["TagName"] + ")\",\"Value\":\"#currentvalueatpath($." + SchemaToUse["Value"] + ")\"}}}";
                        
                            string res = JsonTransformer.Transform(transformer, result);
                                var payload = Newtonsoft.Json.JsonConvert.DeserializeObject<Payload>(res);
                                var valuesfromPalyload = payload.Iterator.ToList().Select(x =>
                                        {
                                        var values = x.Values.ToList();
                                        return values;
                                    });
                                int  PaylodLength=    valuesfromPalyload.ToList().Count;

                        
                            valuesfromPalyload.ToList().ForEach(m =>
                                        {
                                        if (m[0] != null)
                                        {
                                            if (IOHelper.AgentConfig.TagsMapping.ContainsKey(m[0].ToString()))
                                            {
                                                WriteItem(IOHelper.AgentConfig.TagsMapping[m[0].ToString()].ToString(), m[1]);
                                            }
                                        }

                                    });

                            }

                        }
                        

                    
                    Thread.Sleep(IOHelper.AgentConfig.ConsumerRate);
                }
            }
            catch (Exception Ex0)
            {
                WorkerLogger.TraceLog(MessageType.Error, Ex0.Message);

            }
        }
#endregion

    }
}
