using System;
using System.Collections.Generic;

using System.Text;

namespace IntegrationObjects.SIOTHConnectorName.Helper

{
    public class Tag
    {
        public String TagName { get; set; }
        public Boolean OnlyHostPing { get; set; }
        public String Ip_Address {get;set;}
        public int Connection_Timeout { get; set; }
        public int UpdateRate { get; set; }
        public Boolean Synchronous { get; set; }
        public Boolean DontFragment { get; set; }
        public int Port { get; set; }
        public string PortType { get; set; }
        public String Data { get; set; }
        public string  PortDescription { get; set; } 
        public Boolean is_open { get; set; }
        public String TagKey { get; set; }
        public Tag() {

        }
 
  
    }
    
}
