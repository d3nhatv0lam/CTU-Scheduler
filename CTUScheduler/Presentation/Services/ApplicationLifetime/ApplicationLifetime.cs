using System.Threading;
using Microsoft.Extensions.Hosting;

namespace CTUScheduler.Presentation.Services.ApplicationLifetime;

public sealed class DesktopApplicationLifetime: IApplicationLifetime
{
    private readonly IHostApplicationLifetime _hostLifetime;

    public CancellationToken ApplicationStarted => _hostLifetime.ApplicationStarted;
    public CancellationToken ApplicationStopping => _hostLifetime.ApplicationStopping;
    public CancellationToken ApplicationStopped => _hostLifetime.ApplicationStopped;
    
    public DesktopApplicationLifetime(IHostApplicationLifetime hostApplicationLifetime)
    {
        _hostLifetime = hostApplicationLifetime;
    }
    
    public void Shutdown()
    {
        _hostLifetime.StopApplication();
    }
}