using System;
using Serilog;
using Serilog.Events;
using Velopack.Logging;

namespace CTUScheduler.Desktop.Configs;

public class VelopackSerilogAdapter : IVelopackLogger
{
    private readonly ILogger _logger = Serilog.Log.ForContext("ShortTypeName", "Velopack");

    public void Log(VelopackLogLevel logLevel, string? message, Exception? exception)
    {
        var level = logLevel switch
        {
            VelopackLogLevel.Debug => LogEventLevel.Debug,
            VelopackLogLevel.Information => LogEventLevel.Information,
            VelopackLogLevel.Warning => LogEventLevel.Warning,
            VelopackLogLevel.Error => LogEventLevel.Error,
            VelopackLogLevel.Critical => LogEventLevel.Fatal,
            _ => LogEventLevel.Verbose
        };

        if (exception is not null)
        {
            _logger.Write(level, exception, "{message}", message);
        }
        else
        {
            _logger.Write(level, "{message}", message);
        }
    }
}