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

        private Thread serverThread;

        public HttpProxy(int localPort)
        {
            //set up listener
            IPAddress localLoop = IPAddress.Parse("127.0.0.1");
            _listener = new TcpListener(localLoop, localPort);
        }

        public void Start()
        {
            //start tcp listener
            try
            {
                _listener.Start();
            } catch (Exception ex)
            {
                LogController.Error("Start Local TCP Listener Error: " + ex.Message);
            }
            serverThread = new Thread(new ParameterizedThreadStart(Server));
            serverThread.Start(_listener);
        }

        public void Stop()
        {
            _listener.Stop();
            serverThread.Abort();
            serverThread.Join();
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
                LogController.Debug("Get packet: " + packet);
            } catch (Exception ex)
            {
                LogController.Error("Client Stream Error: " + ex.Message);
            }
        }
    }
}
