using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationObjects.SIOTHConnectorName.Helper
{
    public class PublisherClass
    {
        public string SchemaId { get; set; }
        public List<Item> Payload = new List<Item>();
    }
}
