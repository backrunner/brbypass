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
using brbypass_client.Model;
using brbypass_client.Controller;
using MahApps.Metro.Controls;

namespace brbypass_client
{
    /// <summary>
    /// win_manageProxyServer.xaml 的交互逻辑
    /// </summary>
    public partial class win_manageProxyServer : MetroWindow
    {
        public win_manageProxyServer()
        {
            InitializeComponent();
        }

        private List<int> needToBeRemoved = new List<int>();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (Server server in MainWindow.mainWindow.servers)
            {
                lb_servers.Items.Add(server.host);
            }
            foreach (string mode in ServerConfig.ServerModes)
            {
                cb_mode.Items.Add(mode);
            }
            try
            {
                lb_servers.SelectedIndex = MainWindow.mainWindow.cb_selectServer.SelectedIndex;
            } catch (Exception ex)
            {
                LogController.Error("Manage Window Error: " + ex.Message);
            }
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            MainWindow.mainWindow.IsEnabled = true;
            MainWindow.mainWindow.win_mps = null;
        }

        private void Lb_servers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Server t = MainWindow.mainWindow.servers[lb_servers.SelectedIndex];
            tb_host.Text = t.host;
            tb_port.Text = t.port.ToString();
            tb_localPort.Text = t.localPort.ToString();
            tb_password.Password = t.password.ToString();
            cb_mode.SelectedIndex = t.mode - 1;
        }

        private void Btn_delServer_Click(object sender, RoutedEventArgs e)
        {
            var index = lb_servers.SelectedIndex;
            needToBeRemoved.Add(index);
            lb_servers.Items.RemoveAt(index);
            if (index >= lb_servers.Items.Count)
            {
                lb_servers.SelectedIndex = lb_servers.Items.Count - 1;
            } else
            {
                lb_servers.SelectedIndex = index;
            }
            btn_save.IsEnabled = true;
        }

        private void Tb_host_TextChanged(object sender, TextChangedEventArgs e)
        {
            MainWindow.mainWindow.servers[lb_servers.SelectedIndex].host = tb_host.Text;
            btn_save.IsEnabled = true;
        }

        private void Tb_port_TextChanged(object sender, TextChangedEventArgs e)
        {
            int port = -1;
            int.TryParse(tb_port.Text,out port);
            if (port >= 0) {
                MainWindow.mainWindow.servers[lb_servers.SelectedIndex].port = port;
            }
            btn_save.IsEnabled = true;
        }

        private void Tb_password_PasswordChanged(object sender, RoutedEventArgs e)
        {
            MainWindow.mainWindow.servers[lb_servers.SelectedIndex].password = tb_password.Password;
            btn_save.IsEnabled = true;
        }

        private void Tb_localPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            int port = -1;
            int.TryParse(tb_port.Text, out port);
            if (port >= 0)
            {
                MainWindow.mainWindow.servers[lb_servers.SelectedIndex].localPort = port;
            }
            btn_save.IsEnabled = true;
        }

        private void Cb_mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MainWindow.mainWindow.servers[lb_servers.SelectedIndex].mode = cb_mode.SelectedIndex + 1;
        }

        private void Btn_save_Click(object sender, RoutedEventArgs e)
        {
            if (needToBeRemoved.Count > 0)
            {
                var index = MainWindow.mainWindow.cb_selectServer.SelectedIndex;
                foreach(int t in needToBeRemoved)
                {
                    MainWindow.mainWindow.servers.RemoveAt(t);
                    MainWindow.mainWindow.cb_selectServer.Items.RemoveAt(t);
                }
                if (index > MainWindow.mainWindow.cb_selectServer.Items.Count)
                {
                    MainWindow.mainWindow.cb_selectServer.SelectedIndex = MainWindow.mainWindow.cb_selectServer.Items.Count - 1;
                } else
                {
                    MainWindow.mainWindow.cb_selectServer.SelectedIndex = index;
                }
            }
            ServerConfig.SaveConfig(MainWindow.mainWindow.servers.ToArray());
            this.Close();
        }
    }
}
