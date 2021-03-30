using IntegrationObjects.Agents.Common;
using IntegrationObjects.Logger.SDK;
using IntegrationObjects.SIOTHAPI.Messaging.ReqRespPattern;
using IntegrationObjects.SIOTHConnectionConnector.Helper;
using IntegrationObjects.SIOTHConnectorName.Helper;
using Nancy.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        public static BlockingCollection<PublisherClass> Publisherqueue = new BlockingCollection<PublisherClass>();
        public static Dictionary<int, List<Tag>> SynchroneHost = new Dictionary<int, List<Tag>>();
        public static List<string> c = new List<string>();
        // public static Dictionary<int, Dictionary<string, List<Object> >> ASynchroneHost = new Dictionary<int, Dictionary<string, List<Object>>>();
        public static Dictionary<int, ASynchroneHost> ASynchroneHost = new Dictionary<int, ASynchroneHost>();
        public static Dictionary<String, bool> Temp_Test_Connection_Async_Host = new Dictionary<String, bool>();


        /// <summary>This method load the devices configuration from the config file into a dictionary that contains all the information needed to read and write data


        /// <summary> this method Check whether the Hosts are connected or not///<example>
        /// For example:///<code>/// ConnectToBACnetDevices();///results///Device BasicServer is not connected, Device RoomSimulator is connected
        /// ConnectToBACnetDevices():Check the connectivity for the first time
        ///this method check periodicaly the connectivity of multiple hosts
        public void CheckSyncHostStatus()
        {
            
            try
            {
                Parallel.ForEach(SynchroneHost.Keys, item =>
                {
                    while (true) {
                        PublisherClass itemList = new PublisherClass();
                        foreach (Tag tag in SynchroneHost[item])
                        {
                            WorkerManager.PingSynchHost(tag, itemList);
                        }
                        itemList.SchemaId = IOHelper.AgentConfig.Agent_SchemaID;
                        Publisherqueue.Add(itemList);
                        Thread.Sleep(item);
                    }
                    
                });
            }
            catch (Exception ex)
            {
                WorkerLogger.TraceLog(MessageType.Error, ex.Message);
            }
           
                   }
        public void CheckASyncHostStatus()
        {
            try
            {
                Parallel.ForEach(ASynchroneHost.Keys, item => {
                   PingAsyncHosts(ASynchroneHost[item], item.ToString(), Convert.ToInt32(item)).Wait();
                });
            }
            catch(Exception ex)
            {
                WorkerLogger.TraceLog(MessageType.Error, ex.Message);
            }
        
        }
        public void ConnectPortStatus()
        {
            while (true)
            {
                //  PingPort("192.168.1.52", 47809);
                var client = new UdpClient();
                IPAddress address = IPAddress.Parse("127.0.0.1");
                Byte[] bytes = address.GetAddressBytes();

                var serverEndPoint = new IPEndPoint(address, 4201);
                client.Connect(serverEndPoint);

                IPEndPoint remoteEndPoint = null;   // this will always be equal to serverEndPoint in our example
                while (true)
                {
                    try
                    {
                        Console.WriteLine("Sending ping to server");
                        client.Send(Array.Empty<byte>(), 0);

                        Thread.Sleep(500);
                        try { }catch(Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        _ = client.Receive(ref remoteEndPoint);
                        Console.WriteLine("Received pong from server");
                    }
                    catch(SocketException ex)
                    {
                        Console.WriteLine(ex.ErrorCode + ":: " + ex.Message);
                    }
                 
                }

                Thread.Sleep(2000);
            }
            

        }
        public static void LoadConfig()
        {
            try
            {
                foreach (Tag tag in IOHelper.AgentConfig.dataExtractionProperties.DataConfig.DataConfiguration.Tags)
                {
                    if (tag.OnlyHostPing == true && tag.Synchronous == true)
                    {
                        if (SynchroneHost.ContainsKey(tag.UpdateRate) == false)
                        {
                            SynchroneHost.Add(tag.UpdateRate, new List<Tag>());
                            SynchroneHost[tag.UpdateRate].Add(tag);
                        }
                        else
                        {
                            SynchroneHost[tag.UpdateRate].Add(tag);
                        }
                    }
                    else if (tag.OnlyHostPing == false && tag.Synchronous == true)
                    {
                        if (SynchroneHost.ContainsKey(tag.UpdateRate) == false)
                        {
                            SynchroneHost.Add(tag.UpdateRate, new List<Tag>());
                            SynchroneHost[tag.UpdateRate].Add(tag);
                        }
                        else
                        {
                            SynchroneHost[tag.UpdateRate].Add(tag);
                        }
                    }
                    else if (tag.OnlyHostPing == true && tag.Synchronous == false)
                    {
                        if (ASynchroneHost.ContainsKey(tag.UpdateRate) == false)
                        {
                            List<Tag> TagList = new List<Tag>();
                             List<string> Ip_Address = new List<string>();
                               ASynchroneHost.Add(tag.UpdateRate, new ASynchroneHost(TagList,Ip_Address));
                            ASynchroneHost[tag.UpdateRate].Ip_Address.Add(tag.Ip_Address);
                            ASynchroneHost[tag.UpdateRate].TagList.Add(tag);
                        

                        }
                        else
                        {
                            ASynchroneHost[tag.UpdateRate].Ip_Address.Add(tag.Ip_Address);
                            ASynchroneHost[tag.UpdateRate].TagList.Add(tag);
                        }
                    }

                }
                Initialiaze_Test_Connection();
                WorkerLogger.TraceLog(MessageType.Control, "Load  dictionnary configuration succeeded");
            }
            catch(Exception ex)
            {
                WorkerLogger.TraceLog(MessageType.Error, ex.Message);
            }
                   }
        
       //Ping Hosts asynchronously
        public static async Task PingAsyncHosts(ASynchroneHost addresses,string updateRate,int update)
        {
            while (true)
            { 
                //Date before Ping Hosts
                DateTime DateD = DateTime.Now;
                PublisherClass itemList = new PublisherClass();
                //
                var pingTasks = addresses.Ip_Address.Select(address =>
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

                foreach (var item in addresses.TagList)
                {
                    Item element = new Item();
                    element.Address = item.Ip_Address;
                    element.TagName = item.TagName;
                    DateTime localDate = DateTime.Now;
                    element.TimeStamp = localDate;
           
                  
                    if (Temp_Test_Connection_Async_Host[item.Ip_Address] == true)
                    {
                        WorkerLogger.TraceLog(MessageType.Debug, " Host with address: " + item + " is connected");
                        element.Status = true;
                        item.Status = true;
                     }
                    else
                    {
                        WorkerLogger.TraceLog(MessageType.Debug, " Host with address: " + item + " is not connected");
                        element.Status = false;
                        item.Status = false;
                      }
                    itemList.Payload.Add(element);
                  
                }
                Publisherqueue.Add(itemList);



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

        public static void PingSynchHost(Tag tag, PublisherClass itemList)
        {
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();

            // Use the default Ttl value which is 128,
            // but change the fragmentation behavior.
            options.DontFragment = tag.DontFragment;

            // Create a buffer of 32 bytes of data to be transmitted.
    
            byte[] buffer = Encoding.ASCII.GetBytes(tag.Data);
        

            try
            {
                Item element = new Item();
                DateTime localDate = DateTime.Now;
                element.TimeStamp = localDate;
                element.TagName =tag.TagName + "/" + tag.Ip_Address;
                PingReply reply = pingSender.Send(tag.Ip_Address, tag.Connection_Timeout, buffer, options);
                if (reply.Status == IPStatus.Success)
                {
                    WorkerLogger.TraceLog(MessageType.Debug, tag.Ip_Address + " is connected");
                    element.Status = true;
                    tag.Status = true;
                    
                }
                else { WorkerLogger.TraceLog(MessageType.Debug, tag.Ip_Address + " is not connected");
                    element.Status = false;
                    tag.Status = false;
                }
                itemList.Payload.Add(element);
            }
            catch (Exception ex)
            {
                WorkerLogger.TraceLog(MessageType.Error, ex.Message);
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
            //    Console.WriteLine(ex.ErrorCode);
            //    Console.WriteLine("Error pinging host:'" + hostUri + ":" + portNumber.ToString() + "'");
            //    Console.WriteLine(ex.Message);
                switch (ex.ErrorCode)
                {
                    case 10054: Console.WriteLine("Port "+portNumber+" closed or blocked by firewall");
                        break;
                    case 10048: Console.WriteLine("Address already in use");
                        break;
                    case 10051:Console.WriteLine("Network is unreachable");
                        break;
                    case 10050:Console.WriteLine("Network is down");
                        break;
                    case 10056:Console.WriteLine("Socket is already connected");
                        break;
                    case 10060:Console.WriteLine("Connection timed out");
                        break;
                    case 10061:Console.WriteLine("Connection refused::No server application is running");
                        break;
                    case 10064:Console.WriteLine("Host is down");
                        break;
                    default:Console.WriteLine("CodeError: "+ex.ErrorCode+":: "+ex.Message);
                        break;
                }
                return false;
            }
        }
        public static void Initialiaze_Test_Connection()
        {
            foreach (var grp in ASynchroneHost.Keys)
            {
                foreach (var address in ASynchroneHost[grp].Ip_Address)
                {
                    Temp_Test_Connection_Async_Host.Add(address.ToString(), false);
                }

            }
        }
        public static void Clear_Test_Connection()
        {

            foreach (var grp in ASynchroneHost.Keys)
            {
                foreach (var address in ASynchroneHost[grp].Ip_Address)
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

            Console.WriteLine(ItemName+"::"+Value);

        }
    }

}
