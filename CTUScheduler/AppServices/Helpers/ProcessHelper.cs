using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Helpers;

public static class ProcessHelper
{
    public static void OpenUrl(string url)
    {
        try
        {
            if (TryOpenUrlWithWindows()) return;
            if (TryOpenUrlWithLinux()) return;
            if (TryOpenUrlWithMac()) return;

            throw new NotSupportedException("Unsupported OS");
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }

        bool TryOpenUrlWithWindows()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return false;
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                }
            };
            process.Start();
            return true;
        }

        bool TryOpenUrlWithLinux()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return false;
            try
            {
                Process.Start("xdg-open", url);
                return true;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                try
                {
                    // Nếu `xdg-open` không tồn tại, thử `x-www-browser`
                    Process.Start("x-www-browser", url);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
        bool TryOpenUrlWithMac()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return false;
            Process.Start("open", url);
            return true;
        }
    }
}