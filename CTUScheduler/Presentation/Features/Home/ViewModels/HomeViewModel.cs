using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
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
using CTUScheduler.Core.Models.TeachingPlan;
using Microsoft.Extensions.Logging;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Features.Home.ViewModels;

public partial class HomeViewModel : SessionSyncViewModelBase, IRoutableViewModel
{
    [GeneratedRegex(@"\d+")]
    private static partial Regex CohortNumberRegex();

    private readonly IRegistrationRulesService _registrationRulesService;

    private readonly ObservableAsPropertyHelper<RegistrationInformation?> _registrationInfo;
    [ObservableAsProperty] private IReadOnlyList<PlannedCourse>? _plannedCourses;
    [ObservableAsProperty] private TuitionFeeSummary? _tuitionFee;
    [ObservableAsProperty] private TeachingPlanData? _teachingPlan;

    [ObservableAsProperty] private bool _isLoadingRegistrationInfo;
    [ObservableAsProperty] private bool _isLoadingPlannedCourses;
    [ObservableAsProperty] private bool _isLoadingTuitionFee;

    public string UrlPathSegment => nameof(HomeViewModel);
    public IScreen HostScreen { get; }
    public RegistrationInformation? RegistrationInfo => _registrationInfo.Value;
    public TimelineViewModel TimelineViewModel { get; } = new();
    
    public ReactiveCommand<Unit, OperationResult> LoadPlannedCoursesCommand { get; }
    public ReactiveCommand<Unit, OperationResult> LoadTuitionFeeCommand { get; }

    public HomeViewModel(IScreen hostScreen,
        IUserSessionService userSessionService,
        IRegistrationRulesService registrationRulesService,
        ICourseRegistrationService courseRegistrationService,
        ITuitionFeeService tuitionFeeService,
        IPlannedCourseStore plannedCourseStore,
        ITuitionFeeStore tuitionFeeStore,
        ITeachingPlanStore teachingPlanStore,
        IUserInteractionService userInteractionService,
        INavigationRegionManager navigationRegionManager,
        IConnectivityService connectivityService,
        ILogger<HomeViewModel> logger) : base(userInteractionService, navigationRegionManager, connectivityService,
        logger)
    {
        HostScreen = hostScreen;
        _registrationRulesService = registrationRulesService;

        _registrationInfo = userSessionService.RegistrationInfoChanged
            .ToProperty(this, nameof(RegistrationInfo), scheduler: RxSchedulers.MainThreadScheduler)
            .DisposeWith(Disposables);

        _isLoadingRegistrationInfoHelper = this.WhenAnyValue(x => x.IsLoading, x => x.RegistrationInfo,
                (isLoading, data) => isLoading && data is null)
            .ToProperty(this, nameof(IsLoadingRegistrationInfo), deferSubscription: true)
            .DisposeWith(Disposables);

        LoadPlannedCoursesCommand = ReactiveCommand
            .CreateFromTask(courseRegistrationService.RefreshPlannedCourseAsync)
            .DisposeWith(Disposables);

        _plannedCoursesHelper = plannedCourseStore.Changed
            .ToProperty(this, nameof(PlannedCourses), scheduler: RxSchedulers.MainThreadScheduler)
            .DisposeWith(Disposables);

        LoadTuitionFeeCommand = ReactiveCommand
            .CreateFromTask(ct => tuitionFeeService.RefreshTuitionFeeAsync(cancellationToken: ct))
            .DisposeWith(Disposables);

        _tuitionFeeHelper = tuitionFeeStore.TuitionFeeSummaryChanged
            .ToProperty(this, nameof(TuitionFee), scheduler: RxSchedulers.MainThreadScheduler)
            .DisposeWith(Disposables);

        _teachingPlanHelper = teachingPlanStore.Changed
            .ToProperty(this, nameof(TeachingPlan), scheduler: RxSchedulers.MainThreadScheduler)
            .DisposeWith(Disposables);

        this.WhenAnyValue(x => x.TeachingPlan)
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(ApplyTeachingPlanToTimeline)
            .DisposeWith(Disposables);

        userSessionService.RegistrationInfoChanged
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(PersonalizeTimelineNodesBasedOnUserSession)
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

    protected override async Task<OperationResult> ExecuteSyncTaskAsync(CancellationToken cancellationToken)
    {
        // lấy registration info trước mới có context học kỳ - năm
        var rulesTask = _registrationRulesService.RefreshRegistrationAsync(cancellationToken);
        var plannedTask = LoadPlannedCoursesCommand.Execute().ToTask(cancellationToken);
        await Task.WhenAll(rulesTask, plannedTask);

        if (!rulesTask.Result.IsSuccess) return rulesTask.Result;

        var tuitionResult = await LoadTuitionFeeCommand.Execute().ToTask(cancellationToken);

        return OperationResult.Combine(plannedTask.Result, tuitionResult);
    }

    private void ApplyTeachingPlanToTimeline(TeachingPlanData? teachingPlan)
    {
        if (teachingPlan is null)
        {
            return;
        }

        TimelineViewModel.Nodes.Clear();
        foreach (var item in teachingPlan.RegistrationTimeline)
        {
            TimelineViewModel.Nodes.Add(new TimelineNodeViewModel(item));
        }

        PersonalizeTimelineNodesBasedOnUserSession(RegistrationInfo);
    }

    private void PersonalizeTimelineNodesBasedOnUserSession(RegistrationInformation? regInfo)
    {
        // đăng ký học phần Đợt 1 và Đợt 2
        var registrationPhase1Node = TimelineViewModel.Nodes.FirstOrDefault(n => n.Step == TeachingPlanStep.CourseRegistrationPhase1);
        var registrationPhase2Node = TimelineViewModel.Nodes.FirstOrDefault(n => n.Step == TeachingPlanStep.CourseRegistrationPhase2);
        var originalRegistrationPhase1 = TeachingPlan?.RegistrationTimeline?.FirstOrDefault(n => n.GetStepType() == TeachingPlanStep.CourseRegistrationPhase1);
        var originalRegistrationPhase2 = TeachingPlan?.RegistrationTimeline?.FirstOrDefault(n => n.GetStepType() == TeachingPlanStep.CourseRegistrationPhase2);

        var userPeriod = regInfo?.UserPeriod;
        var isPhase1Matched = false;
        var isPhase2Matched = false;

        if (userPeriod is { StartDate: { } userStart, EndDate: { } userEnd })
        {
            var daysDifferenceToPhase1 = originalRegistrationPhase1 is not null 
                ? Math.Abs((userStart - originalRegistrationPhase1.StartDate).TotalDays) 
                : double.MaxValue;
            var daysDifferenceToPhase2 = originalRegistrationPhase2 is not null 
                ? Math.Abs((userStart - originalRegistrationPhase2.StartDate).TotalDays) 
                : double.MaxValue;
            
            // Xác định đợt khớp hơn và chỉ áp dụng nếu đợt cá nhân bắt đầu muộn hơn đợt trường (tránh đè nhầm lịch chung)
            if (daysDifferenceToPhase1 < daysDifferenceToPhase2)
            {
                isPhase1Matched = originalRegistrationPhase1 is null || originalRegistrationPhase1.StartDate < userStart;
            }
            else
            {
                isPhase2Matched = originalRegistrationPhase2 is null || originalRegistrationPhase2.StartDate < userStart;
            }
        }

        var personalScheduleSubtitle = userPeriod is not null 
            ? $"Đã áp dụng lịch cá nhân ({userPeriod.Key} - Nhóm {userPeriod.AllowedGroupsDisplay})" 
            : null;

        ApplyOrRestoreTimelineNode(
            registrationPhase1Node, 
            originalRegistrationPhase1, 
            isPhase1Matched ? userPeriod?.StartDate : null, 
            isPhase1Matched ? userPeriod?.EndDate : null, 
            personalScheduleSubtitle);

        ApplyOrRestoreTimelineNode(
            registrationPhase2Node, 
            originalRegistrationPhase2, 
            isPhase2Matched ? userPeriod?.StartDate : null, 
            isPhase2Matched ? userPeriod?.EndDate : null, 
            personalScheduleSubtitle);

        // Cá nhân hóa Đợt điều chỉnh Kế hoạch học tập (KHTT)
        var adjustStudyPlanNode = TimelineViewModel.Nodes.FirstOrDefault(n => n.Step == TeachingPlanStep.AdjustStudyPlan);
        var originalAdjustStudyPlan = TeachingPlan?.RegistrationTimeline?.FirstOrDefault(n => n.GetStepType() == TeachingPlanStep.AdjustStudyPlan);

        if (adjustStudyPlanNode is not null && originalAdjustStudyPlan is not null)
        {
            var matchedAdjustmentDetail = TryFindMatchingAdjustmentDetail(userPeriod);
            ApplyOrRestoreTimelineNode(
                adjustStudyPlanNode, 
                originalAdjustStudyPlan, 
                matchedAdjustmentDetail?.StartDateTime, 
                matchedAdjustmentDetail?.EndDateTime, 
                matchedAdjustmentDetail is not null ? personalScheduleSubtitle : null);
        }
    }

    private void ApplyOrRestoreTimelineNode(
        TimelineNodeViewModel? targetNodeViewModel, 
        TimelineNode? originalSchoolNode, 
        DateTime? personalizedStartDate, 
        DateTime? personalizedEndDate, 
        string? personalizedSubtitle)
    {
        if (targetNodeViewModel is null || originalSchoolNode is null) return;
        targetNodeViewModel.StartDate = personalizedStartDate ?? originalSchoolNode.StartDate;
        targetNodeViewModel.EndDate = personalizedEndDate ?? originalSchoolNode.EndDate;
        targetNodeViewModel.Subtitle = personalizedStartDate.HasValue ? personalizedSubtitle : null;
    }

    private TeachingPlanAdjustmentDetail? TryFindMatchingAdjustmentDetail(UserPeriodItem? userPeriod)
    {
        if (userPeriod is null || TeachingPlan?.AdjustmentDetails is null) return null;

        var cohortNumberMatch = CohortNumberRegex().Match(userPeriod.Key);
        if (!cohortNumberMatch.Success || !int.TryParse(cohortNumberMatch.Value, out int studentCohort)) return null;

        var studentGroups = userPeriod.AllowedGroups;

        return TeachingPlan.AdjustmentDetails.FirstOrDefault(detail =>
        {
            var detailCohortNumberMatch = CohortNumberRegex().Match(detail.Cohort);
            bool isCohortMatched;

            if (detailCohortNumberMatch.Success && int.TryParse(detailCohortNumberMatch.Value, out int detailCohortNumber))
            {
                if (detail.Cohort.Contains("trở về trước") || detail.Cohort.Contains("trở xuống"))
                    isCohortMatched = studentCohort <= detailCohortNumber;
                else if (detail.Cohort.Contains("trở về sau") || detail.Cohort.Contains("trở lên"))
                    isCohortMatched = studentCohort >= detailCohortNumber;
                else
                    isCohortMatched = studentCohort == detailCohortNumber;
            }
            else
            {
                isCohortMatched = detail.Cohort.Contains(userPeriod.Key, StringComparison.OrdinalIgnoreCase);
            }

            return isCohortMatched && detail.AllowedGroups.Intersect(studentGroups).Any();
        });
    }

    protected override void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            TimelineViewModel.Dispose();
        }

        base.Dispose(isDisposing);
    }
}