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
        //public void ReceiveMessage(string ip_address,int port)
        //{
        //    using (var udpClient = new UdpClient(ip_address,port))
        //    {
        //        IPAddress address = IPAddress.Parse(ip_address);

        //        IPEndPoint RemoteIpEndPoint = new IPEndPoint(address, port);
        //        Byte[] sendBytes = Encoding.ASCII.GetBytes("?");
        //        udpClient.Connect(ip_address, port);
        //        udpClient.Send(sendBytes, sendBytes.Length);
        //        while (true)
        //        {
        //            var receivedResult = udpClient.ReceiveAsync();
        //            Console.WriteLine("Port"+port+"Connected");

        //        }
        //    }
        //}
        public async Task ReceiveMessage(string ip_address, int port)
        {
            using (var udpClient = new UdpClient(ip_address, port))
            {
                IPAddress address = IPAddress.Parse(ip_address);

                IPEndPoint RemoteIpEndPoint = new IPEndPoint(address, port);
                Byte[] sendBytes = Encoding.ASCII.GetBytes("?");
                udpClient.Connect(ip_address, port);
                udpClient.Send(sendBytes, sendBytes.Length);

                var receivedResult = udpClient.ReceiveAsync().Wait(3000);
                // Console.Write(Encoding.ASCII.GetString(receivedResult.Buffer));
                if (receivedResult == true)
                {
                    Console.WriteLine(port + " connected");
                }
                else
                {
                    Console.WriteLine(port + "not connected");
                }
                udpClient.Close();
                Console.WriteLine("Connection closed");
                await Task.Delay(1);

            }
        }
        public void ConnectPortTcpSync()
        {
            Parallel.ForEach(SynchronePortTcp.Keys, updateRate =>
            {
                while (true)
                {
                    foreach (Tag tag in SynchronePortTcp[updateRate])
                    {

                        s_cts.CancelAfter(tag.Connection_Timeout);
                        PingTcpPortSync(tag).Wait();
                        if (tag.is_open == true)
                        {
                            tag.PortDescription = tag.Ip_Address + ":: " + tag.Port + ":: is open";
                        }
                    }
                    Thread.Sleep(updateRate);
                }

            });
         }
        public void ConnectPortTcpASync()
        {
            Parallel.ForEach(ASynchronePortTcp.Keys, updateRate =>
            {
                DateTime DateD = DateTime.Now;
                PublisherClass itemList = new PublisherClass();
                while (true)
    {
                    foreach (Tag tag in ASynchronePortTcp[updateRate])
                    {
                        Item element = new Item();
                        element.Address = tag.Ip_Address;
                        element.TagName = tag.TagName;
                        DateTime localDate = DateTime.Now;
                        element.TimeStamp = localDate;

                        s_cts.CancelAfter(tag.Connection_Timeout);
                        Task.Run(() => PingTcpPortSync(tag));
                        if (tag.is_open == true)
                        {
                            tag.PortDescription = tag.Ip_Address + ":: " + tag.Port + ":: is open";
                            element.is_open = true;
                        }
                        else { element.is_open = false; }

            Console.WriteLine(tag.PortDescription);
        }
        Thread.Sleep(updateRate);
    }

});

        }
        public void ConnectPortUdp()
        {

        }
        public static async Task PingTcpPortSync(Tag tag)
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
        public static bool PingPortAsync(string hostUri, int portNumber)
        {
            try
            {
                //    Socket uSocket = udpClient.Client;
                //    uSocket.ReceiveTimeout = 10000;
                string address = hostUri;
                int port = portNumber;
                int connectTimeoutMilliseconds = 3000;

                var tcpClient = new TcpClient();
                var connectionTask = tcpClient
                    .ConnectAsync(address, port).ContinueWith(task =>
                    {
                        return task.IsFaulted ? null : tcpClient;
                    }, TaskContinuationOptions.ExecuteSynchronously);
                var timeoutTask = Task.Delay(connectTimeoutMilliseconds)
                    .ContinueWith<TcpClient>(task => null, TaskContinuationOptions.ExecuteSynchronously);
                var resultTask = Task.WhenAny(connectionTask, timeoutTask).Unwrap();

                resultTask.Wait();
                var resultTcpClient = resultTask.Result;
                // Or shorter by using `await`:
                // var resultTcpClient = await resultTask;

                if (resultTcpClient != null)
                {
                    // Connected!
                    Console.WriteLine("Port " + port + " Connected ");
                }
                else
                {
                    // Not connected
                    Console.WriteLine("Port " + port + " Not connected ");
                }
                return true;
            }
            catch (AggregateException ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            catch (ObjectDisposedException ex0)
            {
                Console.WriteLine(ex0.Message);
                return false;
            }
            catch (SocketException ex)
            {
                switch (ex.ErrorCode)
                {
                    case 10054:
                        Console.WriteLine("Port " + portNumber + " closed or blocked by firewall");
                        break;
                    case 10048:
                        Console.WriteLine("Address already in use");
                        break;
                    case 10051:
                        Console.WriteLine("Network is unreachable");
                        break;
                    case 10050:
                        Console.WriteLine("Network is down");
                        break;
                    case 10056:
                        Console.WriteLine("Socket is already connected");
                        break;
                    case 10060:
                        Console.WriteLine("Connection timed out, the connection is maybe blocked by the firewall");
                        break;
                    case 10061:
                        Console.WriteLine("Connection refused::No server application is running");
                        break;
                    case 10064:
                        Console.WriteLine("Host is down");
                        break;
                    default:
                        Console.WriteLine("CodeError: " + ex.ErrorCode + ":: " + ex.Message);
                        break;
                }
                return false;
            }
        }
        public void PingPortUdp(string Address, int Port, IPEndPoint RemoteIpEndPoint, Byte[] sendBytes, short Ttl)
        {
            try
            {

                UdpClient udpClient = new UdpClient(Port);
                udpClient.Ttl = Ttl;
                Socket uSocket = udpClient.Client;
                uSocket.ReceiveTimeout = 5000;
                udpClient.Connect(Address, Port);
                // IPAddress address = IPAddress.Parse("192.168.43.190");
                //  IPEndPoint RemoteIpEndPoint = new IPEndPoint(address, Port);
                //  Byte[] sendBytes = Encoding.ASCII.GetBytes("?");
                udpClient.Send(sendBytes, sendBytes.Length);
                udpClient.ReceiveAsync();
                //   Byte[] receiveBytes = udpClient.Receive(ref RemoteIpEndPoint);
                Console.WriteLine("Port: " + Port + " Open");
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.ErrorCode + "::" + e.Message);
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
                    else if (tag.OnlyHostPing == false && tag.Synchronous == true && tag.PortType == "TCP")
                    {
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
                    else if (tag.OnlyHostPing == true && tag.Synchronous == false)
                    {
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
                    element.TagName = item.TagName;
                    DateTime localDate = DateTime.Now;
                    element.TimeStamp = localDate;


                    if (Temp_Test_Connection_Async_Host[item.Ip_Address] == true)
                    {
                        WorkerLogger.TraceLog(MessageType.Debug, " Host with address: " + item + " is connected");
                        element.is_open = true;
                        item.is_open = true;
                    }
                    else
                    {
                        WorkerLogger.TraceLog(MessageType.Debug, " Host with address: " + item + " is not connected");
                        element.is_open = false;
                        item.is_open = false;
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
            //try
            //{

            //}
            //catch (Exception)
            //{
            //    WorkerLogger.TraceLog()      
            //}
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
                element.TagName = tag.TagName + "/" + tag.Ip_Address;
                PingReply reply = pingSender.Send(tag.Ip_Address, tag.Connection_Timeout, buffer, options);
                if (reply.Status == IPStatus.Success)
                {
                    WorkerLogger.TraceLog(MessageType.Debug, tag.Ip_Address + " is connected");
                    element.is_open = true;
                    tag.is_open = true;

                }
                else
                {
                    WorkerLogger.TraceLog(MessageType.Debug, tag.Ip_Address + " is not connected");
                    element.is_open = false;
                    tag.is_open = false;
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

            Console.WriteLine(ItemName + "::" + Value);

        }
    }

}
