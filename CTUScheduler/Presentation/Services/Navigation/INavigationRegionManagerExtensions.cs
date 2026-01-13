using System;
using System.Threading.Tasks;
using CTUScheduler.Presentation.Services.Navigation.Models;
using ReactiveUI;

namespace CTUScheduler.Presentation.Services.Navigation;

public static class INavigationRegionManagerExtensions
{
    public static Task NavigateTo<T>(this INavigationRegionManager manager, RegionId regionId) where T : class, IRoutableViewModel
        => manager.Get(regionId).NavigateTo(typeof(T));

    public static Task NavigateAndResetTo<T>(this INavigationRegionManager manager, RegionId regionId) where T : class, IRoutableViewModel 
        => manager.Get(regionId).NavigateAndResetTo(typeof(T));

    public static Task NavigateAndResetTo(this INavigationRegionManager manager, RegionId regionId, Type vmType) 
        => manager.Get(regionId).NavigateAndResetTo(vmType);
}