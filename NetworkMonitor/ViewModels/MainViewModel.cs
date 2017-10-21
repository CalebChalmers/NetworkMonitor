using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using NetworkMonitor.Helpers;
using NetworkMonitor.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace NetworkMonitor.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private const string ErrorText = "----";
        
        private string _send;
        private string _receive;
        private string _ping;

        private bool isPinging = false;
        private long prevSent = 0L;
        private long prevReceived = 0L;
        private DispatcherTimer timer;
        private NetworkInterface netInterface = null;
        private Ping pinger;

        public MainViewModel()
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                netInterface = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(i =>
                (i.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                i.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) &&
                i.OperationalStatus == OperationalStatus.Up);
            }
            
            if (netInterface == null)
            {
                MessengerInstance.Send(new FatalErrorMessage("No suitable network interfaces found."));
                return;
            }

            RunOnStartupCommand = new RelayCommand<bool>(ExecuteRunOnStartupCommand);
            timer = new DispatcherTimer();
            pinger = new Ping();

            MessengerInstance.Register<ClosingMessage>(this, ClosingMessageReceived);

            RefreshStats();

            // Setup main timer
            timer.Interval = TimeSpan.FromSeconds(Settings.Default.RefreshInterval);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        public string Send
        {
            get { return _send; }
            set
            {
                if(_send != value)
                {
                    _send = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string Receive
        {
            get { return _receive; }
            set
            {
                if (_receive != value)
                {
                    _receive = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string Ping
        {
            get { return _ping; }
            set
            {
                if (_ping != value)
                {
                    _ping = value;
                    RaisePropertyChanged();
                }
            }
        }

        public ICommand RunOnStartupCommand { get; private set; }

        private void ExecuteRunOnStartupCommand(bool runOnStartup)
        {
            if(runOnStartup)
            {
                RegistryHelper.AddStartupKey();
            }
            else
            {
                RegistryHelper.RemoveStartupKey();
            }
        }

        private void ClosingMessageReceived(ClosingMessage msg)
        {
            pinger.SendAsyncCancel();
            timer.Stop();
        }

        private void Timer_Tick(object sender, EventArgs args)
        {
            RefreshStats();
        }

        private void RefreshStats()
        {
            IPv4InterfaceStatistics stats = null;

            if (Settings.Default.Stat_Send || Settings.Default.Stat_Receive)
            {
                stats = netInterface.GetIPv4Statistics();

                if (Settings.Default.Stat_Send)
                {
                    if(stats.BytesReceived > 0)
                    {
                        long sent = stats.BytesSent * 8;
                        double sendSpeed = (sent - prevSent) / Settings.Default.RefreshInterval;
                        Send = GetSpeedText(sendSpeed);
                        prevSent = sent;
                    }
                    else
                    {
                        Send = ErrorText;
                    }
                }

                if (Settings.Default.Stat_Receive)
                {
                    if (stats.BytesReceived > 0)
                    {
                        long received = stats.BytesReceived * 8;
                        double receiveSpeed = (received - prevReceived) / Settings.Default.RefreshInterval;
                        Receive = GetSpeedText(receiveSpeed);
                        prevReceived = received;
                    }
                    else
                    {
                        Receive = ErrorText;
                    }
                }
            }

            if (Settings.Default.Stat_Ping)
            {
                DoPing();
            }
        }

        private string GetSpeedText(double bitsPerSecond)
        {
            string letter = "";

            if (bitsPerSecond >= 1e9d) // 10^9
            {
                bitsPerSecond /= 1e9d;
                letter = "G";
            }
            else if (bitsPerSecond >= 1e6d) // 10^6
            {
                bitsPerSecond /= 1e6d;
                letter = "M";
            }
            else if(bitsPerSecond >= 1e3d) // 10^3
            {
                bitsPerSecond /= 1e3d;
                letter = "K";
            }
            
            return string.Format("{0:0.0}{1}", bitsPerSecond, letter);
        }

        private void DoPing()
        {
            if (isPinging) return;
            pinger.PingCompleted += Ping_PingCompleted;
            try
            {
                isPinging = true;
                pinger.SendAsync(Settings.Default.PingAddress, Settings.Default.PingTimeout, null);
            }
            catch (PingException)
            {
                isPinging = false;
                Ping = ErrorText;
            }
        }

        private void Ping_PingCompleted(object sender, PingCompletedEventArgs e)
        {
            isPinging = false;

            if (e.Reply != null && e.Reply.Status == IPStatus.Success)
            {
                Ping = e.Reply.RoundtripTime.ToString();
            }
            else
            {
                Ping = ErrorText;
            }
        }
    }
}
