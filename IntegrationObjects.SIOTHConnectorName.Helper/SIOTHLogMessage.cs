using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationObjects.SIOTHConnectorName.Helper
{
    public class SIOTHLogMessage
    {
        public string LogLevel { get; set; }
        public string Message { get; set; }
        public DateTime LogDate { get; set; }
        public string LogSource { get; set; }
    }
}
