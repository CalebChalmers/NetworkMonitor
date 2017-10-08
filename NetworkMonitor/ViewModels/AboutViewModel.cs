using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NetworkMonitor.ViewModels
{
    public class AboutViewModel : ViewModelBase
    {
        private FileVersionInfo versionInfo;

        public AboutViewModel()
        {
            versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
        }

        public string Version
        {
            get
            {
                return versionInfo.FileVersion;
            }
        }

        public string Copyright
        {
            get
            {
                return versionInfo.LegalCopyright;
            }
        }
    }
}
