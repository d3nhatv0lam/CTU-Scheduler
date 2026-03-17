using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Authentication.ViewModels;
using CTUScheduler.Presentation.Services.Navigation;
using CTUScheduler.Presentation.Services.Navigation.Models;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using CTUScheduler.Presentation.Services.UserInteractionService.Models;
using CTUScheduler.Presentation.Services.ViewContext.Interfaces;
using CTUScheduler.Presentation.Shared.Models.Regions;
using CTUScheduler.Presentation.Shells.MainShell.ViewModels;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Shells.AppShell.ViewModels;

public partial class MainViewModel : ViewModelBase, IScreen, IActivatableViewModel, IDisposable, IViewContext
{
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    private readonly IConnectivityService _connectivityService;
    private readonly INavigationRegionManager _navigationRegionManager;
    private readonly IUserInteractionService _userInteractionService;
    
    private readonly NotificationOptions _internetNotificationOptions = new() { Expiration = TimeSpan.FromSeconds(10), ShowIcon = true};

    private readonly RegionId _regionId = RegionIds.Root;
    public RoutingState Router { get; } = new();
    public ViewModelActivator Activator { get; } = new();
    public IViewContextService ViewContext { get; }
    
    [Reactive(SetModifier = AccessModifier.Private)]
    private string _windowTitle = "CTU Scheduler";


    public MainViewModel(
        IConnectivityService connectivityService,
        INavigationRegionManager navigationRegionManager,
        IViewContextService viewContextService,
        IUserInteractionService userInteractionService)
    {
        _connectivityService = connectivityService;
        _navigationRegionManager = navigationRegionManager;
        _userInteractionService = userInteractionService;
        ViewContext = viewContextService;

        _navigationRegionManager.Register(_regionId, this)
            .DisposeWith(_disposables);

        _navigationRegionManager.NavigateAndResetTo<LoginViewModel>(_regionId);
        // _navigationRegionManager.NavigateAndResetTo<MainShellViewModel>(_regionId);
        
        this.WhenActivated((CompositeDisposable disposables) =>
        {
            _connectivityService.IsInternetAvailable
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(isAvailable =>
                {
                    WindowTitle = isAvailable ? "CTU Scheduler" : "CTU Scheduler - No Internet";

                    if (!isAvailable)
                    {
                        _userInteractionService.Notification.Light.Warning("Mât kết nối internet!", this._internetNotificationOptions);
                    }
                    else
                    {
                        _userInteractionService.Notification.Light.Success("Kết nối internet đã sẵn sàng!", this._internetNotificationOptions);
                    }

                }).DisposeWith(disposables);
        });
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }


}