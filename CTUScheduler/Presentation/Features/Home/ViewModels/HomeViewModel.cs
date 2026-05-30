using System;
using System.Collections.Generic;
using System.Linq;
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
    private readonly IRegistrationRulesRefactorService _registrationRulesService;

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
        IRegistrationRulesRefactorService registrationRulesService,
        ICourseRegistrationRefactorService courseRegistrationService,
        ITuitionFeeRefactorService tuitionFeeService,
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
        // 1. Phân phối đăng ký học phần Đợt 1 (Node 2) và Đợt 2 (Node 7)
        var dot1Node = TimelineViewModel.Nodes.FirstOrDefault(n => n.Title.Contains("Đợt 1"));
        var dot2Node = TimelineViewModel.Nodes.FirstOrDefault(n => n.Title.Contains("Đợt 2"));

        var originalDot1 = TeachingPlan?.RegistrationTimeline?.FirstOrDefault(n => n.Title.Contains("Đợt 1"));
        var originalDot2 = TeachingPlan?.RegistrationTimeline?.FirstOrDefault(n => n.Title.Contains("Đợt 2"));

        if (regInfo?.UserPeriod is not null)
        {
            var p = regInfo.UserPeriod;
            if (p.StartDate.HasValue && p.EndDate.HasValue)
            {
                // Tự động phân phối lịch cá nhân vào đúng đợt đăng ký bằng cách tìm đợt gần nhất (độ lệch ngày nhỏ nhất)
                double distToDot1 = originalDot1 is not null
                    ? Math.Abs((p.StartDate.Value - originalDot1.StartDate).TotalDays)
                    : double.MaxValue;
                double distToDot2 = originalDot2 is not null
                    ? Math.Abs((p.StartDate.Value - originalDot2.StartDate).TotalDays)
                    : double.MaxValue;

                if (distToDot1 < distToDot2) // Khớp Đợt 1 hơn
                {
                    if (dot1Node is not null)
                    {
                        dot1Node.StartDate = p.StartDate.Value;
                        dot1Node.EndDate = p.EndDate.Value;
                        dot1Node.Subtitle = $"Đã áp dụng lịch cá nhân ({p.Key} - Nhóm {p.AllowedGroupsDisplay})";
                    }

                    // Khôi phục Đợt 2 về lịch trường
                    if (dot2Node is not null && originalDot2 is not null)
                    {
                        dot2Node.StartDate = originalDot2.StartDate;
                        dot2Node.EndDate = originalDot2.EndDate;
                        dot2Node.Subtitle = null;
                    }
                }
                else // Khớp Đợt 2 hơn
                {
                    if (dot2Node is not null)
                    {
                        dot2Node.StartDate = p.StartDate.Value;
                        dot2Node.EndDate = p.EndDate.Value;
                        dot2Node.Subtitle = $"Đã áp dụng lịch cá nhân ({p.Key} - Nhóm {p.AllowedGroupsDisplay})";
                    }

                    // Khôi phục Đợt 1 về lịch trường
                    if (dot1Node is not null && originalDot1 is not null)
                    {
                        dot1Node.StartDate = originalDot1.StartDate;
                        dot1Node.EndDate = originalDot1.EndDate;
                        dot1Node.Subtitle = null;
                    }
                }
            }
        }
        else
        {
            // Reset cả hai về lịch trường khi không đăng nhập / không có lịch cá nhân
            if (dot1Node is not null && originalDot1 is not null)
            {
                dot1Node.StartDate = originalDot1.StartDate;
                dot1Node.EndDate = originalDot1.EndDate;
                dot1Node.Subtitle = null;
            }

            if (dot2Node is not null && originalDot2 is not null)
            {
                dot2Node.StartDate = originalDot2.StartDate;
                dot2Node.EndDate = originalDot2.EndDate;
                dot2Node.Subtitle = null;
            }
        }

        // 2. Cá nhân hóa Đợt điều chỉnh KHTT (Node 3: "Điều chỉnh kế hoạch học tập")
        var khttNode = TimelineViewModel.Nodes.FirstOrDefault(n => n.Title.Contains("Điều chỉnh kế hoạch học tập"));
        var originalKhtt =
            TeachingPlan?.RegistrationTimeline?.FirstOrDefault(n => n.Title.Contains("Điều chỉnh kế hoạch học tập"));

        if (khttNode is not null && originalKhtt is not null)
        {
            if (regInfo?.UserPeriod is not null && TeachingPlan?.AdjustmentDetails is not null)
            {
                var p = regInfo.UserPeriod;
                // Trích xuất số khóa từ p.Key (ví dụ: "Khóa 50" -> 50)
                var cohortNumberMatch = System.Text.RegularExpressions.Regex.Match(p.Key, @"\d+");
                if (cohortNumberMatch.Success && int.TryParse(cohortNumberMatch.Value, out int studentCohort))
                {
                    var studentGroups = p.AllowedGroups ?? Array.Empty<int>();

                    // Tìm kiếm AdjustmentDetail phù hợp từ bảng 3 của PDF
                    var matchedDetail = TeachingPlan.AdjustmentDetails.FirstOrDefault(detail =>
                    {
                        // 1. Kiểm tra khớp khóa
                        bool isCohortMatched = false;
                        var detailCohortMatch = System.Text.RegularExpressions.Regex.Match(detail.Cohort, @"\d+");
                        if (detailCohortMatch.Success && int.TryParse(detailCohortMatch.Value, out int detailCohortNum))
                        {
                            if (detail.Cohort.Contains("trở về trước") || detail.Cohort.Contains("trở xuống"))
                                isCohortMatched = studentCohort <= detailCohortNum;
                            else if (detail.Cohort.Contains("trở về sau") || detail.Cohort.Contains("trở lên"))
                                isCohortMatched = studentCohort >= detailCohortNum;
                            else
                                isCohortMatched = studentCohort == detailCohortNum;
                        }
                        else
                        {
                            isCohortMatched = detail.Cohort.Contains(p.Key, StringComparison.OrdinalIgnoreCase);
                        }

                        if (!isCohortMatched) return false;

                        // 2. Kiểm tra khớp nhóm đơn vị
                        return detail.AllowedGroups.Intersect(studentGroups).Any();
                    });

                    if (matchedDetail is not null)
                    {
                        khttNode.StartDate = matchedDetail.StartDateTime;
                        khttNode.EndDate = matchedDetail.EndDateTime;
                        khttNode.Subtitle = $"Đã áp dụng lịch cá nhân ({p.Key} - Nhóm {p.AllowedGroupsDisplay})";
                    }
                    else
                    {
                        khttNode.StartDate = originalKhtt.StartDate;
                        khttNode.EndDate = originalKhtt.EndDate;
                        khttNode.Subtitle = null;
                    }
                }
                else
                {
                    khttNode.StartDate = originalKhtt.StartDate;
                    khttNode.EndDate = originalKhtt.EndDate;
                    khttNode.Subtitle = null;
                }
            }
            else
            {
                khttNode.StartDate = originalKhtt.StartDate;
                khttNode.EndDate = originalKhtt.EndDate;
                khttNode.Subtitle = null;
            }
        }
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