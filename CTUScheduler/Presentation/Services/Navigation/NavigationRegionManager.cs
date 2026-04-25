using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Disposables;
using CTUScheduler.Presentation.Services.Factories;
using CTUScheduler.Presentation.Services.Navigation.Models;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace CTUScheduler.Presentation.Services.Navigation;

public class NavigationRegionManager : INavigationRegionManager
{
    private readonly ConcurrentDictionary<RegionId, ICachingNavigationService> _regions = new();
    private readonly IViewModelFactory _vmFactory;
    private readonly ILogger<NavigationRegionManager> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public NavigationRegionManager(IViewModelFactory vmFactory, ILoggerFactory loggerFactory)
    {
        _vmFactory = vmFactory;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<NavigationRegionManager>();
    }

    public IDisposable Register(RegionId regionId, IScreen screen)
    {
        _logger.LogDebug("Registering region: {RegionId}", regionId.Value);
        if (_regions.ContainsKey(regionId))
        {
            throw new InvalidOperationException($"Region '{regionId.Value}' is already registered.");
        }

        var navigationService = new CachingNavigationService(
            screen,
            _vmFactory,
            _loggerFactory.CreateLogger<CachingNavigationService>());

        _regions[regionId] = navigationService;

        return Disposable.Create(regionId, id =>
        {
            if (_regions.TryRemove(id, out var s))
            {
                _logger.LogDebug("Unregistering region: {RegionId}", id.Value);
                if (s is IDisposable disposable) disposable.Dispose();
            }
        });
    }

    public ICachingNavigationService Get(RegionId regionId)
    {
        if (!_regions.TryGetValue(regionId, out var service))
        {
            _logger.LogError("Region not found: {RegionId}", regionId.Value);
            throw new KeyNotFoundException($"Region {regionId.Value} not found.");
        }

        return service;
    }
    
    public void ClearCache(RegionId regionId)
    {
        if (_regions.TryGetValue(regionId, out var service))
        {
            service.ClearAllCache();
        }
    }
}