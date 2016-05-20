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
        private long prevSent = 0L;
        private long prevReceived = 0L;

        private NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
        private NetworkInterface selectedInterface;

        private DispatcherTimer timer = new DispatcherTimer();

        private List<PingPanel> pingPanels = new List<PingPanel>();

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

            foreach (string url in Settings.Default.Stat_Ping)
            {
                CreatePingPanel(url);
            }

            // Setup main timer
            timer.Interval = TimeSpan.FromSeconds(updateInterval);
            timer.Tick += Timer_Tick;
            Timer_Tick(null, null);
            timer.Start();
        }

        private void PingPanel_PingCompleted(object sender, PingCompletedEventArgs e)
        {
            if (closing)
            {
                Close();
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            for(int i = 0; i < pingPanels.Count; i++)
            {
                pingPanels[i].Icon = await GetWebsiteIcon(Settings.Default.Stat_Ping[i]);
            }
        }

        private void Timer_Tick(object sender, EventArgs args)
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
            
            for (int i = 0; i < pingPanels.Count; i++)
            {
                pingPanels[i].Ping();
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool pinging = false;
            foreach(PingPanel pingPanel in pingPanels)
            {
                if(pingPanel.IsPinging)
                {
                    pinging = true;
                }
            }

            if (pinging)
            {
                e.Cancel = true;
                if (!closing)
                {
                    closing = true;
                    timer.Stop();
                    Hide();
                }
            }
            else
            {
                Settings.Default.Save();
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
            Settings.Default.Interface = (string)clicked.Tag;
            selectedInterface = GetSelectedInterface();
        }

        private NetworkInterface GetSelectedInterface()
        {
            return interfaces.Where(i => i.Id == Settings.Default.Interface).First();
        }

        private async Task<BitmapImage> GetWebsiteIcon(string url)
        {
            WebClient client = new WebClient();

            WebRequest request = WebRequest.Create("http://www.google.com/s2/favicons?domain_url=" + url);
            WebResponse response = await request.GetResponseAsync();
            using (System.IO.Stream stream = response.GetResponseStream())
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                Debug.WriteLine(bitmap == null);
                return bitmap;
            }
        }

        private PingPanel CreatePingPanel(string url)
        {
            PingPanel pingPanel = new PingPanel();
            pingPanel.Hostname = new Uri(url).Host;
            pingPanel.PingCompleted += PingPanel_PingCompleted;
            pingPanels.Add(pingPanel);
            statPanel.Children.Add(pingPanel);
            return pingPanel;
        }

        private void PingMenuItem_Click(object sender, RoutedEventArgs e)
        {
            PingWindow window = new PingWindow();

            window.URLAdded += async (url) =>
            {
                PingPanel pingPanel = CreatePingPanel(url);
                pingPanel.Icon = await GetWebsiteIcon(url);
            };

            window.URLRemoved += (i) =>
            {
                statPanel.Children.Remove(pingPanels[i]);
                pingPanels.RemoveAt(i);
            };

            window.Owner = this;
            window.ShowDialog();
        }
    }
}
