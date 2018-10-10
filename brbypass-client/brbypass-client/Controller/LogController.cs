using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using brbypass_client.Model;

namespace brbypass_client.Controller
{
    public class LogController
    {
        public static ArrayList logs = new ArrayList();
        public static LogController controller = new LogController();

        //event
        public delegate void LogHandler(Log log);
        public event LogHandler LogEvent;

        public void debug(string content)
        {
            Log t = new Log(content, 1);
            logs.Add(t);
            LogEvent?.Invoke(t);
        }
        public void warn(string content)
        {
            Log t = new Log(content, 2);
            logs.Add(t);
            LogEvent?.Invoke(t);
        }
        public void error(string content)
        {
            Log t = new Log(content, 3);
            logs.Add(t);
            LogEvent?.Invoke(t);
        }

        public static void Debug(string content)
        {
            controller.debug(content);
        }
        public static void Warn(string content)
        {
            controller.warn(content);
        }
        public static void Error(string content)
        {
            controller.error(content);
        }
        public static void BindEvent(LogHandler handler)
        {
            controller.LogEvent += handler;
        }

        public LogController()
        {

        }
    }
}
