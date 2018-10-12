using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace brbypass_client.Controller.Net
{
    public class HttpProxy
    {
        private TcpListener _listener;
        private TcpTunnel _tunnel;

        private Thread serverThread;

        public HttpProxy(int localPort,string remoteHost, int remotePort, string remotePassword)
        {
            //set up listener
            IPAddress localLoop = IPAddress.Parse("127.0.0.1");
            _listener = new TcpListener(localLoop, localPort);
            //set up tunnel
            _tunnel = new TcpTunnel(remoteHost, remotePort, remotePassword);
        }

        public void Start()
        {
            _tunnel.HandshakeSuccessEvent += new TcpTunnel.HandshakeSuccessHandler(remoteHandshakeSuccess);
            serverThread = new Thread(new ThreadStart(_tunnel.TryConnect));
            serverThread.Start();            
        }

        private void remoteHandshakeSuccess()
        {
            //send auth
            _tunnel.AuthSuccessEvent += new TcpTunnel.AuthSuccessHandler(authSuccess);
            _tunnel.TryAuth();
        }

        private void authSuccess()
        {
            //start tcp listener
            try
            {
                _listener.Start();
            }
            catch (Exception ex)
            {
                LogController.Error("Start Local TCP Listener Error: " + ex.Message);
                MainWindow.mainWindow.updateUI_startFailed();
            }
            //update ui
            MainWindow.mainWindow.updateUI_startSucceed();
            //run forever
            Server(_listener);            
        }

        public void Stop()
        {
            _listener.Stop();
            _tunnel.Close();
            serverThread.Abort();
            serverThread.Join();
        }

        private void Server(TcpListener listener)
        {
            try
            {
                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    //while (!ThreadPool.QueueUserWorkItem(new WaitCallback(ProcessClient), client)) ;
                    ProcessClient(client);
                }
            }
            catch(ThreadAbortException)
            {
                LogController.Debug("Local server stop cuz ThreadAbortException.");
            }
            catch (SocketException)
            {
                LogController.Debug("Local server stop cuz SocketException.");
            }
        }

        private void ProcessClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            NetworkStream clientStream = client.GetStream();
            client.Client.IOControl(IOControlCode.KeepAliveValues, KeepAlive(1, 30000, 10000), null);
            try
            {
                while (client.Connected)
                {
                    ProcessPacket(clientStream);
                }
            } catch(Exception ex)
            {
                LogController.Error("ProcessClient Error: " + ex.Message);
            } finally
            {
                client.Close();
            }
        }

        private byte[] KeepAlive(int onOff, int keepAliveTime, int keepAliveInterval)
        {
            byte[] buffer = new byte[12];
            BitConverter.GetBytes(onOff).CopyTo(buffer, 0);
            BitConverter.GetBytes(keepAliveTime).CopyTo(buffer, 4);
            BitConverter.GetBytes(keepAliveInterval).CopyTo(buffer, 8);
            return buffer;
        }

        private void ProcessPacket(NetworkStream clientStream)
        {            
            try
            {
                StringBuilder packet = new StringBuilder();
                if (clientStream.CanRead) {
                    while (clientStream.DataAvailable)
                    {
                        byte[] buffer = new byte[1024];
                        clientStream.Read(buffer, 0, 1024);
                        packet.Append(Encoding.UTF8.GetString(buffer).Trim());
                    }
                    if (packet.Length > 0)
                    {
                        _tunnel.sendHttpContent(packet.ToString().Trim(), clientStream);
                    }
                } else
                {
                    LogController.Error("Cannot read from client stream");
                }
            } catch (Exception ex)
            {
                LogController.Error("Client Stream Error: " + ex.Message);
            }
        }
    }
}
