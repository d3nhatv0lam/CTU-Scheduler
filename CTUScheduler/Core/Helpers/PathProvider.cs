using System;
using System.IO;

namespace CTUScheduler.Core.Helpers;

public static class PathProvider
{
    private static readonly string AppName = "ctu-scheduler";

    public static string GetAppDataRoot()
    {
        // C:\Users\{username}\AppData\Local
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var path = Path.Combine(root, AppName);
        
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        return path;
    }
    
    public static string GetLogFolder()
    {
        var path = Path.Combine(GetAppDataRoot(), "logs");
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        return path;
    }

    public static string GetLogPath() => Path.Combine(GetLogFolder(), "log-.txt");

    public static string GetPlaywrightBrowserPath() 
    {
        var path = Path.Combine(GetAppDataRoot(), "playwright-browsers");
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        return path;
    }
    
    /// <summary>
    /// Tạo file shortcut "Open Logs" ngay cạnh file .exe của App
    /// </summary>
    public static void CreateLogShortcut()
    {
        if (!OperatingSystem.IsWindows()) return;
        try
        {
            var appDir = AppContext.BaseDirectory;
            var shortcutPath = Path.Combine(appDir, "Logs.url");
            
            var targetDir = GetLogFolder();
            
            using StreamWriter writer = new StreamWriter(shortcutPath);
            writer.WriteLine("[InternetShortcut]");
            writer.WriteLine("URL=file:///" + targetDir.Replace('\\', '/')); // Quan trọng: file:///
            writer.WriteLine("IconIndex=0");
            
            string? exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exePath))
            {
                writer.WriteLine("IconFile=" + exePath.Replace('\\', '/'));
            }
            
            writer.Flush();
        }
        catch 
        {
            // Lỗi tạo shortcut không quan trọng, bỏ qua để tránh crash app
        }
    }
}