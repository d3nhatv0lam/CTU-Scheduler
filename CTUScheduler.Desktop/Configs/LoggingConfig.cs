using System;
using CTUScheduler.Core.Helpers;
using Serilog;

namespace CTUScheduler.Desktop.Configs;

public static class LoggingConfig
{
    public static void Init()
    {
        var logPath = PathProvider.GetLogPath();

        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
            .WriteTo.Debug()
#else
        .MinimumLevel.Information()
#endif
            .WriteTo.File(
                path: logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message}{NewLine}{Exception}"
            )
            .CreateLogger();
        LogHeader();
        PathProvider.CreateLogShortcut();
    }

    private static void LogHeader()
    {
        string separator = new string('=', 60);
        Log.Information($"{separator}");
        Log.Information("    CTU-SCHEDULER LOGGER (Started)");
        Log.Information($"    Time: {DateTime.Now}");
        Log.Information($"{separator}");
    }
    
    public static void Flush()
    {
        Log.CloseAndFlush();
    }
}