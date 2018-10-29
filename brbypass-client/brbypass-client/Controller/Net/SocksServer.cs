using brbypass_client.Model.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace brbypass_client.Controller.Net
{
    class SocksServer
    {
        public ushort Port
        {
            get;
            private set;
        }
        public string UserName
        {
            get;
            private set;
        }
        public string Password
        {
            get;
            private set;
        }
        public bool RequireValidate
        {
            get
            {
                return !string.IsNullOrEmpty(this.UserName) || !string.IsNullOrEmpty(this.Password);
            }
        }

        private TcpListener _Listener;
        private TunnelConfig _TunnelConfig;

        internal bool IsStarted
        {
            get;
            private set;
        }

        public SocksServer(ushort port)
        {
            this.Port = port;
        }
        public SocksServer(ushort port, string userName, string password)
        {
            this.Port = port;
            this.UserName = userName;
            this.Password = password;
        }

        public void Start(string remoteHost,int port, string password)
        {
            if (!this.IsStarted)
            {
                this._Listener = new TcpListener(IPAddress.Any, this.Port);
                this._Listener.Start();
                this._Listener.BeginAcceptSocket(this.OnBeginAcceptSocket, this._Listener);
                this._TunnelConfig = new TunnelConfig(remoteHost, port, password);
                this.IsStarted = true;
                LogController.Debug(string.Format("Socks server started at {0}", ":" + this.Port.ToString()));
                //start succeed
                MainWindow.mainWindow.updateUI_startSucceed();
            } else
            {
                LogController.Warn("A socks server is running, can not start a new one.");
                //start failed
                MainWindow.mainWindow.updateUI_startFailed();                
            }
        }

        public void Stop()
        {
            if (this.IsStarted)
            {
                if (this.IsStarted)
                {
                    this.IsStarted = false;
                    this._Listener.Stop();
                    this._Listener = null;
                    LogController.Debug("Socks server is stopped.");
                }
            } else
            {
                LogController.Warn("Socks server is not running...");
            }
        }

        private void OnBeginAcceptSocket(IAsyncResult async)
        {
            TcpListener listener = async.AsyncState as TcpListener;
            try
            {
                Socket client = listener.EndAcceptSocket(async);
                SocksConnection.DoRequest(this, client, _TunnelConfig);
                if (this.IsStarted)
                {
                    listener.BeginAcceptSocket(this.OnBeginAcceptSocket, listener);
                }
            } catch (ObjectDisposedException)
            {

            } catch (Exception ex)
            {
                LogController.Error("Socks proxy server error: " + ex.Message);
            }
        }
    }
}
