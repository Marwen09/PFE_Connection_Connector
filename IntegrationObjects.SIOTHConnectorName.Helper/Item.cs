using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationObjects.SIOTHConnectorName.Helper
{
 public   class Item
    {
        public object Value { get; set; }

        public string TagName { get; set; } //DeviceName/GroupeName/TagName/PropertyName
        public string TimeStamp { get; set; }
    }
}
