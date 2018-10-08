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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using MahApps.Metro.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using brbypass_client.Model;

namespace brbypass_client
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        //window obj
        public static MainWindow mainWindow;

        //config path
        public static string startupPath = AppDomain.CurrentDomain.BaseDirectory;

        public MainWindow()
        {
            InitializeComponent();
            mainWindow = this;
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //check config file
            if (Directory.Exists(startupPath + "config")){
                if (File.Exists(startupPath+"config\\servers.json"))
                {
                    //init items
                    Server[] servers;
                    using (StreamReader jsonFile = File.OpenText(startupPath + "config\\servers.json"))
                    {
                        servers = JsonConvert.DeserializeObject<Server[]>(jsonFile.ReadToEnd());
                    }
                    if (servers.Length > 0)
                    {
                        for (int i = 0; i < servers.Length; i++)
                        {
                            cb_selectServer.Items.Add(servers[i].host);
                        }
                        //init selected item
                        int lastchoice = -1;
                        if (File.Exists(startupPath + "config\\lastChoice.json"))
                        {
                            using (StreamReader lastChoiceConfig = File.OpenText(startupPath + "config\\lastChoice.json"))
                            {
                                using (JsonTextReader reader = new JsonTextReader(lastChoiceConfig))
                                {
                                    JObject o = (JObject)JToken.ReadFrom(reader);
                                    try
                                    {
                                        lastchoice = (int)o["lastChoice"];
                                    } catch (Exception err)
                                    {
                                        
                                    }
                                }
                            }
                        }
                        cb_selectServer.SelectedIndex = lastchoice;
                    }
                    else
                    {
                        cb_selectServer.IsEnabled = false;
                    }
                } else
                {
                    //disable combobox when config is not found
                    cb_selectServer.IsEnabled = false;
                }
            } else
            {
                //disable combobox when config is not found
                cb_selectServer.IsEnabled = false;
                //create config directory
                Directory.CreateDirectory(startupPath + "config");
            }
        }

        private void btn_addProxyServer_Click(object sender, RoutedEventArgs e)
        {
            //open add proxy server window
            win_addProxyServer win_aps = new win_addProxyServer();
            win_aps.Show();
            this.IsEnabled = false;
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            //save last choice of server
            using (StreamWriter sw = new StreamWriter(startupPath + "config\\lastChoice.json"))
            {
                sw.WriteLine("{\"lastChoice\":" + cb_selectServer.SelectedIndex + ",\"lastChoiceHost\":\""+cb_selectServer.SelectedItem.ToString()+"\"}");
                sw.Flush();
                sw.Close();
            }
        }
    }
}
