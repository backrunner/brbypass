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
using MahApps.Metro.Controls;
using brbypass_client.Controller;
using brbypass_client.Model;
using System.Collections;

namespace brbypass_client
{
    /// <summary>
    /// win_Log.xaml 的交互逻辑
    /// </summary>
    public partial class win_Log : MetroWindow
    {
        public win_Log()
        {
            InitializeComponent();
        }

        private LogController lc = new LogController();

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //bind event
            LogController.BindEvent(new LogController.LogHandler(appendLog));
            //load all log
            for (int i = 0; i < LogController.logs.Count; i++)
            {
                Log t = (Log)LogController.logs[i];
                if (t != null)
                {
                    tb_log.Text += t.time + ": " + t.content + "\r\n";
                }
            }
            sv_log.ScrollToBottom();
        }

        private void appendLog(Log t)
        {
            var update = new Action(() => {tb_log.Text += t.time + ": " + t.content + "\r\n"; sv_log.ScrollToBottom(); LogController.logs.Remove(t); });
            this.BeginInvoke(update);            
        }

        private void btn_clear_Click(object sender, RoutedEventArgs e)
        {
            LogController.logs = new ArrayList();
            tb_log.Text = "";
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            MainWindow.logWindow = null;
        }
    }
}
