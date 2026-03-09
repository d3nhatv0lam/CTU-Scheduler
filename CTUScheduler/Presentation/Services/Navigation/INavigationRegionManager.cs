using System;
using CTUScheduler.Presentation.Services.Navigation.Models;
using ReactiveUI;

namespace CTUScheduler.Presentation.Services.Navigation;

public interface INavigationRegionManager
{
    IDisposable Register(RegionId regionId, IScreen screen);
    ICachingNavigationService Get(RegionId regionId);
    void ClearCache(RegionId regionId);
}