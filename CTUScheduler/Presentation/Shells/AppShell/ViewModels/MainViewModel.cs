using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using CTUScheduler.AppServices.Services.Network;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Authentication.ViewModels;
using CTUScheduler.Presentation.Services.Navigation;
using CTUScheduler.Presentation.Services.Navigation.Models;
using CTUScheduler.Presentation.Shared.Models.Regions;
using CTUScheduler.Presentation.Shells.MainShell.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Shells.AppShell.ViewModels;

public partial class MainViewModel : ViewModelBase , IScreen , IActivatableViewModel, IDisposable
{
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    private readonly IConnectivityService _connectivityService;
    private readonly INavigationRegionManager _navigationRegionManager;
    
    private readonly RegionId _regionId = RegionIds.Root;
    public RoutingState Router { get; } = new();
    public ViewModelActivator Activator { get; } = new();
    
    [Reactive(SetModifier = AccessModifier.Private)]
    private string _windowTitle = "CTU Scheduler";

    public MainViewModel(IConnectivityService connectivityService, INavigationRegionManager navigationRegionManager)
    {
        _connectivityService = connectivityService;
        _navigationRegionManager = navigationRegionManager;
        
        _disposables.Add(Activator);

        _navigationRegionManager.Register(_regionId, this)
            .DisposeWith(_disposables);

        _navigationRegionManager.NavigateAndResetTo<LoginViewModel>(_regionId);
        // _navigationRegionManager.NavigateAndResetTo<MainShellViewModel>(_regionId);
        
        // Router.Navigate.Execute(new LoginViewModel(this));
        // Router.Navigate.Execute(new MainShellViewModel(this));

        this.WhenActivated((CompositeDisposable disposables) =>
        {
            _connectivityService.IsInternetAvailable
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(isAvailable =>
                {
                    WindowTitle = isAvailable ? "CTU Scheduler" : "CTU Scheduler - No Internet";
                }).DisposeWith(disposables);
        });
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
