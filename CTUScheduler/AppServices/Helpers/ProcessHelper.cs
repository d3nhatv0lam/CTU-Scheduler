using System;
using System.Diagnostics;
namespace CTUScheduler.AppServices.Helpers;

public static class ProcessHelper
{
    
    /// <summary>
    /// Open url with default browser in Windows, Linux, Mac
    /// </summary>
    /// <param name="url">Web url</param>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public static void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) throw new ArgumentNullException(nameof(url));
        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute)) throw new ArgumentException("Invalid url", nameof(url));
        
        if (TryOpenUrl(url)) return;

        throw new NotSupportedException("Unsupported OS");
    }

    private static bool TryOpenUrl(string url)
    {
        return OperatingSystem.IsWindows() ? OpenUrlInWindows(url) :
            OperatingSystem.IsLinux() ? TryOpenUrlWithLinux(url) :
            OperatingSystem.IsMacOS() ? TryOpenUrlWithMac(url): false;
    }
    
    private static bool OpenUrlInWindows(string url)
    {
        if (!OperatingSystem.IsWindows()) return false;
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

    private static bool TryOpenUrlWithLinux(string url)
    {
        if (!OperatingSystem.IsLinux()) return false;
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
    private static bool TryOpenUrlWithMac(string url)
    {
        if (!OperatingSystem.IsMacOS()) return false;
        Process.Start("open", url);
        return true;
    }
}