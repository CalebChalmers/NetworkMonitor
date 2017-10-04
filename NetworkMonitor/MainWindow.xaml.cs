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
        private const double updateInterval = 1;
        
        private bool isPinging = false;
        private long prevSent = 0L;
        private long prevReceived = 0L;

        private NetworkInterface[] netInterfaces;
        private NetworkInterface netInterface;

        private DispatcherTimer timer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();

            netInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            if (netInterfaces.Length == 0)
            {
                Close();
                return;
            }

            // Initialize the selected interface and the Interface setting
            if(Settings.Default.Interface == "")
            {
                netInterface = netInterfaces[0];
                Settings.Default.Interface = netInterface.Id;
            }
            else
            {
                netInterface = GetSelectedInterface();
            }

            // Fill interfaceSelect with interfaces
            foreach (NetworkInterface ni in netInterfaces)
            {
                MenuItem menuItem = new MenuItem();
                menuItem.Header = ni.Name;
                menuItem.Tag = ni.Id;
                menuItem.ToolTip = ni.Description;
                menuItem.IsChecked = (ni.Id == netInterface.Id);
                menuItem.Click += InterfaceSelect_Item_Click;
                menuItem.IsCheckable = true;
                menuItem.StaysOpenOnClick = true;

                interfaceSelect.Items.Add(menuItem);
            }

            UpdateStats();

            // Setup main timer
            timer.Interval = TimeSpan.FromSeconds(updateInterval);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs args)
        {
            UpdateStats();
        }

        private void UpdateStats()
        {
            IPv4InterfaceStatistics stats = null;

            if (Settings.Default.Stat_Send || Settings.Default.Stat_Receive)
            {
                stats = netInterface.GetIPv4Statistics();

                if (Settings.Default.Stat_Send)
                {
                    long sent = stats.BytesSent * 8;
                    double sendSpeed = (sent - prevSent) / updateInterval;
                    txt_send.Text = GetSpeedText(sendSpeed);
                    prevSent = sent;
                }

                if (Settings.Default.Stat_Receive)
                {
                    long received = stats.BytesReceived * 8;
                    double receiveSpeed = (received - prevReceived) / updateInterval;
                    txt_receive.Text = GetSpeedText(receiveSpeed);
                    prevReceived = received;
                }
            }

            if(Settings.Default.Stat_Ping)
            {
                Ping();
            }
        }

        private string GetSpeedText(double bitsPerSecond)
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
            netInterface = GetSelectedInterface();
        }

        private NetworkInterface GetSelectedInterface()
        {
            return netInterfaces.FirstOrDefault(i => i.Id == Settings.Default.Interface) ?? netInterfaces[0];
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
