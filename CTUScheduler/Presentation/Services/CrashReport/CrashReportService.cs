using System;
using System.Collections.Generic;
using Avalonia.Threading;
using CTUScheduler.Presentation.Features.CrashReport.ViewModels;
using CTUScheduler.Presentation.Features.CrashReport.Views;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Presentation.Services.CrashReport;

public class CrashReportService : ICrashReportService
{
    private readonly ILogger<CrashReportService> _logger;
    private readonly HashSet<string> _mutedSignatures = new();
    private readonly object _lock = new();
    private bool _isWindowOpen;

    public CrashReportService(ILogger<CrashReportService> logger)
    {
        _logger = logger;
    }

    public bool IsMuted(Exception exception)
    {
        lock (_lock)
        {
            var signature = GetExceptionSignature(exception);
            return _mutedSignatures.Contains(signature);
        }
    }

    public void Mute(Exception exception)
    {
        lock (_lock)
        {
            var signature = GetExceptionSignature(exception);
            _mutedSignatures.Add(signature);
        }
    }

    public void ShowCrashReport(Exception exception, bool isFatal = false)
    {
        lock (_lock)
        {
            if (!isFatal && IsMuted(exception))
            {
                _logger.LogDebug("Exception muted by user: {Type}", exception.GetType().Name);
                return;
            }

            if (_isWindowOpen)
            {
                _logger.LogDebug("Crash window is already open. Skipping duplicate window.");
                return;
            }

            _isWindowOpen = true;
        }

        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                var vm = new CrashReportViewModel(exception, isFatal);
                var window = new CrashReportWindow { DataContext = vm };

                window.Closed += (_, _) =>
                {
                    lock (_lock)
                    {
                        if (vm.MuteThisError && !isFatal)
                        {
                            Mute(exception);
                        }
                        _isWindowOpen = false;
                    }

                    if (isFatal)
                    {
                        Environment.Exit(1);
                    }
                };

                window.Show();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to display CrashReportWindow.");
                lock (_lock)
                {
                    _isWindowOpen = false;
                }
            }
        });
    }

    private static string GetExceptionSignature(Exception ex)
    {
        return $"{ex.GetType().FullName}:{ex.TargetSite?.Name}:{ex.Message?.GetHashCode()}";
    }
}
