using brbypass_client.Model.Net;
using brbypass_client.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Starksoft.Aspen.Proxy;

namespace brbypass_client.Controller.Net
{
    class SocksConnection
    {
        public SocksConnection(SocksServer server, Socket client, TunnelConfig tunnelConfig)
        {
            this.Server = server;
            this.Client = client;
            this.TunnelConfig = tunnelConfig;
        }
        private SocksServer Server
        {
            get;
            set;
        }
        internal Socket Client
        {
            private set;
            get;
        }

        private IPEndPoint RemoteEndPoint
        {
            get;
            set;
        }

        private short Type
        {
            get;
            set;
        }

        private string Address
        {
            get;
            set;
        }

        private uint RemotePort
        {
            get;
            set;
        }

        private TunnelConfig TunnelConfig
        {
            get;
            set;
        }

        private void DoRequest(object state)
        {
            if (this.Server.IsStarted)
            {
                if (!this.DoShakeHands())
                {
                    goto __CLOSE;
                }
                if (this.Server.RequireValidate)
                {
                    if (!this.ValidateIdentity())
                    {
                        goto __CLOSE;
                    }
                }
                if (!this.DoProtocolRequest())
                {
                    goto __CLOSE;
                }
                this.CreateProxyBridge();
                goto __EXIT;
            }
            __CLOSE:
            this.Close();
            __EXIT:
            return;
        }

        public static void DoRequest(SocksServer server, Socket client, TunnelConfig tunnelConfig)
        {
            SocksConnection connection = new SocksConnection(server, client, tunnelConfig);
            client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, 1);
            ThreadPool.QueueUserWorkItem(new WaitCallback(connection.DoRequest));
        }

        private void Close()
        {
            if (this.Client != null)
            {
                LogController.Debug("Socks connection closed: " + this.Client.RemoteEndPoint.ToString());
                this.Client.Close(3);
                this.Client = null;
            }
            if (this.Proxy != null)
            {
                if (this.Proxy.Client.RemoteEndPoint != null)
                {
                    LogController.Debug("Socks proxy connection closed: " + this.Proxy.Client.RemoteEndPoint.ToString());
                }
                else
                {
                    LogController.Debug("Socks proxy connection closed.");
                }
                this.Proxy.Close();
                this.Proxy = null;
            }
        }

        private TcpClient Proxy
        {
            get;
            set;
        }

        private byte[] _ClientBuffer;
        private byte[] _ProxyBuffer;

        private void CreateProxyBridge()
        {
            if (this.Client.Connected)
            {
                TcpClient proxyClient = null;
                try
                {
                    proxyClient = new TcpClient(TunnelConfig.Host, TunnelConfig.Port);
                    proxyClient.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, 1);
                } catch (Exception e)
                {
                    LogController.Error("A error occured when creating a tunnel: " + e.Message);
                    this.Close(); 
                    return;
                }
                if (proxyClient != null)
                {
                    this.Proxy = proxyClient;
                    try
                    {
                        //this.Proxy.Connect(TunnelConfig.Host, TunnelConfig.Port);
                        if (this.Proxy.Connected)
                        {// Send Bind Message
                         //
                         //  0    1    2     4      5 - ?
                         // 0x07 0x02 CMD   TYPE   VARIABLE

                            // 3 Type of Message:
                            //
                            //  0    1    2    3    4-7    8-9
                            // 0x07 0x02 CMD  0x01 DESTIP  PORT

                            //  0    1    2     3       4-5     6-7    8-? 
                            // 0x07 0x02 CMD  0x03   DOMAINLEN  PORT  DOMAIN

                            //  0    1    2    3    4 - 19    20-21
                            // 0x07 0x02 CMD  0x04  DESTIP    PORT


                            // CMD  = 0x02 - Bind Request

                            // TYPE = 0x01 - IPv4
                            // TYPE = 0x03 - Domain
                            // TYPE = 0x04 - IPv6

                            //构造数据包
                            byte[] sendBuffer = new byte[0];

                            byte[] b_domain = new byte[0];

                            switch (this.Type)
                            {
                                case 1:
                                    sendBuffer = new byte[12];
                                    break;
                                case 3:
                                    b_domain = Encoding.UTF8.GetBytes(this.Address);
                                    sendBuffer = new byte[10 + b_domain.Length];
                                    break;
                                case 4:
                                    sendBuffer = new byte[24];
                                    break;
                                default:
                                    this.Close();
                                    break;
                            }

                            sendBuffer[0] = 0x07; sendBuffer[1] = 0x02; sendBuffer[2] = 0x02;

                            byte[] b_ip;
                            byte[] b_port;

                            switch (this.Type)
                            {
                                case 1:
                                    sendBuffer[3] = 0x01;
                                    b_ip = RemoteEndPoint.Address.GetAddressBytes();
                                    b_ip.CopyTo(sendBuffer, 4);
                                    b_port = BitConverter.GetBytes(this.RemotePort);
                                    sendBuffer[8] = b_port[0]; sendBuffer[9] = b_port[1];
                                    break;
                                case 3:
                                    sendBuffer[3] = 0x03;
                                    //little endian
                                    byte[] b_domainlen = BitConverter.GetBytes(b_domain.Length);
                                    sendBuffer[4] = b_domainlen[0];
                                    sendBuffer[5] = b_domainlen[1];
                                    b_port = BitConverter.GetBytes(this.RemotePort);
                                    sendBuffer[6] = b_port[0];
                                    sendBuffer[7] = b_port[1];
                                    b_domain.CopyTo(sendBuffer, 8);
                                    break;
                                case 4:
                                    sendBuffer[3] = 0x04;
                                    b_ip = RemoteEndPoint.Address.GetAddressBytes();
                                    b_ip.CopyTo(sendBuffer, 4);
                                    b_port = BitConverter.GetBytes(this.RemotePort);
                                    sendBuffer[20] = b_port[0];
                                    sendBuffer[21] = b_port[1];
                                    break;
                            }


                            //发送数据包
                            SocketUtils.Send(this.Proxy.Client, sendBuffer, 0, sendBuffer.Length);
                            _ClientBuffer = new byte[this.Client.ReceiveBufferSize];
                            _ProxyBuffer = new byte[this.Proxy.ReceiveBufferSize];
                            this.Client.BeginReceive(_ClientBuffer, 0, _ClientBuffer.Length, SocketFlags.None, this.OnClientReceive, this.Client);
                            this.Proxy.Client.BeginReceive(_ProxyBuffer, 0, _ProxyBuffer.Length, SocketFlags.None, this.OnProxyReceive, this.Proxy.Client);
                        }
                        else
                        {
                            this.Close();
                        }
                    }
                    catch (Exception e)
                    {
                        LogController.Error("Find some error on start receive: " + e.Message);
                        this.Close();
                    }
                }
                else
                {
                    LogController.Error("Failed to establish proxy tunnel.");
                    this.Close();
                }
            }
            else
            {
                this.Close();
            }
        }

        private void OnClientReceive(IAsyncResult result)
        {
            if (this.Server.IsStarted)
            {
                try
                {
                    Socket socket = result.AsyncState as Socket;
                    SocketError error;
                    int size = this.Client.EndReceive(result, out error);
                    if (size > 0)
                    {
                        // Send Message
                        //
                        //  0    1    2    3-4      5-?
                        // 0x07 0x02 CMD  LENGTH  CONTENT

                        // CMD  = 0x03 - TCP Request No Encryption 

                        //构造数据包
                        byte[] sendBuffer = new byte[0];

                        byte[] b_domain = new byte[0];

                        sendBuffer = new byte[size + 5];

                        sendBuffer[0] = 0x07; sendBuffer[1] = 0x02; sendBuffer[2] = 0x03;
                        //convert size to bytes (little endian)
                        byte[] b_size = BitConverter.GetBytes(size);
                        sendBuffer[3] = b_size[0]; sendBuffer[4] = b_size[1];

                        Array.Copy(_ClientBuffer, 0, sendBuffer, 5, size);

                        //发送数据包
                        SocketUtils.Send(this.Proxy.Client, sendBuffer, 0, sendBuffer.Length);

                        if (this.Server.IsStarted)
                        {
                            this.Client.BeginReceive(_ClientBuffer, 0, _ClientBuffer.Length, SocketFlags.None, this.OnClientReceive, this.Client);
                        }
                    }
                    else
                    {
                        this.Close();
                    }
                }
                catch (Exception e)
                {
                    LogController.Debug("Found some error on send data to tunnel: " + e.Message);
                    this.Close();
                }
            }
        }

        private void OnProxyReceive(IAsyncResult result)
        {
            if (this.Server.IsStarted)
            {
                try
                {
                    Socket socket = result.AsyncState as Socket;
                    SocketError error;
                    int size = socket.EndReceive(result, out error);
                    if (size > 0)
                    {
                        // Recv Buffer
                        //  0     1     2    3 - 4    5 - ?
                        // 0x06  0x03  CMD  LENGTH    DATA
                        // CMD: 0x03 - TCP Response No Encryption
                        if (_ProxyBuffer[0] == 0x06 && _ProxyBuffer[1] == 0x03 && _ProxyBuffer[2] == 0x04)
                        {
                            int contentLength = BitConverter.ToInt16(_ProxyBuffer, 3);
                            byte[] content = new byte[contentLength];
                            Array.Copy(_ProxyBuffer, 5, content, 0, contentLength);
                            SocketUtils.Send(Client, content, 0, contentLength);
                            if (this.Server.IsStarted)
                            {
                                socket.BeginReceive(_ProxyBuffer, 0, _ProxyBuffer.Length, SocketFlags.None, this.OnProxyReceive, socket);
                            }
                        }
                        else
                        {
                            LogController.Debug("Receive a wrong header packet.");
                            this.Close();
                        }
                    }
                }
                catch (Exception e)
                {
                    LogController.Debug("Catch an exception at OnProxyReceive: " + e.Message);
                    this.Close();
                }
            }
        }

        private bool DoShakeHands()
        {
            byte[] buffer;
            byte method = 0xFF;
            if (SocketUtils.Receive(this.Client, 2, out buffer))
            {
                SocketUtils.Receive(this.Client, 1, out buffer);
                if (this.Server.RequireValidate)
                {
                    while (buffer.Length > 0)
                    {
                        if (buffer[0] == 0x02) method = 0x02;
                        SocketUtils.Receive(this.Client, 1, out buffer);
                    }
                }
                else
                {
                    method = 0x00;
                }
            }
            SocketUtils.Send(this.Client, new byte[] { 0x05, method });
            return (method != 0xFF);
        }

        private bool ValidateIdentity()
        {
            byte[] buffer;
            byte ep = 0xFF;
            string username = string.Empty, password = string.Empty;

            //报文格式:0x01 | 用户名长度（1字节）| 用户名（长度根据用户名长度域指定） | 口令长度（1字节） | 口令（长度由口令长度域指定）
            if (SocketUtils.Receive(this.Client, 2, out buffer))
            {
                if (buffer.Length == 2)
                {
                    //用户名为空
                    if (buffer[1] == 0x00)
                    {
                        if (string.IsNullOrEmpty(this.Server.UserName))
                        {
                            ep = 0x00;  //用户名为空
                        }
                    }
                    else
                    {
                        if (SocketUtils.Receive(this.Client, (uint)buffer[1], out buffer))
                        {
                            username = Encoding.ASCII.GetString(buffer);
                            if (!string.IsNullOrEmpty(this.Server.UserName))
                            {
                                ep = (byte)(username.Equals(this.Server.UserName) ? 0x00 : 0xFF);
                            }
                        }
                    }
                    if (ep == 0x00)
                    {
                        ep = 0xFF;
                        //判断密码
                        if (SocketUtils.Receive(this.Client, 1, out buffer))
                        {
                            if (buffer[0] == 0x00)
                            {
                                if (!string.IsNullOrEmpty(this.Server.Password))
                                {
                                    ep = 0x00;  //密码为空
                                }
                            }
                            else
                            {
                                if (SocketUtils.Receive(this.Client, (uint)buffer[0], out buffer))
                                {
                                    password = Encoding.ASCII.GetString(buffer);
                                    if (!string.IsNullOrEmpty(this.Server.Password))
                                    {
                                        ep = (byte)(password.Equals(this.Server.Password) ? 0x00 : 0xFF);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //输出应答
            SocketUtils.Send(this.Client, new byte[] { 0x01, ep });
            return (ep == 0x00);
        }

        private bool DoProtocolRequest()
        {
            //取前4字节
            byte[] buffer;
            IPAddress ipAddress = null;
            byte rep = 0x07;            //不支持的命令
            if (SocketUtils.Receive(this.Client, 4, out buffer))
            {
                if (buffer.Length == 4)
                {
                    //判断地址类型
                    switch (buffer[3])
                    {
                        case 0x01:
                            this.Type = 1;
                            //IPV4
                            if (SocketUtils.Receive(this.Client, 4, out buffer))
                            {
                                ipAddress = new IPAddress(buffer);
                            }
                            break;
                        case 0x03:
                            this.Type = 3;
                            //域名
                            if (SocketUtils.Receive(this.Client, 1, out buffer))
                            {
                                //取得域名的长度
                                if (SocketUtils.Receive(this.Client, (uint)(buffer[0]), out buffer))
                                {
                                    //取得域名地址
                                    string address = Encoding.ASCII.GetString(buffer);
                                    try
                                    {
                                        IPAddress[] addresses = Dns.GetHostAddresses(address);
                                        if (addresses.Length != 0)
                                        {
                                            ipAddress = addresses[0];
                                            this.Address = address;
                                        }
                                        else
                                        {
                                            rep = 0x04;  //主机不可达
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        LogController.Error("Dns error: " + e.Message);
                                    }
                                }
                            }
                            break;
                        case 0x04:
                            this.Type = 4;
                            //IPV6;
                            if (SocketUtils.Receive(this.Client, 16, out buffer))
                            {
                                ipAddress = new IPAddress(buffer);
                            }
                            break;
                        default:
                            rep = 0x08; //不支持的地址类型
                            break;
                    }
                }
            }

            if (ipAddress != null && rep == 0x07)
            {
                //取得端口号
                if (SocketUtils.Receive(this.Client, 2, out buffer))
                {
                    Array.Reverse(buffer);  //反转端口值
                    if (this.Type == 1 || this.Type == 4)
                    {
                        this.RemoteEndPoint = new IPEndPoint(ipAddress, BitConverter.ToUInt16(buffer, 0));
                    }
                    else
                    {
                        this.RemotePort = BitConverter.ToUInt16(buffer, 0);
                    }
                    rep = 0x00;
                }
            }

            //输出应答
            MemoryStream stream = new MemoryStream();
            stream.WriteByte(0x05);
            stream.WriteByte(rep);
            stream.WriteByte(0x00);
            stream.WriteByte(0x01);
            IPEndPoint localEP = (IPEndPoint)Client.LocalEndPoint;
            byte[] localIP = localEP.Address.GetAddressBytes();
            stream.Write(localIP, 0, localIP.Length);
            byte[] localPort = BitConverter.GetBytes((ushort)IPAddress.HostToNetworkOrder(localEP.Port));
            stream.Write(localPort, 0, localPort.Length);
            SocketUtils.Send(this.Client, stream.ToArray());

            return (this.RemoteEndPoint != null || this.Address != null);
        }
    }
}

