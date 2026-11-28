using System;
using System.Globalization;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Services.UserSessionService;
using CTUScheduler.Core.Models.Academic.Grades;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Grades.Helpers;
using CTUScheduler.Presentation.Features.Grades.ViewModels.Components;
using CTUScheduler.Presentation.Services.Navigation;
using CTUScheduler.Presentation.Services.Theme;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Features.Grades.ViewModels;

public sealed partial class GradesViewModel : SessionSyncViewModelBase, IRoutableViewModel
{
    private readonly IGradeService _gradeService;
    private readonly IGradeStore _gradeStore;
    private int _forceRefreshFlag;

    [ObservableAsProperty] private GradeTranscript? _transcript;
    [ObservableAsProperty] private string _cumulativeGpaDisplay = "--";
    [ObservableAsProperty] private string _totalCreditsDisplay = "--";
    [ObservableAsProperty] private string _conductScoreDisplay = "--";
    [ObservableAsProperty] private string _gradedCourseCountDisplay = "--";
    [ObservableAsProperty] private string _lastUpdatedDisplay = "Chưa tải";
    [ObservableAsProperty] private bool _isInitialLoading;
    [ObservableAsProperty] private bool _hasNoTranscriptData;
    [Reactive] private int _selectedTabIndex;

    public string UrlPathSegment => nameof(GradesViewModel);
    public IScreen HostScreen { get; }
    public GradeTranscriptListViewModel TranscriptListViewModel { get; } = new();
    public GradeChartsViewModel ChartsViewModel { get; }
    public ReactiveCommand<Unit, OperationResult> ReloadGradesCommand { get; }

    public GradesViewModel(
        IScreen hostScreen,
        IGradeService gradeService,
        IGradeStore gradeStore,
        IThemeService themeService,
        IUserInteractionService userInteractionService,
        INavigationRegionManager navigationRegionManager,
        IConnectivityService connectivityService,
        ILogger<GradesViewModel> logger)
        : base(userInteractionService, navigationRegionManager, connectivityService, logger)
    {
        ArgumentNullException.ThrowIfNull(hostScreen);
        ArgumentNullException.ThrowIfNull(gradeService);
        ArgumentNullException.ThrowIfNull(gradeStore);
        ArgumentNullException.ThrowIfNull(themeService);

        HostScreen = hostScreen;
        _gradeService = gradeService;
        _gradeStore = gradeStore;
        ChartsViewModel = new GradeChartsViewModel(themeService);

        var canReload = ConnectivityService.IsInternetAvailable
            .CombineLatest(SyncSessionCommand.IsExecuting, (hasNet, isSyncing) => hasNet && !isSyncing)
            .ObserveOn(RxSchedulers.MainThreadScheduler);

        ReloadGradesCommand = ReactiveCommand.CreateFromTask(
                async ct =>
                {
                    Interlocked.Exchange(ref _forceRefreshFlag, 1);
                    return await SyncSessionCommand.Execute().ToTask(ct);
                },
                canReload)
            .DisposeWith(Disposables);

        _transcriptHelper = _gradeStore.Changed
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .ToProperty(this, nameof(Transcript))
            .DisposeWith(Disposables);

        var kpiStream = this.WhenAnyValue(x => x.Transcript)
            .Select(GradeVisualHelper.ComputeKpis);

        _cumulativeGpaDisplayHelper = kpiStream
            .Select(k => k.LatestCumulativeGpa.HasValue
                ? k.LatestCumulativeGpa.Value.ToString("0.00", CultureInfo.InvariantCulture)
                : "--")
            .ToProperty(this, nameof(CumulativeGpaDisplay))
            .DisposeWith(Disposables);

        _totalCreditsDisplayHelper = kpiStream
            .Select(k => Transcript is null ? "--" : $"{k.TotalAccumulatedCredits} TC")
            .ToProperty(this, nameof(TotalCreditsDisplay))
            .DisposeWith(Disposables);

        _conductScoreDisplayHelper = kpiStream
            .Select(k => k.LatestConductScore.HasValue ? $"{k.LatestConductScore.Value} đ" : "--")
            .ToProperty(this, nameof(ConductScoreDisplay))
            .DisposeWith(Disposables);

        _gradedCourseCountDisplayHelper = kpiStream
            .Select(k => Transcript is null ? "--" : $"{k.GradedCourseCount} môn")
            .ToProperty(this, nameof(GradedCourseCountDisplay))
            .DisposeWith(Disposables);

        _lastUpdatedDisplayHelper = this.WhenAnyValue(x => x.Transcript)
            .Select(t => t is null
                ? "Chưa tải"
                : $"Cập nhật lúc {t.FetchedAt.ToLocalTime():HH:mm dd/MM/yyyy}")
            .ToProperty(this, nameof(LastUpdatedDisplay))
            .DisposeWith(Disposables);

        _isInitialLoadingHelper = this.WhenAnyValue(
                x => x.IsLoading,
                x => x.Transcript,
                (isLoading, transcript) => isLoading && transcript is null)
            .ToProperty(this, nameof(IsInitialLoading))
            .DisposeWith(Disposables);

        _hasNoTranscriptDataHelper = this.WhenAnyValue(
                x => x.IsLoading,
                x => x.Transcript,
                (isLoading, transcript) => !isLoading && transcript is { Semesters.Count: 0 })
            .ToProperty(this, nameof(HasNoTranscriptData))
            .DisposeWith(Disposables);

        this.WhenAnyValue(x => x.Transcript)
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(t =>
            {
                TranscriptListViewModel.UpdateTranscript(t);
                ChartsViewModel.UpdateTranscript(t);
            })
            .DisposeWith(Disposables);
    }

    protected override async Task<OperationResult> ExecuteSyncTaskAsync(CancellationToken cancellationToken)
    {
        bool forceRefresh = Interlocked.Exchange(ref _forceRefreshFlag, 0) == 1;
        if (!forceRefresh && _gradeStore.Current is not null)
        {
            return OperationResult.Success();
        }

        return await _gradeService.RefreshGradesAsync(cancellationToken);
    }

    protected override void OnSyncFailed(OperationResult result)
    {
        if (result.Kind is OperationFailureReason.Unauthorized or OperationFailureReason.UserAction)
        {
            return;
        }

        string message = result.FirstErrorMessage ?? "Không thể tải dữ liệu bảng điểm từ hệ thống trường.";
        UserInteractionService.Notification.Error("Tải bảng điểm thất bại", message);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            TranscriptListViewModel.Dispose();
            ChartsViewModel.Dispose();
        }

        base.Dispose(disposing);
    }
}
