using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NetworkMonitor.Helpers
{
    public static class RegistryHelper
    {
        private const string AppId = "NetworkMonitor";

        private static RegistryKey _runKey = null;

        public static RegistryKey RunKey
        {
            get
            {
                if(_runKey == null)
                {
                    _runKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                }
                return _runKey;
            }
        }

        public static bool HasStartupKey
        {
            get
            {
                return RunKey.GetValue(AppId) != null;
            }
        }

        public static void AddStartupKey()
        {
            RunKey.SetValue(AppId, string.Format("\"{0}\"", Assembly.GetExecutingAssembly().Location));
        }

        public static void RemoveStartupKey()
        {
            RunKey.DeleteValue(AppId, false);
        }
    }
}
