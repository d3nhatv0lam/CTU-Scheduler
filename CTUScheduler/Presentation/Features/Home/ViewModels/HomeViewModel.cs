using System;
using System.Collections.Generic;
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

public partial class HomeViewModel : WebSyncViewModelBase, IRoutableViewModel, IDisposable
{
    private readonly CompositeDisposable _disposable = new();
    private readonly IRegistrationRulesService _registrationRulesService;
    private readonly ICourseRegistrationService _courseRegistrationService;
    private readonly ILogger<HomeViewModel> _logger;

    private readonly ObservableAsPropertyHelper<RegistrationInformation?> _registrationInfo;
    [ObservableAsProperty] private IReadOnlyList<PlannedCourse>? _plannedCourses;
    /// <summary>
    /// Phải có Init được thì mới có token để get các thông tin khác
    /// </summary>
    [ObservableAsProperty] private bool _isInitialLoading;
    [ObservableAsProperty] private bool _isLoadingPlannedCourses;

    public string UrlPathSegment => nameof(HomeViewModel);
    public IScreen HostScreen { get; }
    public RegistrationInformation? RegistrationInfo => _registrationInfo.Value;
    
    public TimelineViewModel TimelineViewModel { get; } = new();

    public ReactiveCommand<Unit, OperationResult<IReadOnlyList<PlannedCourse>>> LoadPlannedCoursesCommand { get; }

    public HomeViewModel(IScreen hostScreen,
        IUserSessionService userSessionService,
        IRegistrationRulesService registrationRulesService,
        ICourseRegistrationService courseRegistrationService,
        IPlannedCourseStore plannedCourseStore,
        IUserInteractionService userInteractionService,
        INavigationRegionManager navigationRegionManager,
        ITeachingPlanLoaderService teachingPlanLoaderService,
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
        
      

        _isInitialLoading = true;
        _isLoadingPlannedCourses = true;

        Observable.StartAsync(async _ => await registrationRulesService.EnsureReadyAsync())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(
                result =>
                {
                    _isInitialLoading = false;

                    result.Match(
                        () =>
                        {
                            _isLoadingPlannedCourses = true;

                            Observable.StartAsync(async _ => await teachingPlanLoaderService.LoadLatestAsync())
                                .ObserveOn(RxApp.MainThreadScheduler)
                                .Subscribe(loadResult =>
                                    {
                                        _isLoadingPlannedCourses = false;

                                        if (loadResult.IsFailed)
                                        {
                                            return;
                                        }
                                        
                                        foreach (var node in TimelineViewModel.Nodes.ToList())
                                        {
                                            node.Dispose();
                                        }
                                        TimelineViewModel.Nodes.Clear();
                                        foreach (var item in loadResult.Content.RegistrationTimeline)
                                        {
                                            TimelineViewModel.Nodes.Add(new TimelineNodeViewModel(item));
                                        }
                                    },
                                    ex =>
                                    {
                                        _isLoadingPlannedCourses = false;
                                    })
                                .DisposeWith(_disposable);
                        },
                        (errors, _) =>
                        {
                            _isLoadingPlannedCourses = false;
                        }
                    );
                },
                ex =>
                {
                    _isInitialLoading = false;
                    _isLoadingPlannedCourses = false;
                }
            );
        
        _registrationInfo = userSessionService.RegistrationInfoChanged
            .ToProperty(this, nameof(RegistrationInfo), scheduler: RxSchedulers.MainThreadScheduler)
            .DisposeWith(_disposable);

        _isInitialLoadingHelper = this.WhenAnyValue(x => x.IsLoading, x => x.RegistrationInfo,
                (isLoading, data) => isLoading && data is null)
            .ToProperty(this, nameof(IsInitialLoading), deferSubscription: true)
            .DisposeWith(_disposable);

        LoadPlannedCoursesCommand = ReactiveCommand
            .CreateFromTask(ct => _courseRegistrationService.FetchPlannedCourseAsync(token: ct))
            .DisposeWith(_disposable);
        
       LoadPlannedCoursesCommand
            .Where(x => x.IsSuccess)
            .Select(x => x.Content!)
            .Subscribe(plannedCourseStore.Update)
            .DisposeWith(_disposable);
       
        _plannedCoursesHelper = plannedCourseStore.PlannedCoursesChanged
            .ToProperty(this, nameof(PlannedCourses), scheduler: RxSchedulers.MainThreadScheduler)
            .DisposeWith(_disposable);

        SyncWebSessionCommand.Where(x => x.IsSuccess)
            .Select(_ => Unit.Default)
            .InvokeCommand(LoadPlannedCoursesCommand)
            .DisposeWith(_disposable);

        // hoạt động tốt khi cached lại! hiện tại loading mỗi lần mở view
        _isLoadingPlannedCoursesHelper = this.WhenAnyValue(x => x.IsInitialLoading, x => x.PlannedCourses)
            .CombineLatest(LoadPlannedCoursesCommand.IsExecuting,
                (state, isExecuting) =>
                {
                    var (isInitial, plannedCourses) = state;
                    return isInitial || (isExecuting && plannedCourses is null);
                })
            .ToProperty(this, nameof(_isLoadingPlannedCourses))
            .DisposeWith(_disposable);
    }

    protected override async Task<OperationResult> ExecuteWebSyncTaskAsync()
    {
        return await _registrationRulesService.EnsureReadyAsync();
    }

    public void Dispose()
    {
        _disposable.Dispose();
        TimelineViewModel.Dispose();
        _logger.LogDebug("{this}: Disposed", nameof(HomeViewModel));
    }
}