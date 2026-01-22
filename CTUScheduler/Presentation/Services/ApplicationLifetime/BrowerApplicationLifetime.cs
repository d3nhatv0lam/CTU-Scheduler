using System.Threading;

namespace CTUScheduler.Presentation.Services.ApplicationLifetime;

public class BrowerApplicationLifetime: IApplicationLifetime
{
    public CancellationToken ApplicationStarted => CancellationToken.None;
    public CancellationToken ApplicationStopping => CancellationToken.None;
    public CancellationToken ApplicationStopped => CancellationToken.None;

    public void Shutdown() { }
}