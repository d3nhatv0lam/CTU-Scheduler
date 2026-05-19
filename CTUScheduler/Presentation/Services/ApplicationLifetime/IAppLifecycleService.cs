using System;
using System.Threading;

namespace CTUScheduler.Presentation.Services.ApplicationLifetime;

public interface IAppLifecycleService
{
    public CancellationToken ApplicationStarted { get; }
    public CancellationToken ApplicationStopping { get; }
    public CancellationToken ApplicationStopped { get; }
    public void Shutdown();
}