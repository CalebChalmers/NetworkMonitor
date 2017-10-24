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

namespace NetworkMonitor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const ShortcutLocation ShortcutLocations = ShortcutLocation.StartMenu;

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            if(Settings.Default.UpgradeRequired)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpgradeRequired = false;
                Settings.Default.Save();
            }

            using (UpdateManager mgr = await UpdateHelper.GetUpdateManager())
            {
                if(mgr != null)
                {
                    SquirrelAwareApp.HandleEvents(
                        onInitialInstall: (v) => OnAppInitialInstall(v, mgr),
                        onAppUpdate: (v) => OnAppUpdate(v, mgr),
                        onAppUninstall: (v) => OnAppUninstall(v, mgr));

                    await UpdateHelper.UpdateApp(mgr);
                }
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
            mgr.CreateShortcutsForExecutable(FileVersionHelper.AppFileName, ShortcutLocations, false);
            mgr.CreateUninstallerRegistryEntry();
        }

        private static void OnAppUpdate(Version version, UpdateManager mgr)
        {
            MessageBoxHelper.Info(string.Format("New version (v{0}) installed.", version));

            mgr.CreateShortcutsForExecutable(FileVersionHelper.AppName, ShortcutLocations, true);
            mgr.CreateUninstallerRegistryEntry();

            if (RegistryHelper.HasStartupKey)
            {
                RegistryHelper.AddStartupKey();
            }
        }

        private static void OnAppUninstall(Version version, UpdateManager mgr)
        {
            mgr.RemoveShortcutsForExecutable(FileVersionHelper.AppFileName, ShortcutLocations);
            mgr.RemoveUninstallerRegistryEntry();

            RegistryHelper.RemoveStartupKey();
        }
    }
}
