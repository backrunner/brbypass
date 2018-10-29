using brbypass_client.Model.Net;
using brbypass_client.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace brbypass_client.Controller.Net
{
    public class TcpTunnel
    {
        private TcpClient Tunnel
        {
            get;
            set;
        }

        private string Host;
        private int Port;

        private string Password;

        public TcpTunnel(TunnelConfig config)
        {
            this.Tunnel = new TcpClient();
            this.Host = config.Host;
            this.Port = config.Port;
            this.Password = config.Password;
        }

        public TcpClient GetClient()
        {
            try
            {
                this.Tunnel.Connect(Host, Port);
            } catch (Exception e)
            {
                LogController.Debug("A error occured when established the tunnel: " + e.Message);
                return null;
            }
            if (Tunnel.Connected)
            {
                /*
                 *      Auth Request
                 *   0    1   |  2     3       4-?
                 *  0x07 0x02 | CMD  LENGTH  PASSWORD              
                 *  
                 *  CMD = 0x01
                 */

                byte[] password = Encoding.UTF8.GetBytes(Password);
                byte[] auth = new byte[] { 0x07,0x02, 0x01, (byte)password.Length};

                byte[] request = new byte[auth.Length + password.Length];
                auth.CopyTo(request, 0);
                password.CopyTo(request, 4);

                SocketUtils.Send(Tunnel.Client, request);

                /*
                 *       Auth Response
                 *   0    1   |  2     3
                 *  0x06 0x03 | CMD  STATUS        
                 *  
                 *  CMD = 0x02
                 *  STATUS: 0x00 - Success 0x01 - Failed
                 *  
                 */

                byte[] authBuffer = new byte[4];
                SocketUtils.Receive(Tunnel.Client, 4, out authBuffer);
                if (authBuffer[0] == 0x06 && authBuffer[1] == 0x03)
                {
                    if (authBuffer[2] == 0x02)
                    {
                        if (authBuffer[3] == 0x00)
                        {
                            return Tunnel;
                        }
                        else
                        {
                            Tunnel.Close();
                            Tunnel = null;
                            return null;
                        }
                    }
                    else
                    {
                        Tunnel.Close();
                        Tunnel = null;
                        return null;
                    }
                } else
                {
                    Tunnel.Close();
                    Tunnel = null;
                    return null;
                }
            }
            else
            {
                Tunnel.Close();
                Tunnel = null;
                return null;
            }
        }
    }
}
