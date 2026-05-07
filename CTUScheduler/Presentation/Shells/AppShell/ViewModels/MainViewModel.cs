using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Helpers;
using CTUScheduler.AppServices.State;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Authentication.ViewModels;
using CTUScheduler.Presentation.Services.Navigation;
using CTUScheduler.Presentation.Services.Navigation.Models;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using CTUScheduler.Presentation.Services.UserInteractionService.Models;
using CTUScheduler.Presentation.Services.ViewContext.Interfaces;
using CTUScheduler.Presentation.Shared.Models.Identifiers;
using CTUScheduler.Presentation.Shells.MainShell.ViewModels;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Shells.AppShell.ViewModels;

public partial class MainViewModel : ViewModelBase, IScreen, IActivatableViewModel, IDisposable, IViewContext
{
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    private readonly IConnectivityService _connectivityService;
    private readonly INavigationRegionManager _navigationRegionManager;
    private readonly IUserInteractionService _userInteractionService;
    private readonly ITeachingPlanLoaderService _teachingPlanLoaderService;
    private readonly ILogger<MainViewModel> _logger;
    private readonly AppState _appState;

    private readonly NotificationOptions _internetNotificationOptions = new() { Expiration = TimeSpan.FromSeconds(10), ShowIcon = true};

    private readonly RegionId _regionId = RegionIds.Root;
    public RoutingState Router { get; } = new();
    public ViewModelActivator Activator { get; } = new();
    public IViewContextService ViewContext { get; }
    
    [Reactive(SetModifier = AccessModifier.Private)]
    private string _windowTitle = "CTU Scheduler";
    
    public ReactiveCommand<Unit, Unit> OpenGithubRepo { get; }


    public MainViewModel(
        IConnectivityService connectivityService,
        INavigationRegionManager navigationRegionManager,
        IViewContextService viewContextService,
        IUserInteractionService userInteractionService,
        ITeachingPlanLoaderService teachingPlanLoaderService,
        ILogger<MainViewModel> logger,
        AppState appState)
    {
        _connectivityService = connectivityService;
        _navigationRegionManager = navigationRegionManager;
        _userInteractionService = userInteractionService;
        _teachingPlanLoaderService = teachingPlanLoaderService;
        _logger = logger;
        _appState = appState;
        ViewContext = viewContextService;

        _navigationRegionManager.Register(_regionId, this)
            .DisposeWith(_disposables);
        
        OpenGithubRepo = ReactiveCommand.Create(() => ProcessHelper.OpenUrl(AppConstants.Urls.GithubRepo))
            .DisposeWith(_disposables);

        _navigationRegionManager.NavigateAndResetTo<LoginViewModel>(_regionId);
        // _navigationRegionManager.NavigateAndResetTo<MainShellViewModel>(_regionId);
        
        this.WhenActivated((CompositeDisposable disposables) =>
        {
            Observable.StartAsync(async _ => await _teachingPlanLoaderService.LoadLatestAsync())
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(result =>
                {
                    if (result.IsFailed)
                    {
                        var message = result.FirstErrorMessage ?? "Không tải được kế hoạch giảng dạy";
                        _userInteractionService.Notification.Light.Warning(message);
                        return;
                    }

                    var data = result.Content;
                    _appState.SetTeachingPlan(data);
                    _logger.LogInformation(
                        "TeachingPlanData: Title={Title}; Semester={Semester}; SchoolYear={SchoolYear}; TimelineCount={Count}",
                        data.Title,
                        data.Semester,
                        data.SchoolYear,
                        data.RegistrationTimeline.Count);

                    for (var i = 0; i < data.RegistrationTimeline.Count; i++)
                    {
                        var item = data.RegistrationTimeline[i];
                        _logger.LogInformation(
                            "TeachingPlanTimeline[{Index}]: {Description} (Start={Start}, End={End})",
                            i + 1,
                            item.Description,
                            item.StartDate,
                            item.EndDate);
                    }
                })
                .DisposeWith(disposables);

            _connectivityService.IsInternetAvailable
                .DistinctUntilChanged()
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(isAvailable =>
                {
                    WindowTitle = isAvailable ? "CTU Scheduler" : "CTU Scheduler - No Internet";

                    if (!isAvailable)
                    {
                        _userInteractionService.Notification.Light.Warning("Mât kết nối internet!", in this._internetNotificationOptions);
                    }
                    else
                    {
                        _userInteractionService.Notification.Light.Success("Kết nối internet đã sẵn sàng!", in this._internetNotificationOptions);
                    }

                }).DisposeWith(disposables);

            Disposable.Create(this.Dispose)
                .DisposeWith(disposables);
        });
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }


}