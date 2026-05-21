using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Helpers;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Authentication.ViewModels;
using CTUScheduler.Presentation.Services.Navigation;
using CTUScheduler.Presentation.Services.Navigation.Models;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using CTUScheduler.Presentation.Services.UserInteractionService.Models;
using CTUScheduler.Presentation.Services.ViewContext.Interfaces;
using CTUScheduler.Presentation.Shared.Interfaces;
using CTUScheduler.Presentation.Shared.Models.Identifiers;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Shells.AppShell.ViewModels;

public partial class MainViewModel : ViewModelBase, IScreen, IActivatableViewModel, IViewContext, ISingletonViewModel,
    IUiDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly ILogger<MainViewModel> _logger;

    private readonly NotificationOptions _internetNotificationOptions =
        new() { Expiration = TimeSpan.FromSeconds(10), ShowIcon = true };

    private bool _isDisposed;


    public IViewContextService ViewContext { get; }

    private readonly RegionId _regionId = RegionIds.Root;
    public RoutingState Router { get; } = new();
    public ViewModelActivator Activator { get; } = new();

    [Reactive(SetModifier = AccessModifier.Private)]
    private string _windowTitle = "CTU Scheduler";

    public ReactiveCommand<Unit, Unit> OpenGithubRepo { get; }


    public MainViewModel(
        IConnectivityService connectivityService,
        INavigationRegionManager navigationRegionManager,
        IViewContextService viewContextService,
        IUserInteractionService userInteractionService,
        ILogger<MainViewModel> logger)
    {
        ViewContext = viewContextService;
        _logger = logger;

        navigationRegionManager.Register(_regionId, this)
            .DisposeWith(_disposables);

        OpenGithubRepo = ReactiveCommand.Create(() => ProcessHelper.OpenUrl(AppConstants.Urls.GithubRepo))
            .DisposeWith(_disposables);

        navigationRegionManager.NavigateAndResetTo<LoginViewModel>(_regionId);
        // _navigationRegionManager.NavigateAndResetTo<MainShellViewModel>(_regionId);

        this.WhenActivated(disposables =>
        {
            connectivityService.IsInternetAvailable
                .DistinctUntilChanged()
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(isAvailable =>
                {
                    WindowTitle = isAvailable ? "CTU Scheduler" : "CTU Scheduler - No Internet";

                    if (!isAvailable)
                    {
                        userInteractionService.Notification.Light.Warning("Mât kết nối internet!",
                            in _internetNotificationOptions);
                    }
                    else
                    {
                        userInteractionService.Notification.Light.Success("Kết nối internet đã sẵn sàng!",
                            in _internetNotificationOptions);
                    }
                }).DisposeWith(disposables);
        });
    }

    ~MainViewModel() => Dispose(false);


    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed) return;

        if (disposing)
        {
            _disposables.Dispose();
            _logger.LogDebug("Disposed");
        }

        _isDisposed = true;
    }
}