using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using brbypass_client.Model;

namespace brbypass_client.Controller
{
    public class ServerConfig
    {
        public static string Path = MainWindow.startupPath + "config\\servers.json";

        public static string[] ServerModes = {"Socks"};

        public static void SaveConfig(Server[] servers)
        {
            JsonSerializer serializer = new JsonSerializer();
            using (StreamWriter sw = new StreamWriter(Path))
            {
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, servers);
                }
            }
        }
    }
}
