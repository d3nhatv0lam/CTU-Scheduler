using System.Threading;

namespace CTUScheduler.Presentation.Services.ApplicationLifetime;

public interface IApplicationLifetime
{
    public CancellationToken ApplicationStarted { get; }
    public CancellationToken ApplicationStopping { get; }
    public CancellationToken ApplicationStopped { get; }
    public void Shutdown();
}