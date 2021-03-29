using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IntegrationObjects.Agents.Common;
using IntegrationObjects.Logger.SDK;
using IntegrationObjects.SIOTHAPI.Messaging.ReqRespPattern;

using IntegrationObjects.SIOTHConnectorName.Helper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IntegrationObjects.SIOTHConnectorName.Agent
{
    public class Worker : BackgroundService
    {
        WorkerManager workerManager = new WorkerManager();
        private WorkerSource WorkerSource;
        private WorkerDestination WorkerDestination;

        public ZMQResponse zmqresp = new ZMQResponse();


        public override Task StartAsync(CancellationToken cancellationToken)
        {
            //  <<<=======================================  Initialize Logger  =======================================>>>

            try
            {
                string strError = string.Empty;
                bool initLog = InitilizeLog(out string error);
                if (initLog && String.IsNullOrEmpty(strError))
                {
                    WorkerLogger.TraceLog(MessageType.Control, "Initializing INI configuration was successful");
                }
                else
                {
                    WorkerLogger.TraceLog(MessageType.Error, "Initializing INI configuration failed");
                }
                //if (initLog && String.IsNullOrEmpty(error))
                //{
                //    WorkerLogger.Control(AgentCommonMessages.InitializingIniConfigurationSuccessful);
                //}
                //else
                //{
                //    StopAsync(cancellationToken);
                //}
            }
            catch (Exception)
            {

                //Ignored
            }

            //  <<<=======================================  Load Config And Start The Worker  =======================================>>>
            #region <-------------------------------------------Load Configuration----------------------------------->
            // Load Configuration
            string LoadconfigError;
            string strPathFile = $"AgentConfig\\" + Process.GetCurrentProcess().ProcessName + ".json";
           
            string mappingFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, strPathFile);

            IOHelper.AgentConfig = IOHelper.LoadConfiguration(mappingFilePath, out LoadconfigError);
            
     
           

            if (!string.IsNullOrEmpty(LoadconfigError))
            {
                WorkerLogger.Error($"{CommonMessages.LoadConfigFileFailed} ,[File Name : {Process.GetCurrentProcess().ProcessName + ".json"}], Reason = [{LoadconfigError}]");
                //StopService                   
                //StopAsync(cancellationToken);
            }
            else
            {
                WorkerLogger.Control(CommonMessages.LoadConfigFileSucceeded);
            }
            //  <<<=======================================  Loading Config And Start The Worker  =======================================>>>
            WorkerLogger.Control(CommonMessages.LoadInifileConfig);
            IOHelper.LoadIniFileConfiguration();
            //Initialize Publisher
            if (!string.IsNullOrEmpty(IOHelper.AgentConfig.SIOTHLogZMQAddress))
            {
                WorkerLogger.InitializeSIOTHLoggingPublisher(IOHelper.AgentConfig.SIOTHLogZMQAddress, IOHelper.AgentConfig.ZMQSecurity);
            }
            else
            {
                WorkerLogger.Control("SIOTH Log ZMQ Address is Null or Empty, No Message Log will be Published to the SIOTH");
            }
            #endregion


            //Initialize ConfigDictionnary
            WorkerManager.LoadConfig();

            //Initialize ZmqResponse
            try
            {
              
                String StrError;
                zmqresp.InitialiseZMQResponse(IOHelper.AgentConfig.ZMQListeningOnRequest, out StrError, IOHelper.AgentConfig.ZMQSecurity);
                if (!string.IsNullOrEmpty(StrError))
                {
                    WorkerLogger.TraceLog(MessageType.Error, "Error occurred while Initializing ZMQ response: " + StrError);
                }
            }
            catch (Exception ex)
            {
                WorkerLogger.TraceLog(MessageType.Error, ex.Message);
            }

            WorkerManager.ListenOnRequest(zmqresp);
            try
            {

                if (IOHelper.AgentConfig.Agent_Type.Equals("Source"))
                {
                    WorkerLogger.Control(CommonMessages.IsPublisherWorker);
                    WorkerSource = new WorkerSource();
                    WorkerSource.StartCollectingData();
                }
                else if (IOHelper.AgentConfig.Agent_Type.Equals("Destination"))
                {
                    WorkerLogger.Control(CommonMessages.IsConsumerWorker, true);

                    WorkerDestination = new WorkerDestination();
                 
                    Thread WorkerDestinationThread = new Thread(WorkerDestination.StartWritingData)
                    {
                        Name = "Worker Destination Thread",
                        IsBackground = true
                    };
                    WorkerDestinationThread.Start();
                    //WorkerDestination.StartWritingData();
                }

            }
            catch (Exception Ex0)
            {
                // ignore
            }

            // <<<========================================= Load and build address space ==============================>>>
            try
            {
                //WorkerLogger.TraceLog(MessageType.Control, CommonMessages.LoadInifileConfig);
                workerManager = new WorkerManager();
                //workerManager.StartlistingToIncomingRequest();
            }
            catch (Exception Ex0)
            {
                WorkerLogger.Exception(Ex0);
            }

            return base.StartAsync(cancellationToken);
        }
        private static bool InitilizeLog(out string error)
        {
            WorkerFilesManager.InitializeIniFiles();
            return WorkerLogger.InitializeLogger(AgentCommonMessages.serviceProcessName, out error, Process.GetCurrentProcess().ProcessName);

        }
     
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

    }
}
