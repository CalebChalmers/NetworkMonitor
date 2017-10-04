using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
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
using System.Windows.Threading;
using NetworkMonitor.Properties;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Net;

namespace NetworkMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private double updateInterval = 1;
        private bool closing = false;
        private bool isPinging = false;
        private long prevSent = 0L;
        private long prevReceived = 0L;

        private NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
        private NetworkInterface selectedInterface;

        private DispatcherTimer timer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();

            if (interfaces.Length == 0)
                return;

            // Initialize selectedInterface and the Interface setting
            if(Settings.Default.Interface == "")
            {
                Settings.Default.Interface = interfaces[0].Id;
            }

            selectedInterface = GetSelectedInterface();

            // Fill interfaceSelect with interfaces
            foreach (NetworkInterface ni in interfaces)
            {
                MenuItem menuItem = new MenuItem();
                menuItem.Header = ni.Name;
                menuItem.Tag = ni.Id;
                menuItem.IsCheckable = true;
                menuItem.IsChecked = (ni.Id == selectedInterface.Id);
                menuItem.StaysOpenOnClick = true;
                menuItem.Click += InterfaceSelect_Item_Click;

                interfaceSelect.Items.Add(menuItem);
            }


            // Setup main timer
            timer.Interval = TimeSpan.FromSeconds(updateInterval);
            timer.Tick += Timer_Tick;
            Timer_Tick(null, null);
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs args)
        {
        }

        {
            IPv4InterfaceStatistics stats = null;

            if (Settings.Default.Stat_Send || Settings.Default.Stat_Receive)
            {
                stats = selectedInterface.GetIPv4Statistics();
            }

            if (Settings.Default.Stat_Send)
            {
                long sent = stats.BytesSent * 8;
                long sendSpeed = (long)((sent - prevSent) * (1.0 / updateInterval));
                txt_send.Text = GetSpeedText(sendSpeed);
                prevSent = sent;
            }

            if (Settings.Default.Stat_Receive)
            {
                long received = stats.BytesReceived * 8;
                long receiveSpeed = (long)((received - prevReceived) * (1.0 / updateInterval));
                txt_receive.Text = GetSpeedText(receiveSpeed);
                prevReceived = received;
            }

            if(Settings.Default.Stat_Ping)
            {
                Ping();
            }
        }

        private string GetSpeedText(decimal bitsPerSecond)
        {
            var ordinals = new[] { "", "K", "M", "G", "T", "P", "E" };
            var ordinal = 0;

            while (bitsPerSecond >= 1024)
            {
                bitsPerSecond /= 1024;
                ordinal++;
            }

            return String.Format("{0}{1}", bitsPerSecond.ToString("#.#"), ordinals[ordinal]);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void InterfaceSelect_Item_Click(object sender, RoutedEventArgs e)
        {
            foreach(MenuItem item in interfaceSelect.Items)
            {
                item.IsChecked = false;
            }

            MenuItem clicked = (MenuItem)sender;
            clicked.IsChecked = true;
            Settings.Default.Interface = (string)clicked.Tag;
            selectedInterface = GetSelectedInterface();
        }

        private NetworkInterface GetSelectedInterface()
        {
            return interfaces.Where(i => i.Id == Settings.Default.Interface).First();
        }

        private void Ping()
        {
            if (isPinging) return;
            Ping pinger = new Ping();
            pinger.PingCompleted += Ping_PingCompleted;
            try
            {
                isPinging = true;
                pinger.SendAsync(Settings.Default.PingAddress, null);
            }
            catch (PingException)
            {
                isPinging = false;
                PingError();
            }
        }

        private void Ping_PingCompleted(object sender, PingCompletedEventArgs e)
        {
            isPinging = false;

            if (e.Reply != null && e.Reply.Status == IPStatus.Success)
            {
                txt_ping.Text = e.Reply.RoundtripTime.ToString();
            }
            else
            {
                PingError();
            }
        }

        private void PingError()
        {
            txt_ping.Text = "---";
        }

        private void PingMenuItem_Click(object sender, RoutedEventArgs e)
        {
            PingWindow window = new PingWindow();
            window.Owner = this;
            window.ShowDialog();
        }
    }
}
