using System;
using System.IO;

namespace CTUScheduler.Core.Shared.Helpers;

public static class StandardPathBuilder
{
    /// <summary>
    /// Constructs and returns the full roaming folder path for the application based on the operating system.
    /// </summary>
    /// <param name="publisherName">The name of the application's publisher, used on Windows systems to organize application data.</param>
    /// <param name="appNameWindows">The name of the application when running on Windows systems.</param>
    /// <param name="appNameUnix">The name of the application when running on Unix-based systems (e.g., macOS, Linux).</param>
    /// <returns>The full path to the application's roaming folder, organized based on the operating system's conventions.</returns>
    public static string GetRoamingPath(string publisherName, string appNameWindows, string appNameUnix)
    {
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        
        return OperatingSystem.IsWindows() 
            ? Path.Combine(roaming, publisherName, appNameWindows)
            : Path.Combine(roaming, appNameUnix); // Mac/Linux không dùng publisher
    }

    /// <summary>
    /// Constructs and returns the full local cache folder path for the application based on the operating system.
    /// </summary>
    /// <param name="publisherName">The name of the application's publisher, used on Windows systems to organize application data.</param>
    /// <param name="appNameWindows">The name of the application when running on Windows systems.</param>
    /// <param name="appNameUnix">The name of the application when running on Unix-based systems (e.g., macOS, Linux).</param>
    /// <returns>The full path to the application's local cache folder, organized based on the operating system's conventions.</returns>
    public static string GetLocalCachePath(string publisherName, string appNameWindows, string appNameUnix)
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        
        return OperatingSystem.IsWindows() 
            ? Path.Combine(local, publisherName, appNameWindows, "Cache")
            : Path.Combine(local, appNameUnix, "cache");
    }
    
    /// <summary>
    /// Constructs and returns the full path for storing application log files.
    /// Logs are stored locally (not synced) to save network bandwidth.
    /// </summary>
    public static string GetLogPath(string publisherName, string appNameWindows, string appNameUnix)
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        
        return OperatingSystem.IsWindows() 
            ? Path.Combine(local, publisherName, appNameWindows, "Logs")
            : Path.Combine(local, appNameUnix, "logs");
    }
    
    /// <summary>
    /// Constructs an application-specific temporary folder path. 
    /// Useful for short-lived files (e.g., extracting updates, temporary downloads).
    /// </summary>
    public static string GetAppSpecificTempPath(string publisherName, string appNameWindows, string appNameUnix)
    {
        var temp = Path.GetTempPath(); // C:\Users\<Name>\AppData\Local\Temp hoặc /tmp
        
        return OperatingSystem.IsWindows() 
            ? Path.Combine(temp, publisherName, appNameWindows)
            : Path.Combine(temp, appNameUnix);
    }
    
    /// <summary>
    /// Gets the base directory where the application executable is currently running.
    /// Perfect for accessing bundled read-only assets (e.g., default images, SQLite templates).
    /// </summary>
    public static string GetAppBaseDirectory()
    {
        // AppDomain.CurrentDomain.BaseDirectory hoặc AppContext.BaseDirectory đều ok
        return AppContext.BaseDirectory;
    }
    
    /// <summary>
    /// Cross-platform wrapper to get the current user's Downloads folder.
    /// </summary>
    public static string GetUserDownloadsPath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    }
    
    /// <summary>
    /// Cross-platform wrapper to get the current user's Desktop folder.
    /// </summary>
    public static string GetUserDesktopPath()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    }
    
    /// <summary>
    /// Constructs an application-specific folder within the current user's Documents folder.
    /// Ideal for user-generated content, exported reports, or saved projects.
    /// </summary>
    public static string GetAppDocumentsPath(string appNameWindows, string appNameUnix)
    {
        var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        
        return OperatingSystem.IsWindows() 
            ? Path.Combine(docs, appNameWindows)
            : Path.Combine(docs, appNameUnix);
    }
    
    /// <summary>
    /// Constructs an application-specific folder within the current user's Pictures folder.
    /// Useful if the application deals with image processing, screenshots, or media exports.
    /// </summary>
    public static string GetAppPicturesPath(string appNameWindows, string appNameUnix)
    {
        var pics = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
    
        return OperatingSystem.IsWindows() 
            ? Path.Combine(pics, appNameWindows)
            : Path.Combine(pics, appNameUnix);
    }
}