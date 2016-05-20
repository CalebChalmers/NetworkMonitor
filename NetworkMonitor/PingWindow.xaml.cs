using NetworkMonitor.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace NetworkMonitor
{
    /// <summary>
    /// Interaction logic for PingWindow.xaml
    /// </summary>
    public partial class PingWindow : Window
    {
        public delegate void URLAddedEventHandler(string url);
        public event URLAddedEventHandler URLAdded;
        public delegate void URLRemovedEventHandler(int index);
        public event URLRemovedEventHandler URLRemoved;

        public PingWindow()
        {
            InitializeComponent();
            lbx_addresses.SelectionChanged += lbx_addresses_SelectionChanged;
            lbx_addresses.ItemsSource = Settings.Default.Stat_Ping;
        }

        private void lbx_addresses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btn_remove.IsEnabled = (lbx_addresses.SelectedIndex != -1);
        }

        private void btn_add_Click(object sender, RoutedEventArgs e)
        {
            string url = tbx_address.Text;

            Uri result;
            if(Uri.TryCreate(url, UriKind.Absolute, out result))
            {
                tbx_address.Clear();

                Settings.Default.Stat_Ping.Add(url);

                if (URLAdded != null)
                {
                    URLAdded(url);
                }

                lbx_addresses.Items.Refresh();
            }
            else
            {
                tbx_address.Focus();
                MessageBox.Show("Invalid URL", "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.ServiceNotification);
            }
        }

        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            int index = lbx_addresses.SelectedIndex;
            Settings.Default.Stat_Ping.RemoveAt(index);

            if (URLRemoved != null)
            {
                URLRemoved(index);
            }

            lbx_addresses.Items.Refresh();
        }
    }
}
