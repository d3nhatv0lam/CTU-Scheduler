using System;
using System.Threading;

namespace CTUScheduler.Presentation.Services.ApplicationLifetime;

public sealed class AppLifetimeManager : IApplicationLifetime, IDisposable
{
    private readonly CancellationTokenSource _startedCts = new();
    private readonly CancellationTokenSource _stoppingCts = new();
    private readonly CancellationTokenSource _stoppedCts = new();

    public CancellationToken ApplicationStarted => _startedCts.Token;
    public CancellationToken ApplicationStopping => _stoppingCts.Token;
    public CancellationToken ApplicationStopped => _stoppedCts.Token;

    public event Action? ShutdownRequested;

    public void NotifyStarted() => _startedCts.Cancel();
    public void NotifyStopping() => _stoppingCts.Cancel();
    public void NotifyStopped() => _stoppedCts.Cancel();

    public void Shutdown()
    {
        ShutdownRequested?.Invoke();
    }

    public void Dispose()
    {
        _startedCts.Dispose();
        _stoppingCts.Dispose();
        _stoppedCts.Dispose();
    }
}
