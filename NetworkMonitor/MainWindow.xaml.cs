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
        private double updateInterval = 1;
        private string pingAddress = "www.google.com";
        private bool pinging = false;
        private bool autoExit = false;
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
            if(Properties.Settings.Default.Interface == "")
            {
                Properties.Settings.Default.Interface = interfaces[0].Id;
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
            if (!NetworkInterface.GetIsNetworkAvailable())
                return;

            long sent = selectedInterface.GetIPv4Statistics().BytesSent * 8;
            long received = selectedInterface.GetIPv4Statistics().BytesReceived * 8;

            long sendSpeed = (long)((sent - prevSent) * (1.0 / updateInterval));
            long receiveSpeed = (long)((received - prevReceived) * (1.0 / updateInterval));

            txt_send.Text = GetSpeedText(sendSpeed);
            txt_receive.Text = GetSpeedText(receiveSpeed);

            prevSent = sent;
            prevReceived = received;

            Ping();
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

        private void TopMost_Checked(object sender, RoutedEventArgs e)
        {
            ShowInTaskbar = false;
        }

        private void TopMost_Unchecked(object sender, RoutedEventArgs e)
        {
            ShowInTaskbar = true;
        }

        private void InterfaceSelect_Item_Click(object sender, RoutedEventArgs e)
        {
            foreach(MenuItem item in interfaceSelect.Items)
            {
                item.IsChecked = false;
            }

            MenuItem clicked = (MenuItem)sender;
            clicked.IsChecked = true;
            Properties.Settings.Default.Interface = (string)clicked.Tag;
            selectedInterface = GetSelectedInterface();
        }

        private NetworkInterface GetSelectedInterface()
        {
            return interfaces.Where(i => i.Id == Properties.Settings.Default.Interface).First();
        }
    }
}
