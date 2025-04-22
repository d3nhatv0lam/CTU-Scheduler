using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Helpers
{
    public static class ProcessHelper
    {
        public static void OpenUrl(string url)
        {
            try
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
                    try
                    {
                        Process.Start("xdg-open", url);
                    }
                    catch (System.ComponentModel.Win32Exception)
                    {
                        // Nếu `xdg-open` không tồn tại, thử `x-www-browser`
                        Process.Start("x-www-browser", url);
                    }
                    return;
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                    return;
                }
                throw new NotSupportedException("Unsupported OS");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
    }
}
