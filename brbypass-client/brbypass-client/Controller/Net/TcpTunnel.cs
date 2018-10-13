using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using brbypass_client.Model.Net;
using brbypass_client.Util;
using Newtonsoft.Json;

namespace brbypass_client.Controller.Net
{
    public class TcpTunnel
    {
        //config
        private string remoteHost;
        private int remotePort;
        private string cPassword;

        //object
        private TcpClient tunnel;
        private NetworkStream stream;
        private StreamWriter sw;

        //event
        public delegate void HandshakeSuccessHandler();
        public event HandshakeSuccessHandler HandshakeSuccessEvent;
        public delegate void AuthSuccessHandler();
        public event AuthSuccessHandler AuthSuccessEvent;

        public TcpTunnel(string host, int port, string password)
        {
            remoteHost = host;
            remotePort = port;
            cPassword = password;
            //set up client
            tunnel = new TcpClient();
            tunnel.Client.IOControl(IOControlCode.KeepAliveValues, KeepAlive(1, 30000, 10000), null);
        }

        public bool IsOnline(TcpClient c)
        {
            bool result = false;
            try
            {
                result = !((c.Client.Poll(1000, SelectMode.SelectRead) && (c.Client.Available == 0)) || !c.Client.Connected);
            }
            catch (Exception ex)
            {
                LogController.Error("Check socket status error: " + ex.Message);
                Close();
            }
            return result;
        }

        private byte[] KeepAlive(int onOff, int keepAliveTime, int keepAliveInterval)
        {
            byte[] buffer = new byte[12];
            BitConverter.GetBytes(onOff).CopyTo(buffer, 0);
            BitConverter.GetBytes(keepAliveTime).CopyTo(buffer, 4);
            BitConverter.GetBytes(keepAliveInterval).CopyTo(buffer, 8);
            return buffer;
        }

        public void CloseTunnel()
        {
            if (stream != null)
            {
                stream.Close();
            }
            tunnel.Close();
        }

        private void Close()
        {
            var task = new Task(MainWindow.mainWindow.httpProxy.Stop);
            task.Start();
        }

        public void TryConnect()
        {
            LogController.Debug("Try to connect " + remoteHost + ":" + remotePort);
            try
            {
                tunnel.Connect(remoteHost, remotePort);
            }
            catch (Exception ex)
            {
                LogController.Error("Connect to remote server error: " + ex.Message);
                Close();
                return;
            }
            if (IsOnline(tunnel))
            {
                stream = tunnel.GetStream();
                LogController.Debug("Server connected, now trying handshake");
                //try handshake
                stream.WriteTimeout = 3000;
                stream.ReadTimeout = 3000;
                if (stream.CanWrite)
                {
                    //use StreamWriter write handshake message to netstream
                    using (sw = new StreamWriter(stream))
                    {
                        string data = JsonRequest.getString("handshake", "brbypasshandshake");
                        sw.WriteLine(data);
                        sw.Flush();
                        if (stream.CanRead)
                        {
                            byte[] headerBuffer = new byte[6];
                            try
                            {
                                stream.Read(headerBuffer, 0, 6);
                            }
                            catch (IOException ex)
                            {
                                LogController.Error("Handshake IO error: " + ex.Message);
                            }
                            Protocol.Header header = new Protocol.Header(headerBuffer);
                            if (header.contentLength > 0)
                            {
                                byte[] bytes = new byte[header.contentLength];
                                try
                                {
                                    stream.Read(bytes, 0, header.contentLength);
                                }
                                catch (IOException tex)
                                {
                                    LogController.Error("Read data timeout: " + tex.Message);
                                    Close();
                                    return;
                                }
                                JsonResponse jsonResponse = new JsonResponse();
                                try
                                {
                                    //get handshake response
                                    string response = Encoding.UTF8.GetString(bytes).Trim();
                                    jsonResponse = JsonConvert.DeserializeObject<JsonResponse>(response);
                                }
                                catch (Exception ex)
                                {
                                    LogController.Error("Resolve handshake response error: " + ex.Message);
                                    Close();
                                    return;
                                }
                                if (jsonResponse.status == "success")
                                {
                                    LogController.Debug("Received handshake response");
                                    HandshakeSuccessEvent?.Invoke();
                                }
                            }
                            else
                            {
                                LogController.Warn("Handshake received a protocol header error packet");
                                Close();
                                return;
                            }
                        }
                        else
                        {
                            LogController.Error("Cannot read data from stream");
                            Close();
                            return;
                        }
                    }
                }
                else
                {
                    LogController.Error("Cannot write data to stream");
                    Close();
                    return;
                }
            }
            else
            {
                LogController.Error("Connection error");
                Close();
                return;
            }
        }

        public void TryAuth()
        {
            if (IsOnline(tunnel))
            {
                LogController.Debug("Trying to send auth");
                //try send auth
                if (stream.CanWrite)
                {
                    sw.WriteLine(JsonRequest.getString("auth", cPassword));
                    sw.Flush();
                    if (stream.CanRead)
                    {
                        byte[] headerBuffer = new byte[6];
                        try
                        {
                            stream.Read(headerBuffer, 0, 6);
                        }
                        catch (IOException ex)
                        {
                            LogController.Error("Auth IO error: " + ex.Message);
                        }
                        Protocol.Header header = new Protocol.Header(headerBuffer);
                        if (header.contentLength > 0)
                        {
                            byte[] bytes = new byte[header.contentLength];
                            try
                            {
                                stream.Read(bytes, 0, header.contentLength);
                            }
                            catch (IOException tex)
                            {
                                LogController.Error("Read data timeout: " + tex.Message);
                                Close();
                                return;
                            }
                            JsonResponse jsonResponse = new JsonResponse();
                            try
                            {
                                //get auth response
                                string response = Encoding.UTF8.GetString(bytes).Trim();
                                jsonResponse = JsonConvert.DeserializeObject<JsonResponse>(response);
                            }
                            catch (Exception ex)
                            {
                                LogController.Error("Resolve auth response error: " + ex.Message);
                            }
                            if (jsonResponse.status == "success")
                            {
                                LogController.Debug("Auth success");
                                AuthSuccessEvent?.Invoke();
                            }
                            else
                            {
                                LogController.Debug("Auth failed");
                                Close();
                                return;
                            }
                        }
                        else
                        {
                            LogController.Warn("Auth received a protocol header error packet");
                            Close();
                            return;
                        }
                    }
                    else
                    {
                        LogController.Error("Cannot read data from stream");
                        Close();
                        return;
                    }
                }
                else
                {
                    LogController.Error("Cannot write auth data to stream");
                    Close();
                    return;
                }
            }
            else
            {
                LogController.Error("Connection is break");
                Close();
                return;
            }
        }

        public void sendHttpContent(string request, NetworkStream clientStream)
        {
            if (IsOnline(tunnel))
            {
                if (stream.CanWrite)
                {
                    sw.WriteLine(JsonRequest.getString("http", request));
                    sw.Flush();
                    if (stream.CanRead)
                    {
                        try
                        {
                            byte[] headerBuffer = new byte[6];
                            stream.Read(headerBuffer, 0, 6);
                            Protocol.Header header = new Protocol.Header(headerBuffer);
                            if (header.contentLength > 0)
                            {
                                byte[] buffer = new byte[header.contentLength];
                                stream.Read(buffer, 0, header.contentLength);
                                if (buffer.Length > 0)
                                {
                                    string response = Encoding.UTF8.GetString(buffer);
                                    JsonResponse jsonResponse = new JsonResponse();
                                    try
                                    {
                                        jsonResponse = JsonConvert.DeserializeObject<JsonResponse>(response);
                                    }
                                    catch (Exception ex)
                                    {
                                        LogController.Error("Resolve stream response error: " + ex.Message);
                                        return;
                                    }
                                    byte[] content;
                                    switch (jsonResponse.type)
                                    {
                                        case "http":
                                            while (jsonResponse.status != "end")
                                            {
                                                content = HexUtil.HexToByte(jsonResponse.content);
                                                if (clientStream.CanWrite)
                                                {
                                                    clientStream.Write(content, 0, content.Length);
                                                }
                                                else
                                                {
                                                    LogController.Error("Cannot write data to local client stream");
                                                    return;
                                                }

                                                headerBuffer = new byte[6];
                                                stream.Read(headerBuffer, 0, 6);
                                                header = new Protocol.Header(headerBuffer);
                                                if (header.contentLength > 0)
                                                {
                                                    buffer = new byte[header.contentLength];
                                                    //read next package
                                                    stream.Read(buffer, 0, header.contentLength);
                                                    response = Encoding.UTF8.GetString(buffer);
                                                    if (buffer.Length > 0)
                                                    {
                                                        try
                                                        {
                                                            jsonResponse = JsonConvert.DeserializeObject<JsonResponse>(response);
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            LogController.Error("Resolve stream response error: " + ex.Message);
                                                            return;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        LogController.Warn("Stream received empty response");
                                                        return;
                                                    }
                                                }
                                                else
                                                {
                                                    LogController.Warn("Stream received a protocol header error packet");
                                                    return;
                                                }
                                            }
                                            clientStream.Flush();
                                            break;
                                        case "httpconnect":
                                            content = Encoding.UTF8.GetBytes(jsonResponse.content + "\r\n\r\n");
                                            if (clientStream.CanWrite)
                                            {
                                                clientStream.Write(content, 0, content.Length);
                                                clientStream.Flush();
                                            }
                                            else
                                            {
                                                LogController.Error("Cannot write data to local client stream");
                                                return;
                                            }
                                            break;
                                    }
                                }
                                else
                                {
                                    LogController.Warn("Stream received empty response");
                                    return;
                                }
                            }
                            else
                            {
                                LogController.Warn("Stream received a protocol header error packet");
                                return;
                            }
                        }
                        catch (IOException ex)
                        {
                            LogController.Error("Stream IO error: " + ex.Message);
                        }
                    }
                }
                else
                {
                    LogController.Error("Cannot write data to remote stream");
                    Close();
                    return;
                }
            }
            else
            {
                LogController.Error("Connection is break");
                Close();
                return;
            }
        }
    }
}
