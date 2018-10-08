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
    }
}
