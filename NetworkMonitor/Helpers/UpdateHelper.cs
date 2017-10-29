using NetworkMonitor.Properties;
using Squirrel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;
using System.Reflection;

namespace NetworkMonitor.Helpers
{
    public static class UpdateHelper
    {
        private static readonly string BackupUserSettingsFilePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\..\user.config.backup";

        public static async Task UpdateApp(bool notify = false)
        {
            try
            {
                using (UpdateManager mgr = await UpdateManager.GitHubUpdateManager(Settings.Default.UpdateURL, prerelease: Settings.Default.UsePreReleases))
                {
                    UpdateInfo updates = await mgr.CheckForUpdate();
                    if (updates.ReleasesToApply.Count > 0)
                    {
                        ReleaseEntry lastVersion = updates.ReleasesToApply.OrderBy(x => x.Version).Last();

                        if (MessageBoxHelper.Ask(
                            string.Format("An update is available (v{0}). Would you like to install it?", lastVersion.Version)
                            ) == true)
                        {
                            await mgr.DownloadReleases(new[] { lastVersion });
                            await mgr.ApplyReleases(updates);
                            BackupSettings();
                            UpdateManager.RestartApp();
                        }
                        return;
                    }
                }
            }
            catch (InvalidOperationException)
            {

            }
            finally
            {
                if (notify)
                {
                    MessageBoxHelper.Info("No updates found.");
                }
            }
        }
        
        public static void BackupSettings()
        {
            string settingsFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
            File.Copy(settingsFile, BackupUserSettingsFilePath, true);
        }

        public static bool RestoreSettings()
        {
            string destFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
            string sourceFile = BackupUserSettingsFilePath;
            
            if (!File.Exists(sourceFile))
            {
                return true;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destFile)); // Create directory if doesn't exist
                File.Copy(sourceFile, destFile, true);
            }
            catch (IOException)
            {
                return false;
            }

            try
            {
                File.Delete(sourceFile);
            }
            catch (IOException)
            {

            }

            return true;
        }
    }
}
