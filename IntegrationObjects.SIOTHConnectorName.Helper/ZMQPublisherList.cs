using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationObjects.SIOTHConnectorName.Helper
{
    public class ZMQPublisherList
    {
        public string ZMQAddress { get; set; }
        public List<string> TopicList { get; set; }
    }
}
