using IntegrationObjects.Agents.Common;
using IntegrationObjects.Logger.SDK;
using IntegrationObjects.SIOTHAPI.Messaging.ReqRespPattern;
using IntegrationObjects.SIOTHConnectorName.Helper;
using Nancy.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace IntegrationObjects.SIOTHConnectorName.Agent
{
    public class WorkerManager
    {


        public PublisherClass item = new PublisherClass();
        public BlockingCollection<PublisherClass> Publisherqueue = new BlockingCollection<PublisherClass>();
        public static Dictionary<int, List<Tag>> SynchroneHost = new Dictionary<int, List<Tag>>();
        public static List<string> c = new List<string>();
        public static Dictionary<int, Dictionary<string, List<Object> >> ASynchroneHost = new Dictionary<int, Dictionary<string, List<Object>>>();



        /// <summary>This method load the devices configuration from the config file into a dictionary that contains all the information needed to read and write data


        /// <summary> this method Check whether the Hosts are connected or not///<example>
        /// For example:///<code>/// ConnectToBACnetDevices();///results///Device BasicServer is not connected, Device RoomSimulator is connected
        /// ConnectToBACnetDevices():Check the connectivity for the first time
        ///this method check periodicaly the connectivity of multiple hosts
        public void CheckSyncHostStatus()
        {
            Parallel.ForEach(SynchroneHost.Keys, item =>
            {
                foreach (Tag tag in SynchroneHost[item])
                {
                    WorkerManager.PingSynchHost(tag.Ip_Address, tag.Connection_Timeout, tag.DontFragment, tag.Data);
                }
            });
              
            
            

            

        }
        public void CheckASyncHostStatus()
        {
            Parallel.ForEach(ASynchroneHost.Keys, item => {
                PingAsyncHosts(ASynchroneHost[item]["Ip_Address"], item.ToString(), Convert.ToInt32(item)).Wait();

            });
        }

        public static void LoadConfig()
        {
            foreach (Tag tag in IOHelper.AgentConfig.dataExtractionProperties.DataConfig.DataConfiguration.Tags)
            {
                if(tag.OnlyHostPing == true && tag.Synchronous == true)
                {
                    if (SynchroneHost.ContainsKey(tag.UpdateRate)==false)
                    { 
                        SynchroneHost.Add(tag.UpdateRate, new List<Tag>());
                        SynchroneHost[tag.UpdateRate].Add(tag);
                    }
                    else
                    {
                        SynchroneHost[tag.UpdateRate].Add(tag);
                    }
                }
                else if( tag.OnlyHostPing == true && tag.Synchronous == false)
                {
                    if (ASynchroneHost.ContainsKey(tag.UpdateRate) == false)
                    {
                        ASynchroneHost.Add(tag.UpdateRate, new Dictionary<string, List<Object>> ());
                        ASynchroneHost[tag.UpdateRate].Add("TagList", new List<Object>());
                        ASynchroneHost[tag.UpdateRate].Add("Ip_Address", new List<Object>());
                        ASynchroneHost[tag.UpdateRate]["TagList"].Add(tag);
                        ASynchroneHost[tag.UpdateRate]["Ip_Address"].Add(tag.Ip_Address);



                    }
                    else
                    {
                        ASynchroneHost[tag.UpdateRate]["TagList"].Add(tag);
                        ASynchroneHost[tag.UpdateRate]["Ip_Address"].Add(tag.Ip_Address);
                    }
                }
         
            }
              Initialiaze_Test_Connection();


        }
        public static Dictionary<String, bool> Temp_Test_Connection_Async_Host = new Dictionary<String, bool>();
       
        public static async Task PingAsyncHosts(List<object> addresses,string updateRate,int update)
        {
            while (true)
            {   //Date before Ping Hosts
                DateTime DateD = DateTime.Now;
                var pingTasks = addresses.Select(address =>
                {
                    return new Ping().SendPingAsync(address.ToString());
                });

                await Task.WhenAll(pingTasks);


                StringBuilder pingResultBuilder = new StringBuilder();

                foreach (var pingReply in pingTasks)
                {
                    pingResultBuilder.Append(pingReply.Result.Address);
                    Temp_Test_Connection_Async_Host[pingReply.Result.Address.ToString()] = true;

                }

                foreach (var item in addresses)
                {
                    if (Temp_Test_Connection_Async_Host[item.ToString()] == true)
                    {
                        WorkerLogger.TraceLog(MessageType.Debug, " Host with address: " + item + " is connected");
                        Temp_Test_Connection_Async_Host[item.ToString()] = false;


                    }
                    else
                    {
                        WorkerLogger.TraceLog(MessageType.Debug, " Host with address: " + item + " is not connected");
                    }

                }

                Clear_Test_Connection();
                //Date after test connection
                DateTime DateF = DateTime.Now;
                //We have to subtract the time of traitement from the updateRate 
                //If the traitement time was higher than the updateRate we had to excute the next ping without waiting 
                if (update - (DateD.Second - DateF.Second) > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(update - (DateD.Second - DateF.Second)));
                }
                //we have to set the test_connection values to false
                Clear_Test_Connection();

            }
        }

        public static void PingSynchHost(string Ip_Address,int timeout,Boolean DontFragment,string data)
        {
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();

            // Use the default Ttl value which is 128,
            // but change the fragmentation behavior.
            options.DontFragment = DontFragment;

            // Create a buffer of 32 bytes of data to be transmitted.
    
            byte[] buffer = Encoding.ASCII.GetBytes(data);
        

            try
            {
                PingReply reply = pingSender.Send(Ip_Address, timeout, buffer, options);
                if (reply.Status == IPStatus.Success)
                {
                    Console.WriteLine("Host:: " + Ip_Address + " is connected");
                 
                }
                else { Console.WriteLine("Host:: " + Ip_Address + " is not connected"); }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception:: " + ex.Message);
            }
        }
        public static bool PingPort(string hostUri, int portNumber)
        {
            try
            {
                using (var client = new TcpClient(hostUri, portNumber))
                    Console.WriteLine("Port " + portNumber + " Connected");
                return true;
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.ErrorCode);
                Console.WriteLine("Error pinging host:'" + hostUri + ":" + portNumber.ToString() + "'");
                Console.WriteLine(ex.Message);
                return false;
            }
        }
        public static void Initialiaze_Test_Connection()
        {
            foreach (var grp in ASynchroneHost.Keys)
            {
                foreach (var address in ASynchroneHost[grp]["Ip_Address"])
                {
                    Temp_Test_Connection_Async_Host.Add(address.ToString(), false);
                }

            }
        }
        public static void Clear_Test_Connection()
        {

            foreach (var grp in ASynchroneHost.Keys)
            {
                foreach (var address in ASynchroneHost[grp]["Ip_Address"])
                {
                    Temp_Test_Connection_Async_Host[address.ToString()] = false;
                }



            }
        }
        public static void ListenOnRequest(ZMQResponse zmqresp)
        {

            //Thread Listening = new Thread(() =>
            //{
            //    string strError = string.Empty;

            //    while (true)
            //    {
            //        try
            //        {
            //            string message = zmqresp.ReceiveRequest(out strError);
            //            JObject js = JObject.Parse(message);

            //            //ConcurrentDictionary<string, SubStatus> Subscribers = new ConcurrentDictionary<string, SubStatus>();
            //            //ConcurrentDictionary<string, List<string>> OfflineSub = new ConcurrentDictionary<string, List<string>>();
            //            switch (js["type"].ToString())
            //            {
            //                case "browse":
            //                    {
            //                        zmqresp.SendResponse(JsonConvert.SerializeObject(BacnetDevice.AddressSpace), out strError);
            //                    }
            //                    break;

            //                case "ReadInitialValue":
            //                    {
            //                        List<string> list_sub = js["tags"].Values<string>().ToList();
            //                        bool bfull = bool.Parse(js["AllMonitoredTags"].ToString());
            //                        Dictionary<string, Dictionary<string, object>> DicInitValues = new Dictionary<string, Dictionary<string, object>>();
            //                     /*   foreach (string item in BacnetDevice.InitialValues.Keys)
            //                        {

            //                           //     DicInitValues.Add(item, BacnetDevice.InitialValues[item]);

            //                        }*/
            //                        zmqresp.SendResponse(JsonConvert.SerializeObject(DicInitValues,
            //                              Formatting.None,
            //                              new JsonSerializerSettings
            //                              {
            //                                  NullValueHandling = NullValueHandling.Ignore
            //                              }), out strError);
            //                    }
            //                    break;
            //                default:
            //                    {
            //                        zmqresp.SendResponse("Command type not valid", out strError);
            //                    }
            //                    break;
            //            }
            //        }
            //        catch (Exception ex)
            //        {
            //            WorkerLogger.TraceLog(MessageType.Error, "Failed to respond to request." + ex.Message);
            //            zmqresp.SendResponse("Failed to respond to request." + ex.Message, out strError);
            //            return;
            //        }

            //    }

            //});
            //Listening.Start();
        }



        //Prepare the Que before publish
        public void PrepareQue()
        {
            //try
            //{
            //    PublisherClass itemList = new PublisherClass();
            //    foreach (BacnetReadAccessResult tag in res)
            //    {

            //        try
            //        {
            //            Tag tagFound = new Tag();
            //            tagFound = null;
            //            FindTag(tag, bloc, out tagFound);
            //            if (tagFound != null)
            //            {
            //                int i = 0;


            //                //Add all properties to values
            //                Dictionary<string, Object> ListOfValues = new Dictionary<string, Object>();
            //                while (i < tag.values.Count)
            //                {
            //                    Item element = new Item();
            //                    DateTime localDate = DateTime.Now;
            //                    element.TimeStamp = localDate.ToString();
            //                    element.Value = tag.values[i].value[0].Value;

            //                    element.TagName = tagFound.TageKey +"/"+ tag.values[i].property.ToString();
            //                    itemList.Payload.Add(element);

            //                    i++;
            //                }
            //                itemList.SchemaId = IOHelper.AgentConfig.Agent_SchemaID;//schema id ?




            //                WorkerLogger.TraceLog(MessageType.Debug, "Collecting data from " + tagFound.TageKey + " succeeded");
            //            }

            //        }
            //        catch (Exception ex)
            //        {

            //            WorkerLogger.TraceLog(MessageType.Error, ex.Message);
            //        }

            //    }
            //    Publisherqueue.Add(itemList);

            //}
            //catch
            //{ WorkerLogger.TraceLog(MessageType.Error, "Cannot read data from bloc " + bloc.Name); }


        }






        public void WriteItem(string ItemName, object Value)
        {



        }
    }

}
