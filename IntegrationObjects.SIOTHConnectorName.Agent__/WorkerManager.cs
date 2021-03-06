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
        public static Dictionary<int, List<Tag>> SynchronePortTcp = new Dictionary<int, List<Tag>>();
        public static Dictionary<int, List<Tag>> ASynchronePortTcp = new Dictionary<int, List<Tag>>();
        public static Dictionary<int, List<Tag>> SynchronePortUdp = new Dictionary<int, List<Tag>>();
        public static Dictionary<int, List<Tag>> ASynchronePortUdp = new Dictionary<int, List<Tag>>();
        public static CancellationTokenSource s_cts = new CancellationTokenSource();

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
                    while (true)
                    {
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
                Parallel.ForEach(ASynchroneHost.Keys, item =>
                {
                    PingAsyncHosts(ASynchroneHost[item], item.ToString(), Convert.ToInt32(item)).Wait();
                });
            }
            catch (Exception ex)
            {
                WorkerLogger.TraceLog(MessageType.Error, ex.Message);
            }

        }

     
        public void ConnectPortTcpSync()
        {
            Parallel.ForEach(SynchronePortTcp.Keys, updateRate =>
            {
                while (true)
                {
                    PublisherClass itemList = new PublisherClass();
                    foreach (Tag tag in SynchronePortTcp[updateRate])
                    {
                        Item element = new Item();
                        element.Address = tag.Ip_Address + ":" + tag.Port;
                        element.TagName = tag.TagKey;

                        s_cts.CancelAfter(tag.Connection_Timeout);
                        PingTcpPort(tag).Wait();
                        if (tag.is_open == true)
                        {
                            tag.PortDescription = tag.Ip_Address + ":: " + tag.Port + ":: is open";
                            tag.is_open = true;
                            element.is_open = true;
                        }
                        else
                        {
                            tag.is_open = false;
                            if (tag.PortDescription == null)
                            {
                                tag.PortDescription = "Cannot establish a Tcp connection with " + element.Address;
                            }
                        }
                        element.Description = tag.PortDescription;
                        itemList.Payload.Add(element);
                    }
                    itemList.SchemaId = IOHelper.AgentConfig.Agent_SchemaID;
                    Publisherqueue.Add(itemList);
                    Thread.Sleep(updateRate);
                }

            });
        }
        public void ConnectPortTcpASync()
        {
            Parallel.ForEach(ASynchronePortTcp.Keys, updateRate =>
            {

                while (true)
                {
                    DateTime DateD = DateTime.Now;
                    PublisherClass itemList = new PublisherClass();
                    foreach (Tag tag in ASynchronePortTcp[updateRate])
                    {
                        Item element = new Item();
                        element.Address = tag.Ip_Address;
                        element.TagName = tag.TagName;
                        DateTime localDate = DateTime.Now;
                        element.TimeStamp = localDate;

                        s_cts.CancelAfter(tag.Connection_Timeout);
                        Task.Run(() => PingTcpPort(tag));
                        if (tag.is_open == true)
                        {
                            tag.PortDescription = tag.Ip_Address + ":: " + tag.Port + ":: is open";
                            element.is_open = true;
                        }
                        else
                        {
                            element.is_open = false;
                            if (tag.PortDescription == null)
                            {
                                tag.PortDescription = "Cannot establish Tcp Connection to " + tag.Ip_Address + ":" + tag.Port;
                            }
                        }
                        element.Description = tag.PortDescription;


                        itemList.Payload.Add(element);
                    }
                    itemList.SchemaId = IOHelper.AgentConfig.Agent_SchemaID;
                    Publisherqueue.Add(itemList);
                    Thread.Sleep(updateRate);
                }

            });

        }
        public void ConnectPortUdpASync()
        {
            Byte[] sendBytes = Encoding.ASCII.GetBytes("?");
            while (true)
            {
                PublisherClass itemList = new PublisherClass();
                Parallel.ForEach(ASynchronePortUdp.Keys, updateRate =>
                {
                    foreach (Tag tag in ASynchronePortUdp[updateRate])
                    {
                        Task.Run(() => PingUdpPort(tag, sendBytes));
                        Item element = new Item();
                        DateTime DateD = DateTime.Now;
                        element.Address = tag.Ip_Address + ":" + tag.Port;
                        element.TagName = tag.TagKey;
                        element.TimeStamp = DateD;
                        if (tag.is_open == true)
                        {
                            element.is_open = true;
                        }
                        else
                        {
                            element.is_open = false;
                        }
                        element.Description = tag.PortDescription;

                        itemList.Payload.Add(element);

                    }
                    itemList.SchemaId = IOHelper.AgentConfig.Agent_SchemaID;
                    Publisherqueue.Add(itemList);
                    Thread.Sleep(updateRate);
                });

            }


        }
        public void ConnectPortUdpSync()
        {
            Byte[] sendBytes = Encoding.ASCII.GetBytes("?");
            while (true)
            {
                PublisherClass itemList = new PublisherClass();
                Parallel.ForEach(SynchronePortUdp.Keys, updateRate =>
                {
                    foreach (Tag tag in SynchronePortUdp[updateRate])
                    {
                        Item element = new Item();
                        DateTime DateD = DateTime.Now;
                        element.Address = tag.Ip_Address + ":" + tag.Port;
                        element.TagName = tag.TagKey;
                        element.TimeStamp = DateD;
                        PingUdpPort(tag, sendBytes).Wait();
                        if (tag.is_open == true)
                        {
                            element.is_open = true;
                        }
                        else
                        {
                            element.is_open = false;
                        }
                        element.Description = tag.PortDescription;

                        itemList.Payload.Add(element);

                    }
                    itemList.SchemaId = IOHelper.AgentConfig.Agent_SchemaID;
                    Publisherqueue.Add(itemList);
                    Thread.Sleep(updateRate);
                });
            }


        }
        public static async Task PingTcpPort(Tag tag)
        {
            try
            {

                var client = new TcpClient();
                await client.ConnectAsync(tag.Ip_Address, tag.Port);
                tag.is_open = true;
            }
            catch (SocketException ex)
            {
                switch (ex.ErrorCode)
                {
                    case 10054:
                        WorkerLogger.TraceLog(MessageType.Debug, tag.PortDescription);
                        tag.PortDescription = tag.Ip_Address + ":" + tag.Port + ":: " + "is closed or connection blocked by firewall";
                        break;
                    case 10048:
                        tag.PortDescription = "Port " + tag.Port + ":: " + "Address already in use";
                        WorkerLogger.TraceLog(MessageType.Debug, tag.PortDescription);
                        break;
                    case 10051:
                        tag.PortDescription = "Port " + tag.Port + "::" + "Network is unreachable";
                        WorkerLogger.TraceLog(MessageType.Debug, tag.PortDescription);
                        break;
                    case 10050:
                        tag.PortDescription = tag.Ip_Address + ":" + tag.Port + "::" + "Network is down";
                        WorkerLogger.TraceLog(MessageType.Debug, tag.PortDescription);
                        break;
                    case 10056:
                        tag.PortDescription = tag.Ip_Address + ":" + tag.Port + "::" + "Socket is already connected";
                        WorkerLogger.TraceLog(MessageType.Debug, tag.PortDescription);
                        break;
                    case 10060:
                        tag.PortDescription = tag.Ip_Address + ":" + tag.Port + "::" + "Connection timed out, the connection is maybe blocked by the firewall";
                        WorkerLogger.TraceLog(MessageType.Debug, tag.PortDescription);
                        break;
                    case 10061:
                        tag.PortDescription = tag.Ip_Address + ":" + tag.Port + ":" + "Connection refused::No server application is running";
                        WorkerLogger.TraceLog(MessageType.Debug, tag.PortDescription);
                        break;
                    case 10064:
                        tag.PortDescription = tag.Ip_Address + ":" + tag.Port + "::" + "Host is down";
                        WorkerLogger.TraceLog(MessageType.Debug, tag.PortDescription);
                        break;
                    default:
                        tag.PortDescription = tag.Ip_Address + ":" + tag.Port + "::" + "CodeError: " + ex.ErrorCode + ":: " + ex.Message;
                        WorkerLogger.TraceLog(MessageType.Debug, tag.PortDescription);
                        break;
                }

            }
        }

        public static async Task PingUdpPort(Tag tag, Byte[] sendBytes)
        {
            try
            {
                UdpClient udpClient = new UdpClient(tag.Port);
                Socket uSocket = udpClient.Client;
                uSocket.ReceiveTimeout = 5000;
                udpClient.Connect(tag.Ip_Address, tag.Port);
                udpClient.Send(sendBytes, sendBytes.Length);
                IPEndPoint RemoteIpEndPoint = tag.RemoteIpEndPoint;
                Byte[] receiveBytes = udpClient.Receive(ref RemoteIpEndPoint);
                tag.is_open = true;
                tag.PortDescription = tag.Ip_Address + ":" + tag.Port + " is open";
                udpClient.Close();

            }
            catch (SocketException ex)
            {
                tag.is_open = false;

                switch (ex.ErrorCode)
                {
                    case 10054:
                        WorkerLogger.TraceLog(MessageType.Debug, tag.PortDescription);
                        tag.PortDescription = tag.Ip_Address + ":" + tag.Port + ":: " + "is closed or connection blocked by firewall";
                        break;
                    case 10048:
                        tag.PortDescription = "Port " + tag.Port + ":: " + "Address already in use";
                        WorkerLogger.TraceLog(MessageType.Debug, tag.PortDescription);
                        break;
                    case 10051:
                        tag.PortDescription = "Port " + tag.Port + "::" + "Network is unreachable";
                        WorkerLogger.TraceLog(MessageType.Debug, tag.PortDescription);
                        break;
                    case 10050:
                        tag.PortDescription = tag.Ip_Address + ":" + tag.Port + "::" + "Network is down";
                        WorkerLogger.TraceLog(MessageType.Debug, tag.PortDescription);
                        break;
                    case 10056:
                        tag.PortDescription = tag.Ip_Address + ":" + tag.Port + "::" + "Socket is already connected";
                        WorkerLogger.TraceLog(MessageType.Debug, tag.PortDescription);
                        break;
                    case 10060:
                        tag.PortDescription = tag.Ip_Address + ":" + tag.Port + "::" + "Connection timed out, the connection is maybe blocked by the firewall";
                        WorkerLogger.TraceLog(MessageType.Debug, tag.PortDescription);
                        break;
                    case 10061:
                        tag.PortDescription = tag.Ip_Address + ":" + tag.Port + ":" + "Connection refused::No server application is running";
                        WorkerLogger.TraceLog(MessageType.Debug, tag.PortDescription);
                        break;
                    case 10064:
                        tag.PortDescription = tag.Ip_Address + ":" + tag.Port + "::" + "Host is down";
                        WorkerLogger.TraceLog(MessageType.Debug, tag.PortDescription);
                        break;
                    default:
                        tag.PortDescription = tag.Ip_Address + ":" + tag.Port + "::" + "CodeError: " + ex.ErrorCode + ":: " + ex.Message;
                        WorkerLogger.TraceLog(MessageType.Debug, tag.PortDescription);
                        break;
                }

            }

        }



        //Ping Hosts asynchronously
        public static async Task PingAsyncHosts(ASynchroneHost addresses, string updateRate, int update)
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
                    element.TagName = item.TagKey;
                    DateTime localDate = DateTime.Now;
                    element.TimeStamp = localDate;


                    if (Temp_Test_Connection_Async_Host[item.Ip_Address] == true)
                    {
                        WorkerLogger.TraceLog(MessageType.Debug, " Host with address: " + item + " is connected");
                        element.is_open = true;
                        item.is_open = true;
                        element.Description = "Host with ip: " + item.Ip_Address + "is open";
                    }
                    else
                    {
                        WorkerLogger.TraceLog(MessageType.Debug, " Host with address: " + item + " is not connected");
                        element.is_open = false;
                        item.is_open = false;
                        element.Description = "Host with ip: " + item.Ip_Address + "is down or cannot be reachable";
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
                element.TagName = tag.TagKey;
                PingReply reply = pingSender.Send(tag.Ip_Address, tag.Connection_Timeout, buffer, options);
                if (reply.Status == IPStatus.Success)
                {
                    WorkerLogger.TraceLog(MessageType.Debug, tag.Ip_Address + " is connected");
                    element.is_open = true;
                    tag.is_open = true;
                    element.Description = "Host with ip: " + tag.Ip_Address + "is open";
                }
                else
                {
                    WorkerLogger.TraceLog(MessageType.Debug, tag.Ip_Address + " is not connected");
                    element.is_open = false;
                    tag.is_open = false;
                    element.Description = "Host with ip: " + tag.Ip_Address + "is down or cannot be reachable";
                }
                itemList.Payload.Add(element);
            }
            catch (Exception ex)
            {
                WorkerLogger.TraceLog(MessageType.Error, ex.Message);
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
        public static void LoadConfig()
        {
            try
            {
                foreach (Tag tag in IOHelper.AgentConfig.dataExtractionProperties.DataConfig.DataConfiguration.Tags)
                {

                    if (tag.OnlyHostPing == true && tag.Synchronous == true)
                    {
                        tag.TagKey = tag.TagName + "/" + tag.Ip_Address;
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
                    else if (tag.OnlyHostPing == false && tag.Synchronous == true && tag.PortType == "TCP")
                    {
                        tag.TagKey = tag.TagKey = tag.TagName + "/" + tag.Ip_Address + ":" + tag.Port;
                        if (SynchronePortTcp.ContainsKey(tag.UpdateRate) == false)
                        {
                            SynchronePortTcp.Add(tag.UpdateRate, new List<Tag>());
                            SynchronePortTcp[tag.UpdateRate].Add(tag);
                        }
                        else
                        {
                            SynchronePortTcp[tag.UpdateRate].Add(tag);
                        }
                    }
                    else if (tag.OnlyHostPing == false && tag.Synchronous == false && tag.PortType == "TCP")
                    {
                        tag.TagKey = tag.TagKey = tag.TagName + "/" + tag.Ip_Address + ":" + tag.Port;
                        if (ASynchronePortTcp.ContainsKey(tag.UpdateRate) == false)
                        {
                            ASynchronePortTcp.Add(tag.UpdateRate, new List<Tag>());
                            ASynchronePortTcp[tag.UpdateRate].Add(tag);
                        }
                        else
                        {
                            ASynchronePortTcp[tag.UpdateRate].Add(tag);
                        }
                    }
                    else if (tag.OnlyHostPing == false && tag.Synchronous == true && tag.PortType == "UDP")
                    {
                        tag.TagKey = tag.TagKey = tag.TagName + "/" + tag.Ip_Address + ":" + tag.Port;
                        IPAddress address = IPAddress.Parse(tag.Ip_Address);
                        tag.RemoteIpEndPoint = new IPEndPoint(address, tag.Port);
                        if (SynchronePortUdp.ContainsKey(tag.UpdateRate) == false)
                        {
                            SynchronePortUdp.Add(tag.UpdateRate, new List<Tag>());
                            SynchronePortUdp[tag.UpdateRate].Add(tag);
                        }
                        else
                        {
                            SynchronePortUdp[tag.UpdateRate].Add(tag);
                        }
                    }
                    else if (tag.OnlyHostPing == false && tag.Synchronous == false && tag.PortType == "UDP")
                    {
                        tag.TagKey = tag.TagName + "/" + tag.Ip_Address + ":" + tag.Port;
                        IPAddress address = IPAddress.Parse(tag.Ip_Address);
                        tag.RemoteIpEndPoint = new IPEndPoint(address, tag.Port);
                        if (ASynchronePortUdp.ContainsKey(tag.UpdateRate) == false)
                        {
                            ASynchronePortUdp.Add(tag.UpdateRate, new List<Tag>());
                            ASynchronePortUdp[tag.UpdateRate].Add(tag);
                        }
                        else
                        {
                            ASynchronePortUdp[tag.UpdateRate].Add(tag);
                        }
                    }
                    else if (tag.OnlyHostPing == true && tag.Synchronous == false)
                    {
                        tag.TagKey = tag.TagName + "/" + tag.Ip_Address;
                        if (ASynchroneHost.ContainsKey(tag.UpdateRate) == false)
                        {
                            List<Tag> TagList = new List<Tag>();
                            List<string> Ip_Address = new List<string>();
                            ASynchroneHost.Add(tag.UpdateRate, new ASynchroneHost(TagList, Ip_Address));
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
            catch (Exception ex)
            {
                WorkerLogger.TraceLog(MessageType.Error, ex.Message);
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
      
        public void WriteItem(string ItemName, object Value)
        {

            Console.WriteLine(ItemName + "::" + Value);

        }
    }

}
