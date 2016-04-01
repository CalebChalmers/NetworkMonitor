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

namespace NetworkMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private RegistryKey runKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        private double updateInterval = 1;
        private string pingAddress = "www.google.com";
        private bool pinging = false;
        private bool autoExit = false;
        private long prevSent = 0L;
        private long prevReceived = 0L;

        private DispatcherTimer timer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();
            
            timer.Interval = TimeSpan.FromSeconds(updateInterval);
            timer.Tick += Timer_Tick;
            Timer_Tick(null, null);
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs args)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return;

            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface ni in interfaces)
            {
                if(ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet && ni.Name == "Ethernet")
                {
                    long sent = ni.GetIPv4Statistics().BytesSent * 8;
                    long received = ni.GetIPv4Statistics().BytesReceived * 8;

                    long sendSpeed = (long)((sent - prevSent) * (1.0 / updateInterval));
                    long receiveSpeed = (long)((received - prevReceived) * (1.0 / updateInterval));

                    txt_send.Text = GetSpeedText(sendSpeed);
                    txt_receive.Text = GetSpeedText(receiveSpeed);

                    prevSent = sent;
                    prevReceived = received;

                    Ping();
                }
            }
        }
        
        private void Ping()
        {
            Ping pinger = new Ping();
            pinger.PingCompleted += Ping_PingCompleted;
            try
            {
                if (!pinging)
                {
                    pinger.SendAsync(pingAddress, null);
                    pinging = true;
                }
            }
            catch (PingException)
            {
                // Discard PingExceptions and return false;
            }
        }

        private void Ping_PingCompleted(object sender, PingCompletedEventArgs e)
        {
            pinging = false;

            if (e.Reply.Status == IPStatus.Success)
            {
                txt_ping.Text = e.Reply.RoundtripTime.ToString();
            }

            if (autoExit)
            {
                Close();
            }
        }

        private string GetSpeedText(decimal bitsPerSecond)
        {
            var ordinals = new[] { "", "K", "M", "G", "T", "P", "E" };
            var ordinal = 0;

            while (bitsPerSecond > 1024)
            {
                bitsPerSecond /= 1024;
                ordinal++;
            }

            return String.Format("{0}{1}", bitsPerSecond.ToString("#.#"), ordinals[ordinal]);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (pinging)
            {
                e.Cancel = true;
                if (!autoExit)
                {
                    autoExit = true;
                    timer.Stop();
                    Hide();
                }
            }
            else
            {
                Properties.Settings.Default.Save();
            }
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

        private void itmRunOnStartup_Checked(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Adding startup key to system registry...");
            Assembly curAssembly = Assembly.GetExecutingAssembly();
            runKey.SetValue(curAssembly.GetName().Name, curAssembly.Location);
        }

        private void itmRunOnStartup_Unchecked(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Removing startup key from system registry...");
            runKey.DeleteValue(Assembly.GetExecutingAssembly().GetName().Name, false);
        }
    }
}
