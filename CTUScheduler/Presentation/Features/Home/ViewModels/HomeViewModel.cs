using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Presentation.Shared.Controls.Timeline;
using Microsoft.Extensions.Logging;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Features.Home.ViewModels;

public partial class HomeViewModel : WebSyncViewModelBase, IRoutableViewModel
{
    private readonly IRegistrationRulesService _registrationRulesService;
    private readonly ICourseRegistrationService _courseRegistrationService;
    private readonly ITuitionFeeService _tuitionFeeService;
    private readonly ILogger<HomeViewModel> _logger;

    private readonly ObservableAsPropertyHelper<RegistrationInformation?> _registrationInfo;
    [ObservableAsProperty] private IReadOnlyList<PlannedCourse>? _plannedCourses;
    [ObservableAsProperty] private TuitionFeeSummary? _tuitionFee;

    [ObservableAsProperty] private bool _isInitialLoading;
    [ObservableAsProperty] private bool _isLoadingPlannedCourses;
    [ObservableAsProperty] private bool _isLoadingTuitionFee;

    public string UrlPathSegment => nameof(HomeViewModel);
    public IScreen HostScreen { get; }
    public RegistrationInformation? RegistrationInfo => _registrationInfo.Value;

    public TimelineViewModel TimelineViewModel { get; } = new();

    public ReactiveCommand<Unit, OperationResult<IReadOnlyList<PlannedCourse>>> LoadPlannedCoursesCommand { get; }
    public ReactiveCommand<Unit, OperationResult<TuitionFeeSummary>> LoadTuitionFeeCommand { get; }

    public HomeViewModel(IScreen hostScreen,
        IUserSessionService userSessionService,
        IRegistrationRulesService registrationRulesService,
        ICourseRegistrationService courseRegistrationService,
        IPlannedCourseStore plannedCourseStore,
        ITuitionFeeService tuitionFeeService,
        ITuitionFeeStore tuitionFeeStore,
        IUserInteractionService userInteractionService,
        INavigationRegionManager navigationRegionManager,
        IConnectivityService connectivityService,
        ILogger<HomeViewModel> logger) : base(userInteractionService, navigationRegionManager, connectivityService)
    {
        HostScreen = hostScreen;
        _registrationRulesService = registrationRulesService;
        _courseRegistrationService = courseRegistrationService;
        _tuitionFeeService = tuitionFeeService;
        _logger = logger;

        registrationRulesService.RegistrationInfoChanged
            .Subscribe(userSessionService.UpdateServerInfo)
            .DisposeWith(Disposables);

        _registrationInfo = userSessionService.RegistrationInfoChanged
            .ToProperty(this, nameof(RegistrationInfo), scheduler: RxSchedulers.MainThreadScheduler)
            .DisposeWith(Disposables);

        _isInitialLoadingHelper = this.WhenAnyValue(x => x.IsLoading, x => x.RegistrationInfo,
                (isLoading, data) => isLoading && data is null)
            .ToProperty(this, nameof(IsInitialLoading), deferSubscription: true)
            .DisposeWith(Disposables);

        LoadPlannedCoursesCommand = ReactiveCommand
            .CreateFromTask(ct => _courseRegistrationService.FetchPlannedCourseAsync(token: ct))
            .DisposeWith(Disposables);

        LoadPlannedCoursesCommand
            .Where(x => x.IsSuccess)
            .Select(x => x.Content!)
            .Subscribe(plannedCourseStore.Update)
            .DisposeWith(Disposables);


        _plannedCoursesHelper = plannedCourseStore.PlannedCoursesChanged
            .ToProperty(this, nameof(PlannedCourses), scheduler: RxSchedulers.MainThreadScheduler)
            .DisposeWith(Disposables);

        LoadTuitionFeeCommand = ReactiveCommand.CreateFromTask(ct => _tuitionFeeService.FetchTuitionFeeAsync(ct))
            .DisposeWith(Disposables);

        LoadTuitionFeeCommand
            .Where(x => x.IsSuccess)
            .Select(x => x.Content!)
            .Subscribe(tuitionFeeStore.Update)
            .DisposeWith(Disposables);

        _tuitionFeeHelper = tuitionFeeStore.TuitionFeeSummaryChanged
            .ToProperty(this, nameof(TuitionFee), scheduler: RxSchedulers.MainThreadScheduler)
            .DisposeWith(Disposables);

        SyncWebSessionCommand.Where(x => x.IsSuccess)
            .Select(_ => Unit.Default)
            .InvokeCommand(LoadPlannedCoursesCommand)
            .DisposeWith(Disposables);

        SyncWebSessionCommand.Where(x => x.IsSuccess)
            .Select(_ => Unit.Default)
            .InvokeCommand(LoadTuitionFeeCommand)
            .DisposeWith(Disposables);

        _isLoadingPlannedCoursesHelper = this.WhenAnyValue(x => x.IsLoading, x => x.PlannedCourses)
            .CombineLatest(LoadPlannedCoursesCommand.IsExecuting,
                (state, isExecuting) =>
                {
                    var (isLoading, plannedCourses) = state;
                    return plannedCourses is null && (isLoading || isExecuting);
                })
            .ToProperty(this, nameof(IsLoadingPlannedCourses))
            .DisposeWith(Disposables);

        _isLoadingTuitionFeeHelper = this.WhenAnyValue(x => x.IsLoading, x => x.TuitionFee)
            .CombineLatest(LoadTuitionFeeCommand.IsExecuting,
                (state, isExecuting) =>
                {
                    var (isLoading, tuitionFee) = state;
                    return tuitionFee is null && (isLoading || isExecuting);
                }).ToProperty(this, nameof(IsLoadingTuitionFee))
            .DisposeWith(Disposables);
    }

    protected override async Task<OperationResult> ExecuteWebSyncTaskAsync()
    {
        return await _registrationRulesService.EnsureReadyAsync();
    }

    protected override void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            TimelineViewModel.Dispose();
            _logger.LogDebug("{this}: Disposed", nameof(HomeViewModel));
        }

        base.Dispose(isDisposing);
    }
}