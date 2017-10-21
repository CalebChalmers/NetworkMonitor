using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NetworkMonitor.Helpers
{
    public static class FileVersionHelper
    {
        public static FileVersionInfo _fileVersionInfo = null;

        public static FileVersionInfo FileVersionInfo
        {
            get
            {
                if(_fileVersionInfo == null)
                {
                    _fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
                }
                return _fileVersionInfo;
            }
        }
    }
}
