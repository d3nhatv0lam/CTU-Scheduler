using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Utilities
{
    public static class ProcessHelper
    {
        public static void OpenUrl(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    }
                };
                process.Start();
                return;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("x-www-browser", url);
                return;
            }
            // not MacOS
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) throw new NotSupportedException("Unsupported OS");

            Process.Start("open", url);
        }
    }
}
