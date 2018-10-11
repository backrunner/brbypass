using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace brbypass_client.Controller.Net
{
    public class HttpProxy
    {
        private TcpListener _listener;
        private TcpTunnel _tunnel;

        private Thread serverThread;
        private Thread remoteThread;

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
            remoteThread = new Thread(new ThreadStart(_tunnel.TryConnect));
            remoteThread.Start();            
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
            serverThread = new Thread(new ParameterizedThreadStart(Server));
            serverThread.Start(_listener);
            //update ui
            MainWindow.mainWindow.updateUI_startSucceed();
        }

        public void Stop()
        {
            _listener.Stop();
            _tunnel.Close();
            if (serverThread != null)
            {
                serverThread.Abort();
            }
            remoteThread.Abort();
            if (serverThread != null)
            {
                serverThread.Join();
            }
            remoteThread.Join();
        }

        private void Server(object obj)
        {
            TcpListener listener = (TcpListener)obj;
            try
            {
                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    while (!ThreadPool.QueueUserWorkItem(new WaitCallback(ProcessClient), client)) ;
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
            try
            {
                ProcessPacket(client);
            } catch(Exception ex)
            {
                LogController.Error("ProcessClient Error: " + ex.Message);
            } finally
            {
                client.Close();
            }
        }

        private void ProcessPacket(TcpClient client)
        {
            Stream clientStream = client.GetStream();
            StreamReader clientStreamReader = new StreamReader(clientStream);
            try
            {
                string packet = clientStreamReader.ReadToEnd();

            } catch (Exception ex)
            {
                LogController.Error("Client Stream Error: " + ex.Message);
            }
        }
    }
}
