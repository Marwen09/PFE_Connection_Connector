using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationObjects.SIOTHConnectorName.Helper
{
    public class dataExtractionProperties
    {
     
        public Dataconfig DataConfig = new Dataconfig();
    }
    public class Payload
    {
        public string SchemaId;
        public IEnumerable<Dictionary<string, object>> Iterator;
    }
}
