using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using brbypass_client.Model;
using brbypass_client.Controller;

namespace brbypass_client
{
    /// <summary>
    /// win_addProxyServer.xaml 的交互逻辑
    /// </summary>
    public partial class win_addProxyServer : MetroWindow
    {
        private string serverConfigPath;

        public win_addProxyServer()
        {
            InitializeComponent();
            serverConfigPath = MainWindow.startupPath + "config\\servers.json";
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            MainWindow.mainWindow.IsEnabled = true;
        }

        private void btn_AddServer_Save_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(serverConfigPath))
            {
                Server[] servers;
                using (StreamReader jsonFile = File.OpenText(serverConfigPath))
                {
                    servers = JsonConvert.DeserializeObject<Server[]>(jsonFile.ReadToEnd());
                }

                //append new server
                Server server = new Server();
                server.host = txt_host.Text.Trim();
                //check repeat
                for (int i = 0; i < servers.Length; i++)
                {
                    if (servers[i].host.Equals(server.host))
                    {
                        this.ShowMessageAsync("Error", "The host is existed, please check your input.");
                        return;
                    }
                }
                server.password = txt_password.Password.Trim();
                server.port = Convert.ToInt32(txt_port.Text.Trim()) ;
                server.localPort = Convert.ToInt32(txt_localPort.Text.Trim());
                //append the new one to array
                Server[] new_servers = new Server[servers.Length + 1];
                Array.Copy(servers, 0, new_servers, 0, servers.Length);
                new_servers[new_servers.Length - 1] = server;
                //save config
                ServerConfig.SaveConfig(new_servers);
                this.Close();
                //add to mainWindow's combobox
                MainWindow.mainWindow.cb_selectServer.Items.Add(server.host);
                MainWindow.mainWindow.servers = new_servers;
            } else
            {
                Server server = new Server();
                server.host = txt_host.Text.Trim();
                server.password = txt_password.Password.Trim();
                server.port = Convert.ToInt32(txt_port.Text.Trim());
                server.localPort = Convert.ToInt32(txt_localPort.Text.Trim());
                //create array
                Server[] servers = new Server[1];
                servers[0] = server;
                //serialize to json
                ServerConfig.SaveConfig(servers);
                this.Close();
                //Add to mainWindow's combobox
                MainWindow.mainWindow.cb_selectServer.Items.Add(server.host);
                MainWindow.mainWindow.servers = servers;
            }
        }
    }
}
