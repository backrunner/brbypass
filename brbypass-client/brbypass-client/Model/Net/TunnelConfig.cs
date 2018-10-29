using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace brbypass_client.Model.Net
{
    public class TunnelConfig
    {
        public string Host
        {
            get;
            set;
        }
        public int Port
        {
            get;
            set;
        }
        public string Password
        {
            get;
            set;
        }
        public TunnelConfig(string host)
        {
            this.Host = host;
            this.Port = 1234;
            this.Password = "";
        }
        public TunnelConfig(string host,string password)
        {
            this.Host = host;
            this.Password = password;
            this.Port = 1234;
        }
        public TunnelConfig(string host,int port, string password)
        {
            this.Host = host;
            this.Port = port;
            this.Password = password;
        }
    }
}
