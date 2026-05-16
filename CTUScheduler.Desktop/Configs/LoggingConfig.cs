using System;
using CTUScheduler.Core.Helpers;
using Serilog;
using Serilog.Enrichers.ShortTypeName;

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
            .Enrich.FromLogContext()
            .Enrich.WithShortTypeName()
            .WriteTo.File(
                path: logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {ShortTypeName,-40} | {Message}{NewLine}{Exception}")
            .CreateLogger();
        LogHeader();
        PathProvider.CreateLogShortcut();
    }

    private static void LogHeader()
    {
        var headerLogger = Log.ForContext("ShortTypeName", "System");

        string separator = new string('=', 60);

        headerLogger.Information(separator);
        headerLogger.Information("CTU-SCHEDULER LOGGER (Started)");
        headerLogger.Information("Time: {Now:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
        headerLogger.Information(separator);
    }

    public static void CloseAndFlush()
    {
        Log.ForContext("ShortTypeName", "Host")
            .Information("================= LOG END =================");
        Log.CloseAndFlush();
    }
}