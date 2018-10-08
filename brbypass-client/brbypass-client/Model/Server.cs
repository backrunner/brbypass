using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace brbypass_client.Model
{
    public class Server
    {
        public string host;
        public int port;
        public string password;

        public Server()
        {
            host = "";
            port = 0;
            password = "";
        }
        public Server(string host, int port, string password)
        {
            this.host = host;
            this.port = port;
            this.password = password;
        }
    }
}
