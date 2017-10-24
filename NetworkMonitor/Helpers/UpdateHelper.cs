using NetworkMonitor.Properties;
using Squirrel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkMonitor.Helpers
{
    public static class UpdateHelper
    {
        public static async Task UpdateApp(bool notify = false)
        {
#if !DEBUG
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
                            //UpdateManager.RestartApp();
                        }
                    }
                    else if (notify)
                    {
                        MessageBoxHelper.Info("No updates found.");
                    }
                }
            }
            catch (InvalidOperationException e)
            {
                Debug.WriteLine("No releases found!");
                Debug.WriteLine(e);

                if (notify)
                {
                    MessageBoxHelper.Error("There was a problem checking for updates.");
                }
            }
#endif
        }
    }
}
