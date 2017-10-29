using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;
using NetworkMonitor.Properties;
using Squirrel;
using System.Diagnostics;
using NetworkMonitor.Helpers;
using NetworkMonitor.Windows;

namespace NetworkMonitor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const ShortcutLocation ShortcutLocations = ShortcutLocation.StartMenu;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
#if !DEBUG
            using (UpdateManager mgr = new UpdateManager(""))
            {
                SquirrelAwareApp.HandleEvents(                    
                    onFirstRun: OnFirstRun,
                    onInitialInstall: (v) => OnAppInitialInstall(v, mgr),
                    onAppUpdate: (v) => OnAppUpdate(v, mgr),
                    onAppUninstall: (v) => OnAppUninstall(v, mgr));
            }
#endif

            if(!UpdateHelper.RestoreSettings())
            {
                MessageBoxHelper.Error("There was an error restoring settings.");
            }

            new MainWindow().Show();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Settings.Default.Save();
        }

        private static void OnFirstRun()
        {
            MessageBoxHelper.Info("Install successful.");
        }

        private static void OnAppInitialInstall(Version version, UpdateManager mgr)
        {
            mgr.CreateShortcutsForExecutable(AssemblyHelper.AppFileName, ShortcutLocations, false);
            mgr.CreateUninstallerRegistryEntry();
        }

        private static void OnAppUpdate(Version version, UpdateManager mgr)
        {
            //mgr.CreateShortcutsForExecutable(FileVersionHelper.AppFileName, ShortcutLocations, true);
            //mgr.CreateUninstallerRegistryEntry();

            if (RegistryHelper.HasStartupKey)
            {
                RegistryHelper.AddStartupKey();
            }

            MessageBoxHelper.Info(string.Format("New version (v{0}) installed.", AssemblyHelper.FileVersionInfo.ProductVersion));
        }

        private static void OnAppUninstall(Version version, UpdateManager mgr)
        {
            mgr.RemoveShortcutsForExecutable(AssemblyHelper.AppFileName, ShortcutLocations);
            mgr.RemoveUninstallerRegistryEntry();

            RegistryHelper.RemoveStartupKey();

            MessageBoxHelper.Info("Uninstall successful.");
        }
    }
}
