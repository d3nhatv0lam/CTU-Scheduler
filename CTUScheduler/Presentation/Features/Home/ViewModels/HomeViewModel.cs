using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Services.UserSessionService;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Services.Navigation;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using ReactiveUI;
using Serilog;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using Microsoft.Extensions.Logging;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Features.Home.ViewModels;

public partial class HomeViewModel : WebSyncViewModelBase, IRoutableViewModel, IDisposable
{
    private readonly CompositeDisposable _disposable = new();
    private readonly IRegistrationRulesService _registrationRulesService;
    private readonly ICourseRegistrationService _courseRegistrationService;
    private readonly ILogger<HomeViewModel> _logger;

    private readonly ObservableAsPropertyHelper<RegistrationInformation?> _registrationInfo;
    [ObservableAsProperty] private IReadOnlyList<PlannedCourse>? _plannedCourses;
    [ObservableAsProperty] private bool _isInitialLoading;

    public string UrlPathSegment => nameof(HomeViewModel);
    public IScreen HostScreen { get; }
    public RegistrationInformation? RegistrationInfo => _registrationInfo.Value;

    public ReactiveCommand<Unit, OperationResult<IReadOnlyList<PlannedCourse>>> LoadPlannedCoursesCommand { get; }

    public HomeViewModel(IScreen hostScreen,
        IUserSessionService userSessionService,
        IRegistrationRulesService registrationRulesService,
        ICourseRegistrationService courseRegistrationService,
        IUserInteractionService userInteractionService,
        INavigationRegionManager navigationRegionManager,
        IConnectivityService connectivityService,
        ILogger<HomeViewModel> logger) : base(userInteractionService, navigationRegionManager, connectivityService)
    {
        HostScreen = hostScreen;
        _registrationRulesService = registrationRulesService;
        _courseRegistrationService = courseRegistrationService;
        _logger = logger;

        registrationRulesService.RegistrationInfoChanged
            .Subscribe(userSessionService.UpdateServerInfo)
            .DisposeWith(_disposable);

        _registrationInfo = userSessionService.RegistrationInfoChanged
            .ToProperty(this, nameof(RegistrationInfo), scheduler: RxApp.MainThreadScheduler)
            .DisposeWith(_disposable);

        _isInitialLoadingHelper = this.WhenAnyValue(x => x.IsLoading, x => x.RegistrationInfo,
                (isLoading, data) => isLoading && data is null)
            .ToProperty(this, nameof(IsInitialLoading), deferSubscription: true)
            .DisposeWith(_disposable);

        LoadPlannedCoursesCommand = ReactiveCommand
            .CreateFromTask(ct => _courseRegistrationService.FetchPlannedCourseAsync(token: ct))
            .DisposeWith(_disposable);

        _plannedCoursesHelper = LoadPlannedCoursesCommand
            .Where(x => x.IsSuccess)
            .Select(x => x.Content)
            .ToProperty(this, nameof(PlannedCourses))
            .DisposeWith(_disposable);

        SyncWebSessionCommand.Where(x => x.IsSuccess)
            .Select(_ => Unit.Default)
            .InvokeCommand(LoadPlannedCoursesCommand)
            .DisposeWith(_disposable);
    }

    protected override async Task<OperationResult> ExecuteWebSyncTaskAsync()
    {
        return await _registrationRulesService.EnsureReadyAsync();
    }

    protected override void OnWebSyncSuccess()
    {
    }

    public void Dispose()
    {
        _disposable.Dispose();
        _logger.LogDebug("{this}: Disposed", nameof(HomeViewModel));
    }
}