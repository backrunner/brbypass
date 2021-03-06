﻿using System;
using System.Collections;
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
using System.Net.NetworkInformation;
using brbypass_client.Controller;
using brbypass_client.Controller.Net;

namespace brbypass_client
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        //window obj
        public static MainWindow mainWindow;
        public win_Log logWindow;
        public win_addProxyServer win_aps;
        public win_manageProxyServer win_mps;

        //config path
        public static string startupPath = AppDomain.CurrentDomain.BaseDirectory;

        public MainWindow()
        {
            InitializeComponent();
            mainWindow = this;
        }

        public List<Server> servers = new List<Server>();

        //net obj
        private SocksServer socksServer;

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //check config file
            if (Directory.Exists(startupPath + "config"))
            {
                if (File.Exists(ServerConfig.Path))
                {
                    //init items    
                    Server[] tempServers;
                    using (StreamReader jsonFile = File.OpenText(ServerConfig.Path))
                    {
                        tempServers = JsonConvert.DeserializeObject<Server[]>(jsonFile.ReadToEnd());
                    }
                    if (tempServers.Length > 0)
                    {
                        for (int i = 0; i < tempServers.Length; i++)
                        {
                            servers.Add(tempServers[i]);
                            cb_selectServer.Items.Add(tempServers[i].host);
                        }
                        //init selected item
                        int lastchoice = -1;
                        if (File.Exists(startupPath + "config\\lastChoice.json"))
                        {
                            using (StreamReader lastChoiceConfig = File.OpenText(startupPath + "config\\lastChoice.json"))
                            {
                                using (JsonTextReader reader = new JsonTextReader(lastChoiceConfig))
                                {
                                    try
                                    {
                                        JObject o = (JObject)JToken.ReadFrom(reader);
                                        lastchoice = (int)o["lastChoice"];
                                    }
                                    catch (Exception ex)
                                    {
                                        LogController.Error("Load last choice error: " + ex.Message);
                                    }
                                }
                            }
                        }
                        if (lastchoice >= cb_selectServer.Items.Count || lastchoice < 0)
                        {
                            cb_selectServer.SelectedIndex = 0;
                        }
                        else
                        {
                            cb_selectServer.SelectedIndex = lastchoice;
                        }
                    }
                    else
                    {
                        cb_selectServer.IsEnabled = false;
                    }
                }
                else
                {
                    //disable combobox when config is not found
                    cb_selectServer.IsEnabled = false;
                }
            }
            else
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
            if (win_aps != null)
            {
                win_aps.Focus();
            }
            else
            {
                win_aps = new win_addProxyServer();
                win_aps.Show();
                this.IsEnabled = false;
            }
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            //save last choice of server
            using (StreamWriter sw = new StreamWriter(startupPath + "config\\lastChoice.json"))
            {
                sw.WriteLine("{\"lastChoice\":" + cb_selectServer.SelectedIndex + ",\"lastChoiceHost\":\"" + cb_selectServer.SelectedItem.ToString() + "\"}");
                sw.Flush();
                sw.Close();
            }
        }

        //ping delay
        private double averagePingDelay = 0;
        private short nowPingTest = 0;
        private short successPingTest = 0;

        private void btn_test_Click(object sender, RoutedEventArgs e)
        {
            //init ping
            Ping ping = new Ping();
            averagePingDelay = 0;
            nowPingTest = 0;
            successPingTest = 0;
            //send ping
            if (cb_selectServer.SelectedIndex != -1)
            {
                for (int i = 0; i < 8; i++)
                {
                    sendPingToHost(cb_selectServer.SelectedItem.ToString());
                }
                //disable button
                btn_test.IsEnabled = false;
            }
            else
            {
                lbl_pingDelay.Content = "Empty Item";
            }
        }

        private async void sendPingToHost(string hostname)
        {
            Ping ping = new Ping();
            PingReply reply = await ping.SendPingAsync(hostname, 1000);
            nowPingTest++;
            if (reply.Status == IPStatus.Success)
            {
                successPingTest++;
                averagePingDelay += reply.RoundtripTime;
            }
            if (nowPingTest == 8)
            {
                if (successPingTest > 0)
                {
                    btn_test.IsEnabled = true;
                    averagePingDelay = averagePingDelay / successPingTest;
                    lbl_pingDelay.Content = ((int)averagePingDelay).ToString() + "ms";
                    LogController.Debug("Send Ping to \"" + hostname + "\": " + ((int)averagePingDelay).ToString() + "ms" + ".");
                }
                else
                {
                    lbl_pingDelay.Content = "Failed";
                    LogController.Error("Send Ping to \"" + hostname + "\" Failed.");
                    btn_test.IsEnabled = true;
                }
            }
        }

        private void btn_log_Click(object sender, RoutedEventArgs e)
        {
            if (logWindow != null)
            {
                logWindow.Focus();
            }
            else
            {
                logWindow = new win_Log();
                logWindow.Show();
            }
        }

        private void btn_start_Click(object sender, RoutedEventArgs e)
        {
            if (cb_selectServer.SelectedIndex != -1)
            {
                cb_selectServer.IsEnabled = false;
                //lock buttons
                btn_start.IsEnabled = false;
                btn_stop.IsEnabled = false;

                switch ((servers[cb_selectServer.SelectedIndex]).mode)
                {
                    case 1:
                        socksServer = new SocksServer((ushort)(servers[cb_selectServer.SelectedIndex]).localPort);
                        socksServer.Start((servers[cb_selectServer.SelectedIndex]).host, (servers[cb_selectServer.SelectedIndex]).port, (servers[cb_selectServer.SelectedIndex]).password);
                        break;
                }

            }
        }

        //ui update method
        public void updateUI_startFailed()
        {
            var update = new Action(() =>
            {
                btn_start.IsEnabled = true;
                cb_selectServer.IsEnabled = true;
                btn_stop.Visibility = Visibility.Hidden;
                btn_start.Visibility = Visibility.Visible;
            });
            this.BeginInvoke(update);
        }

        public void updateUI_startSucceed()
        {
            var update = new Action(() =>
            {
                btn_start.Visibility = Visibility.Hidden;
                btn_stop.Visibility = Visibility.Visible;
                btn_stop.IsEnabled = true;
            });
            this.BeginInvoke(update);
        }

        private void btn_stop_Click(object sender, RoutedEventArgs e)
        {
            cb_selectServer.IsEnabled = true;
            btn_start.IsEnabled = true;
            btn_start.Visibility = Visibility.Visible;
            btn_stop.Visibility = Visibility.Hidden;
            switch ((servers[cb_selectServer.SelectedIndex]).mode)
            {
                case 1:
                    socksServer.Stop();
                    break;
            }
        }

        private void Btn_deleteProxyServer_Click(object sender, RoutedEventArgs e)
        {
            int index = cb_selectServer.SelectedIndex;
            servers.Remove(servers[cb_selectServer.SelectedIndex]);
            ServerConfig.SaveConfig(servers.ToArray());
            //update ui
            cb_selectServer.Items.RemoveAt(index);
            if (index >= cb_selectServer.Items.Count)
            {
                cb_selectServer.SelectedIndex = cb_selectServer.Items.Count - 1;
            }
            else
            {
                cb_selectServer.SelectedIndex = index;
            }
        }

        private void Btn_manageServers_Click(object sender, RoutedEventArgs e)
        {
            if (win_mps != null)
            {
                win_mps.Focus();
            }
            else
            {
                win_mps = new win_manageProxyServer();
                win_mps.Show();
                this.IsEnabled = false;
            }
        }
    }
}
