using IntegrationObjects.SIOTHConnectorName.Helper;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationObjects.SIOTHConnectionConnector.Helper
{
  public  class ASynchroneHost
    {
        public List<Tag> TagList = new List<Tag>();
        public List<string> Ip_Address = new List<string>();
         public  ASynchroneHost(List<Tag> TagList, List<string> Ip_Address) {
            this.TagList = TagList;
            this.Ip_Address=Ip_Address;
        }
    }
}
