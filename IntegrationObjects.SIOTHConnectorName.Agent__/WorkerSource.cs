using IntegrationObjects.SIOTHAPI.Messaging.PubSubPattern;
using IntegrationObjects.SIOTHConnectorName.Helper;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using IntegrationObjects.Agents.Common;
using IntegrationObjects.Logger.SDK;


namespace IntegrationObjects.SIOTHConnectorName.Agent
{
    public class WorkerSource : WorkerManager
    {

        #region "Attributes"
        public WorkerManager workerManager;
        public ZMQPublisher zmqPublisher;
        #endregion
        public WorkerSource()
        {

            try
            {
                string strError = string.Empty;
                zmqPublisher = new ZMQPublisher();
                zmqPublisher.InitialisePublisher(IOHelper.AgentConfig.ZMQAddress, out strError, 1000, IOHelper.AgentConfig.ZMQSecurity);
            }
            catch (Exception ex)
            {
                WorkerLogger.TraceLog(MessageType.Error, ex.Message);
            }

        }
        //Check Host and/or port status
        public void Connection()
        {
            try
            {

                //Check tcp ports sync
                Thread ConnectPortsTcpSyncThread = new Thread(ConnectPortTcpSync);
                ConnectPortsTcpSyncThread.Name = "Thread To Connect To Tcp port Synchronously";
                ConnectPortsTcpSyncThread.IsBackground = true;
                ConnectPortsTcpSyncThread.Start();

                //Check tcp ports async
                Thread ConnectPortsTcpASyncThread = new Thread(ConnectPortTcpASync);
                ConnectPortsTcpASyncThread.Name = "Thread To Connect To Synchronously";
                ConnectPortsTcpASyncThread.IsBackground = true;
                ConnectPortsTcpASyncThread.Start();
                //check udp ports async
                Thread ConnectPortsUdpSyncThread = new Thread(ConnectPortUdpSync);
                ConnectPortsUdpSyncThread.Name = "Thread To Connect To Device";
                ConnectPortsUdpSyncThread.IsBackground = true;
                ConnectPortsUdpSyncThread.Start();

                //check udp ports async
                Thread ConnectPortsUdpASyncThread = new Thread(ConnectPortUdpASync);
                ConnectPortsUdpASyncThread.Name = "Thread To Connect To Device";
                ConnectPortsUdpASyncThread.IsBackground = true;
                ConnectPortsUdpASyncThread.Start();

                //To Check Device Status
                Thread SynchroneHostStatusThread = new Thread(CheckSyncHostStatus);
                SynchroneHostStatusThread.Name = "Thread To Check Host Status Synchronously";
                SynchroneHostStatusThread.IsBackground = true;
                SynchroneHostStatusThread.Priority = ThreadPriority.BelowNormal;
                SynchroneHostStatusThread.Start();
                //To Check Device Status
                Thread ASynchroneHostStatusThread = new Thread(CheckASyncHostStatus);
                ASynchroneHostStatusThread.Name = "Thread To Check Host Status ASynchronously";
                ASynchroneHostStatusThread.IsBackground = true;
                ASynchroneHostStatusThread.Priority = ThreadPriority.BelowNormal;
                ASynchroneHostStatusThread.Start();



                //  <<<=======================================  Start publishing Data To SIOTH  =======================================>>>
                Thread PublisherThread = new Thread(PublishDADataToSIOTHBUS);
                PublisherThread.Name = "Thread To publish data to SIOTH";
                PublisherThread.IsBackground = true;
                PublisherThread.Priority = ThreadPriority.BelowNormal;
                PublisherThread.Start();
          
            }
            catch (Exception Ex)
            {
                WorkerLogger.TraceLog(MessageType.Error, Ex.Message);

            }
        }
        public void PublishDADataToSIOTHBUS(object obj)
        {
          
            WorkerLogger.TraceLog(MessageType.Control, "Starting to publish data .......");
            PublisherClass PItem;
            string strError = string.Empty;

            while (true)
            {
                while (Publisherqueue.TryTake(out PItem))
                {
                    zmqPublisher.PublishData(JsonConvert.SerializeObject(PItem), IOHelper.AgentConfig.Topic, out strError);

                    if (!string.IsNullOrEmpty(strError))
                    {
                        WorkerLogger.TraceLog(MessageType.Error, "Error when publishing data: " + strError);
                    }

                }
                Thread.Sleep(IOHelper.AgentConfig.PublishRate);
            }

        }
       







        private void PerformGarbageCollection(object obj)
        {
            throw new NotImplementedException();
        }
   
    }
}
