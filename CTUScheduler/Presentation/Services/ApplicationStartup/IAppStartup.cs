using Avalonia.Controls.ApplicationLifetimes;

namespace CTUScheduler.Presentation.Services.ApplicationStartup;

public interface IAppStartup
{
    void Initialize(IApplicationLifetime lifetime);
}
