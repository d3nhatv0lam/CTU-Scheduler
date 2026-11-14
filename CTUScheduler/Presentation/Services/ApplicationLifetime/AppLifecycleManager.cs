using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Presentation.Services.ApplicationLifetime;

public sealed class AppLifecycleManager : IAppLifecycleService, IAppLifecycleController, IDisposable
{
    private readonly ILogger<AppLifecycleManager> _logger;
    public event Action? ShutdownRequested;
    private readonly CancellationTokenSource _startedCts = new();
    private readonly CancellationTokenSource _stoppingCts = new();
    private readonly CancellationTokenSource _stoppedCts = new();

    public CancellationToken ApplicationStarted => _startedCts.Token;
    public CancellationToken ApplicationStopping => _stoppingCts.Token;
    public CancellationToken ApplicationStopped => _stoppedCts.Token;

    public AppLifecycleManager(ILogger<AppLifecycleManager> logger)
    {
        _logger = logger;
    }

    public void NotifyStarted()
    {
        if (!_startedCts.IsCancellationRequested)
            _startedCts.Cancel();
    }

    public void NotifyStopping()
    {
        if (!_stoppingCts.IsCancellationRequested)
            _stoppingCts.Cancel();
    }

    public void NotifyStopped()
    {
        if (!_stoppedCts.IsCancellationRequested)
            _stoppedCts.Cancel();
    }

    public void Shutdown() => ShutdownRequested?.Invoke();

    public void Restart()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
            {
                exePath = Environment.GetCommandLineArgs().FirstOrDefault();
            }

            if (!string.IsNullOrEmpty(exePath))
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true,
                    WorkingDirectory = AppContext.BaseDirectory
                };

                Process.Start(startInfo);
            }
            else
            {
                _logger.LogWarning("Không thể xác định đường dẫn thực thi để khởi động lại ứng dụng.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra khi cố gắng khởi động lại ứng dụng.");
        }
        finally
        {
            Shutdown();
        }
    }

    public void Dispose()
    {
        _startedCts.Dispose();
        _stoppingCts.Dispose();
        _stoppedCts.Dispose();
    }
}