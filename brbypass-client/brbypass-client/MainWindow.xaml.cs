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


        public MainWindow()
        {
            InitializeComponent();
            mainWindow = this;
        }

        private void btn_addProxyServer_Click(object sender, RoutedEventArgs e)
        {
            win_addProxyServer win_aps = new win_addProxyServer();
            win_aps.Show();
            this.IsEnabled = false;
        }
    }
}
