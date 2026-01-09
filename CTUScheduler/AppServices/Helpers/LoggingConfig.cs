using System;
using Serilog;

namespace CTUScheduler.AppServices.Helpers;

public static class LoggingConfig
{
    public static void Init()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                path: "logs/log-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}"
            )
            .WriteTo.Debug()
            .CreateLogger();
                
        LogHeader();
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