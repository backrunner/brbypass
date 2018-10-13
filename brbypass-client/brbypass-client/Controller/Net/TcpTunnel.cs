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
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

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
        private X509Certificate2 certificate;

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
            //set up cert
            try
            {
                certificate = new X509Certificate2(MainWindow.startupPath+"ssl/server.pfx","brbypass23333");
            }
            catch (Exception ex)
            {
                LogController.Error("Cannot create certificate: "+ex.Message);
            }
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
                    byte[] sendHeaderBuffer = new byte[6] { 98, 114, 98, 112, 0, 0 };
                    string jsonRequest = JsonRequest.getString("handshake", "brbypasshandshake");
                    byte[] lengthBytes = BitConverter.GetBytes(jsonRequest.Length);
                    //send is little
                    sendHeaderBuffer[4] = lengthBytes[1];
                    sendHeaderBuffer[5] = lengthBytes[0];
                    byte[] sendBuffer = Encoding.UTF8.GetBytes(jsonRequest);
                    try
                    {
                        stream.Write(sendHeaderBuffer, 0, 6);
                        stream.Write(sendBuffer, 0, sendBuffer.Length);
                        stream.Flush();
                    }
                    catch (IOException ex)
                    {
                        LogController.Error("Send http request error: " + ex.Message);
                        Close();
                    }
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
                    byte[] sendHeaderBuffer = new byte[6] { 98, 114, 98, 112, 0, 0 };
                    string jsonRequest = JsonRequest.getString("auth", cPassword);
                    byte[] lengthBytes = BitConverter.GetBytes(jsonRequest.Length);
                    sendHeaderBuffer[4] = lengthBytes[1];
                    sendHeaderBuffer[5] = lengthBytes[0];
                    byte[] sendBuffer = Encoding.UTF8.GetBytes(jsonRequest);
                    try
                    {
                        stream.Write(sendHeaderBuffer, 0, 6);
                        stream.Write(sendBuffer, 0, sendBuffer.Length);
                        stream.Flush();
                    }
                    catch (IOException ex)
                    {
                        LogController.Error("Send http request error: " + ex.Message);
                        Close();
                    }
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

        public Stream sendHttpContent(string request, Stream clientStream)
        {
            if (IsOnline(tunnel))
            {
                if (stream.CanWrite)
                {
                    byte[] sendHeaderBuffer = new byte[6] { 98, 114, 98, 112, 0, 0 };
                    string jsonRequest = JsonRequest.getString("http", request);
                    byte[] lengthBytes = BitConverter.GetBytes(jsonRequest.Length);
                    sendHeaderBuffer[4] = lengthBytes[1];
                    sendHeaderBuffer[5] = lengthBytes[0];
                    byte[] sendBuffer = Encoding.UTF8.GetBytes(jsonRequest);
                    //write to stream
                    try
                    {
                        stream.Write(sendHeaderBuffer, 0, 6);
                        stream.Write(sendBuffer, 0, sendBuffer.Length);
                        stream.Flush();
                    } catch (IOException ex)
                    {
                        LogController.Error("Send http request error: " + ex.Message);
                        Close();
                        return clientStream;
                    }
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
                                LogController.Debug("=====Now content length: "+header.contentLength.ToString());
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
                                        return clientStream;
                                    }
                                    byte[] content;
                                    switch (jsonResponse.type)
                                    {
                                        case "http":
                                            while (jsonResponse.status != "end" && stream.CanRead)
                                            {
                                                content = HexUtil.HexToByte(jsonResponse.content);
                                                if (clientStream.CanWrite)
                                                {
                                                    clientStream.Write(content, 0, content.Length);                                                    
                                                }
                                                else
                                                {
                                                    LogController.Error("Cannot write data to local client stream");
                                                    return clientStream;
                                                }
                                                headerBuffer = new byte[6];
                                                stream.Read(headerBuffer, 0, 6);
                                                header = new Protocol.Header(headerBuffer);
                                                if (header.contentLength > 0)
                                                {
                                                    buffer = new byte[header.contentLength];
                                                    LogController.Debug("=====Now content length: " + header.contentLength.ToString());
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
                                                        }
                                                    }
                                                    else
                                                    {
                                                        LogController.Warn("Stream received empty response");
                                                    }
                                                }
                                                else
                                                {
                                                    LogController.Warn("Stream received a protocol header error packet");
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
                                                return clientStream;
                                            }
                                            break;
                                        case "httpsconnect":
                                            content = Encoding.UTF8.GetBytes(jsonResponse.content + "\r\n\r\n");
                                            if (clientStream.CanWrite)
                                            {
                                                clientStream.Write(content, 0, content.Length);
                                                clientStream.Flush();
                                                SslStream sslStream = new SslStream(clientStream, false);
                                                try
                                                {
                                                    sslStream.AuthenticateAsServer(certificate, false, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12,true);                                                    
                                                }
                                                catch (Exception ex)
                                                {
                                                    LogController.Error("Create ssl stream error: " + ex.Message);
                                                    return null;
                                                }
                                                return sslStream;
                                            }
                                            else
                                            {
                                                LogController.Error("Cannot write data to local client stream");                                                
                                            }
                                            break;
                                    }
                                }
                                else
                                {
                                    LogController.Warn("Stream received empty response");
                                }
                            }
                            else
                            {
                                LogController.Warn("Stream received a protocol header error packet");
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
                }
            }
            else
            {
                LogController.Error("Connection is break");
                Close();
            }
            return clientStream;
        }
    }
}
