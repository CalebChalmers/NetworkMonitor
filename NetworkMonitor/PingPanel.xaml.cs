using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
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

namespace NetworkMonitor
{
    /// <summary>
    /// Interaction logic for PingPanel.xaml
    /// </summary>
    public partial class PingPanel : UserControl
    {
        public delegate void PingCompletedEventHandler(object sender, PingCompletedEventArgs e);
        public event PingCompletedEventHandler PingCompleted;

        public string Hostname { get; set; }
        public bool IsPinging { get; private set; }

        public ImageSource Icon
        {
            get { return img_icon.Source; }
            set { img_icon.Source = value; }
        }

        public string Value
        {
            get { return txt_value.Text; }
            set { txt_value.Text = value; }
        }

        public PingPanel()
        {
            IsPinging = false;
            InitializeComponent();
        }
        
        public void Ping()
        {
            if (IsPinging) return;
            Ping pinger = new Ping();
            pinger.PingCompleted += Ping_PingCompleted;
            try
            {
                IsPinging = true;
                pinger.SendAsync(Hostname, null);
            }
            catch (PingException)
            {
                // Discard Exception
            }
        }

        private void Ping_PingCompleted(object sender, PingCompletedEventArgs e)
        {
            IsPinging = false;

            if (e.Reply != null && e.Reply.Status == IPStatus.Success)
            {
                Value = e.Reply.RoundtripTime.ToString();
            }

            if (PingCompleted != null)
            {
                PingCompleted(sender, e);
            }
        }
    }
}
