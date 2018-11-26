using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace brbypass_client.Util
{
    /// <summary>
    /// Socket的实用类
    /// </summary>
    public static class SocketUtils
    {
        /// <summary>
        /// 超时时间
        /// </summary>
        public const int TIMEOUT = 30000000; //30秒钟超时
        #region 接收信息
        /// <summary>
        /// 接收数据
        /// </summary>
        /// <param name="client"></param>
        /// <param name="maxSize">要取提数据包大小</param>
        /// <param name="buffer">接收到的数据</param>
        /// <returns></returns>
        internal static bool Receive(Socket client, uint maxSize, out byte[] buffer)
        {
            buffer = new byte[maxSize];
            int offset = 0;
            if (client.Connected)
            {
                try
                {
                    buffer = new byte[maxSize];
                    do
                    {
                        if (client.Available == 0)
                        {
                            //查询数据是否有数据可读
                            if (!client.Poll(TIMEOUT, SelectMode.SelectRead)) break;
                            if (client.Available == 0) break;  //无数据可读则退出
                        }
                        //读取数据
                        int size = client.Receive(buffer, offset, buffer.Length - offset, SocketFlags.None);
                        offset += size;

                    } while (offset < buffer.Length);

                    if (offset > 0 && offset < buffer.Length)
                    {
                        //读取到的数据不够
                        Array.Resize<byte>(ref buffer, offset);
                    }
                }
                catch
                {
                    offset = 0;
                }
            }
            return offset != 0;
        }
        #endregion

        #region 发送信息
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="data"></param>
        internal static void Send(Socket client, byte[] data)
        {
            Send(client, data, 0, data.Length);
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>

        internal static void Send(Socket client, byte[] data, int offset, int size)
        {
            if (client.Connected)
            {
                try
                {
                    //查询是否允许写数据
                    if (client.Poll(TIMEOUT, SelectMode.SelectWrite))
                    {
                        //client.SendBufferSize = 10;
                        var sent = client.Send(data, offset, size, SocketFlags.Partial);
                        byte[] new_data = new byte[size - sent];
                        Array.Copy(data, sent, new_data, 0, size - sent);
                        if (sent < size)
                        {
                            Send(client, new_data, 0, size - sent);
                        }
                    }
                }
                catch { }
            }
        }
        #endregion
    }
}
