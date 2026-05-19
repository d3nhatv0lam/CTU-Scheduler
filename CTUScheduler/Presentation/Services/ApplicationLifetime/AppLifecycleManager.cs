using System;
using System.Threading;

namespace CTUScheduler.Presentation.Services.ApplicationLifetime;

public sealed class AppLifecycleManager : IAppLifecycleService, IAppLifecycleController, IDisposable
{
    public event Action? ShutdownRequested;
    private readonly CancellationTokenSource _startedCts = new();
    private readonly CancellationTokenSource _stoppingCts = new();
    private readonly CancellationTokenSource _stoppedCts = new();

    public CancellationToken ApplicationStarted => _startedCts.Token;
    public CancellationToken ApplicationStopping => _stoppingCts.Token;
    public CancellationToken ApplicationStopped => _stoppedCts.Token;

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


    public void Dispose()
    {
        _startedCts.Dispose();
        _stoppingCts.Dispose();
        _stoppedCts.Dispose();
    }
}