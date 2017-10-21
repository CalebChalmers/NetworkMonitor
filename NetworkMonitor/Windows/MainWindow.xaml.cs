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
using GalaSoft.MvvmLight.Messaging;
using NetworkMonitor.ViewModels;
using NetworkMonitor.Helpers;

namespace NetworkMonitor.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            Messenger.Default.Register<FatalErrorMessage>(this, FatalError);
            
            InitializeComponent();
        }

        private void FatalError(FatalErrorMessage msg)
        {
            MessageBoxHelper.Error(msg.Message);
            Close();
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

        private void PingMenuItem_Click(object sender, RoutedEventArgs e)
        {
            PingWindow window = new PingWindow();
            window.Owner = this;
            window.ShowDialog();
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow window = new AboutWindow();
            window.Owner = this;
            window.ShowDialog();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Messenger.Default.Send(new ClosingMessage());
        }
    }
}
