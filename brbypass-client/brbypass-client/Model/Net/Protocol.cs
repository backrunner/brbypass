using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace brbypass_client.Model.Net
{
    public class Protocol
    {
        public class Header
        {
            public int contentLength;

            public Header(byte[] header)
            {
                if (header.Length == 6)
                {
                    if (header[0] == 98 && header[1] == 114 && header[2] == 98 && header[3] == 112)
                    {
                        if (header[4] != 0)
                        {
                            string length = Convert.ToString(header[4], 16) + Convert.ToString(header[5], 16);
                            contentLength = Convert.ToInt32(length, 16);
                        } else
                        {
                            contentLength = header[5];
                        }                        
                    } else
                    {
                        contentLength = -1;
                    }
                }
                else
                {
                    contentLength = -1;
                }
            }
        }
    }
}
