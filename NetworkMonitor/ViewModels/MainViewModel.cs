using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
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
        private const double UpdateInterval = 1;
        private const int PingTimeout = 3000;
        private const string ErrorText = "----";
        
        private string _send;
        private string _receive;
        private string _ping;
        private NetworkInterface _netInterface;
        private NetworkInterface[] _netInterfaces;

        private bool isPinging = false;
        private long prevSent = 0L;
        private long prevReceived = 0L;
        private DispatcherTimer timer = new DispatcherTimer();
        private Ping pinger;

        public MainViewModel()
        {
            ChangeNetInterface = new RelayCommand<NetworkInterface>(ExecuteChangeNetInterface);

            pinger = new Ping();

            _netInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            
            if (NetInterfaces.Length == 0)
            {
                MessengerInstance.Send(new FatalErrorMessage("No network interfaces found."));
                return;
            }

            NetworkInterface ni = NetInterfaces.FirstOrDefault(i => i.Id == Settings.Default.Interface);
            if (ni == null)
            {
                _netInterface = NetInterfaces[0];
                Settings.Default.Interface = _netInterface.Id;
            }
            else
            {
                _netInterface = ni;
            }

            MessengerInstance.Register<ClosingMessage>(this, ClosingMessageReceived);

            UpdateStats();

            // Setup main timer
            timer.Interval = TimeSpan.FromSeconds(UpdateInterval);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        public NetworkInterface[] NetInterfaces
        {
            get { return _netInterfaces; }
        }

        public NetworkInterface NetInterface
        {
            get { return _netInterface; }
            set
            {
                if (_netInterface != value)
                {
                    _netInterface = value;
                    RaisePropertyChanged();
                }
            }
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

        public ICommand ChangeNetInterface { get; private set; }

        private void ExecuteChangeNetInterface(NetworkInterface netInterface)
        {
            NetInterface = netInterface;
            Settings.Default.Interface = netInterface.Id;
        }

        private void ClosingMessageReceived(ClosingMessage msg)
        {
            pinger.SendAsyncCancel();
            timer.Stop();
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
                stats = NetInterface.GetIPv4Statistics();

                if (Settings.Default.Stat_Send)
                {
                    if(stats.BytesReceived > 0)
                    {
                        long sent = stats.BytesSent * 8;
                        double sendSpeed = (sent - prevSent) / UpdateInterval;
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
                        double receiveSpeed = (received - prevReceived) / UpdateInterval;
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
            var ordinals = new[] { "", "K", "M", "G", "T", "P", "E" };
            var ordinal = 0;

            while (bitsPerSecond >= 1024)
            {
                bitsPerSecond /= 1024;
                ordinal++;
            }

            if(bitsPerSecond >= 1000) // Keep to 4 digits
            {
                bitsPerSecond = Math.Round(bitsPerSecond);
            }

            return String.Format("{0}{1}", bitsPerSecond.ToString("0.#"), ordinals[ordinal]);
        }

        private void DoPing()
        {
            if (isPinging) return;
            pinger.PingCompleted += Ping_PingCompleted;
            try
            {
                isPinging = true;
                pinger.SendAsync(Settings.Default.PingAddress, PingTimeout, null);
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
