using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CTUScheduler.Presentation.Features.CrashReport.ViewModels;
using CTUScheduler.Presentation.Features.CrashReport.Views;
using CTUScheduler.Presentation.Services.ApplicationLifetime;
using CTUScheduler.Presentation.Services.ViewContext.Interfaces;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Presentation.Services.CrashReport;

public class CrashReportService : ICrashReportService
{
    private readonly ILogger<CrashReportService> _logger;
    private readonly IViewContextService _viewContextService;
    private readonly IAppLifecycleService _appLifecycleService;
    private readonly HashSet<string> _mutedSignatures = [];
    private readonly Lock _lock = new();
    private bool _isWindowOpen;

    public CrashReportService(
        ILogger<CrashReportService> logger,
        IViewContextService viewContextService,
        IAppLifecycleService appLifecycleService)
    {
        _logger = logger;
        _viewContextService = viewContextService;
        _appLifecycleService = appLifecycleService;
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

        var waitHandle = new ManualResetEventSlim(false);

        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                var vm = new CrashReportViewModel(exception, isFatal, _appLifecycleService);
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

                    waitHandle.Set();

                    if (isFatal)
                    {
                        _appLifecycleService.Shutdown();
                    }
                };

                Window? owner = _viewContextService.CurrentTopLevel as Window;
                if (owner == null && Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    owner = desktop.Windows.FirstOrDefault(w => w.IsVisible && w is not CrashReportWindow)
                           ?? desktop.MainWindow;
                }

                if (owner is { IsVisible: true })
                {
                    await window.ShowDialog(owner);
                }
                else
                {
                    window.Show();
                    window.Activate();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to display CrashReportWindow.");
                lock (_lock)
                {
                    _isWindowOpen = false;
                }
                waitHandle.Set();
            }
        });

        if (isFatal && !Dispatcher.UIThread.CheckAccess())
        {
            // Chặn thread nền để CLR không huỷ process ngay lập tức trước khi UI kịp hiển thị cửa sổ báo lỗi
            waitHandle.Wait();
        }
    }

    private static string GetExceptionSignature(Exception ex)
    {
        return $"{ex.GetType().FullName}:{ex.TargetSite?.Name}:{ex.Message?.GetHashCode()}";
    }
}
