using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationObjects.SIOTHConnectorName.Helper
{
 public   class Item
    {
        public Boolean is_open { get; set; }

        public string TagName { get; set; } //TagName/Address
        public string Address { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Description { get; set; } //port status
    }
}
