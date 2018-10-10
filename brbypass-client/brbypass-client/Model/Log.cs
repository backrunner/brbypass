using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace brbypass_client.Model
{
    public class Log
    {
        public string time;
        public long timestamp;
        public string content;

        //level:
        //0: unknown
        //1: debug
        //2: warning
        //3: error
        public short level;

        public Log()
        {
            DateTime t = DateTime.Now;
            time =  t.ToString();
            timestamp = GetTimestamp(t);
            content = "";
            level = 0;
        }

        public Log(string content)
        {
            DateTime t = DateTime.Now;
            time = t.ToString();
            timestamp = GetTimestamp(t);
            this.content = content;
            level = 0;
        }

        public Log(string content, short level)
        {
            DateTime t = DateTime.Now;
            time = t.ToString();
            timestamp = GetTimestamp(t);
            this.content = content;
            this.level = level;
        }

        public static long GetTimestamp(DateTime d)
        {
            TimeSpan ts = d - new DateTime(1970, 1, 1);
            return (long)(ts.TotalMilliseconds / 1000);
        }
    }
}
