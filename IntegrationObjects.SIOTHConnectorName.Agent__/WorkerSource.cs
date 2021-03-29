﻿using IntegrationObjects.SIOTHAPI.Messaging.PubSubPattern;
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
                //this.ZMQAddress = ZMQAddress;
                zmqPublisher.InitialisePublisher(IOHelper.AgentConfig.ZMQAddress, out strError, 1000, IOHelper.AgentConfig.ZMQSecurity);
            }
            catch (Exception EX0)
            {
                //Log error
            }

        }
        public void StartCollectingData()
        {
            try
            {
                //To DO Connect To Source Device
             /*   Thread ConnectThread = new Thread(ConnectToBACnetDevices);
                ConnectThread.Name = "Thread To Connect To Device";
                ConnectThread.IsBackground = true;
                ConnectThread.Start();
             */

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
                throw Ex;
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
                Thread.Sleep(1000);
            }

        }
       







        private void PerformGarbageCollection(object obj)
        {
            throw new NotImplementedException();
        }
   
    }
}