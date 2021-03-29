using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationObjects.SIOTHConnectorName.Helper
{
    public class DeviceConfig
    {
        public String Device_Name { get; set; }
        public String IP_Address { get; set; }
        public String Transport { get; set; }
        public int Device_ID { get; set; }
        public int Port { get; set; }
        public int ClientPort { get; set; }
        public int Connection_Timeout { get; set; }
        public int Retries { get; set; }
        public int Address { get; set; }
        public String MSTPPort { get; set; }
        public int Baud { get; set; }
        public int ClientAddress { get; set; }

    }
}
