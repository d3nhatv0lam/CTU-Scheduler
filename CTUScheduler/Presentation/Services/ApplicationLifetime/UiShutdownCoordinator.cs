using System;
using CTUScheduler.Presentation.Extensions;
using CTUScheduler.Presentation.Shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Presentation.Services.ApplicationLifetime;

public sealed class UiShutdownCoordinator : IUiShutdownCoordinator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UiShutdownCoordinator> _logger;

    public UiShutdownCoordinator(IServiceProvider serviceProvider, ILogger<UiShutdownCoordinator> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void DisposeUiServices()
    {
        _logger.UiInfo("Starting disposal of UI services...");

        var services = _serviceProvider.GetServices<IUiDisposable>();

        int count = 0;
        foreach (var service in services)
        {
            try
            {
                service.Dispose();
                count++;
            }
            catch (Exception ex)
            {
                _logger.UiError(ex, "Failed to dispose UI service {ServiceType}", service.GetType().Name);
            }
        }

        _logger.UiInfo("Disposed {Count} UI services successfully.", count);
    }
}