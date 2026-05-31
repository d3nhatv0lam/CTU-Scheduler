using System;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Presentation.Services.Navigation;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Services.ScheduleService;
using CTUScheduler.AppServices.Services.UserSessionService;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Infrastructure.Excel;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;
using CTUScheduler.Presentation.Services.Factories;
using CTUScheduler.Presentation.Shared.Dialogs.ViewModels;
using DynamicData;
using DynamicData.Aggregation;
using Humanizer;
using ReactiveUI;
using System.Linq;
using System.Threading;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels.Shells;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using CTUScheduler.Presentation.Services.UserInteractionService.Models.Dialogs;
using CTUScheduler.Presentation.Shared.Models.Identifiers;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Presentation.Features.TimetableManager.ViewModels
{
    public class TimetableManagerViewModel : SessionSyncViewModelBase, IRoutableViewModel
    {
        private readonly IConnectivityService _connectivityService;
        private readonly IProfileQueryService _profileQueryService;
        private readonly IScheduleSyncService _scheduleSyncService;
        private readonly IScheduleRegistrationService _scheduleRegistrationService;
        private readonly IExcelExporterService _excelExporterService;
        private readonly IUserSessionService _userSessionService;
        private readonly IWorkspaceStore _workspaceStore;
        private readonly IViewModelFactory _viewModelFactory;

        private readonly ReadOnlyObservableCollection<TimetableEditorViewModel> _bindableTimetableLayouts;

        private readonly ObservableAsPropertyHelper<int> _timetableLayoutsCount;
        private readonly ObservableAsPropertyHelper<bool> _isEmptyTimetableLayouts;
        private readonly ObservableAsPropertyHelper<bool> _isExpiredSaved;
        private readonly ObservableAsPropertyHelper<string> _lastSavedText;
        private readonly ObservableAsPropertyHelper<bool> _hasSelectedTimetable;
        public bool HasSelectedTimetable => _hasSelectedTimetable.Value;
        public string UrlPathSegment => nameof(TimetableManagerViewModel);
        public IScreen HostScreen { get; }
        public ReadOnlyObservableCollection<TimetableEditorViewModel> TimetableLayouts => _bindableTimetableLayouts;
        public int TimetableLayoutsCount => _timetableLayoutsCount.Value;
        public bool IsEmptyTimetableLayouts => _isEmptyTimetableLayouts.Value;
        public bool IsExpiredSaved => _isExpiredSaved.Value;
        public string LastSaved => _lastSavedText.Value;

        public ReactiveCommand<Unit, Unit> ShowAddCourseDialogCommand { get; }
        public ReactiveCommand<IReadOnlyList<IStorageFile>, Unit> LoadScheduleCommand { get; }
        public ReactiveCommand<IStorageFile, Unit> SaveScheduleCommand { get; }
        public ReactiveCommand<Unit, Unit> ReloadAllTimetableCommand { get; }
        public ReactiveCommand<Unit, Unit> ExportSelectedTimetablesCommand { get; }
        public ReactiveCommand<Unit, bool> DeleteSelectedTimetablesCommand { get; }
        public ReactiveCommand<TimetableLayoutBaseViewModel, Unit> ShowTimetableDetailsCommand { get; }


        public TimetableManagerViewModel(
            IScreen hostScreen,
            IConnectivityService connectivityService,
            IUserSessionService userSessionService,
            IWorkspaceStore workspaceStore,
            IProfileQueryService profileQueryService,
            IScheduleSyncService scheduleSyncService,
            IScheduleRegistrationService scheduleRegistrationService,
            IExcelExporterService excelExporterService,
            IViewModelFactory viewModelFactory,
            IUserInteractionService userInteractionService,
            INavigationRegionManager navigationRegionManager,
            ILogger<TimetableManagerViewModel> logger) : base(userInteractionService, navigationRegionManager,
            connectivityService, logger)
        {
            HostScreen = hostScreen;
            _connectivityService = connectivityService;
            _userSessionService = userSessionService;
            _workspaceStore = workspaceStore;
            _profileQueryService = profileQueryService;
            _scheduleSyncService = scheduleSyncService;
            _scheduleRegistrationService = scheduleRegistrationService;
            _excelExporterService = excelExporterService;
            _viewModelFactory = viewModelFactory;

            var sharedViewModelsStream = _profileQueryService.ConnectProfiles()
                .ObserveOn(RxSchedulers.TaskpoolScheduler)
                .Transform(viewModelFactory.Create<TimetableEditorViewModel, ScheduleProfile>)
                .DisposeMany()
                .Publish();

            sharedViewModelsStream
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Bind(out _bindableTimetableLayouts)
                .Subscribe()
                .DisposeWith(Disposables);

            _hasSelectedTimetable = sharedViewModelsStream
                .AutoRefresh(x => x.IsSelected)
                .ObserveOn(RxSchedulers.TaskpoolScheduler)
                .Filter(x => x.IsSelected)
                .Count()
                .Select(x => x > 0)
                .ToProperty(this, nameof(HasSelectedTimetable), scheduler: RxSchedulers.MainThreadScheduler)
                .DisposeWith(Disposables);

            sharedViewModelsStream.Connect()
                .DisposeWith(Disposables);

            _timetableLayoutsCount = _profileQueryService.ConnectProfiles()
                .Count()
                .ToProperty(this, nameof(TimetableLayoutsCount), scheduler: RxSchedulers.MainThreadScheduler)
                .DisposeWith(Disposables);

            _lastSavedText = userSessionService.LastSaved
                .Select(savedTime =>
                {
                    if (savedTime is null) return Observable.Return("Chưa có sao lưu!");
                    return Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(60))
                        .Select(_ => FormatTime(savedTime.Value));
                })
                .Switch()
                .ToProperty(this, nameof(LastSaved), scheduler: RxSchedulers.MainThreadScheduler)
                .DisposeWith(Disposables);

            _isEmptyTimetableLayouts = this.WhenAnyValue(x => x.TimetableLayoutsCount)
                .Select(count => count == 0)
                .ToProperty(this, nameof(IsEmptyTimetableLayouts), scheduler: RxSchedulers.MainThreadScheduler)
                .DisposeWith(Disposables);

            _isExpiredSaved = userSessionService.IsReadonly
                .ToProperty(this, nameof(IsExpiredSaved), scheduler: RxSchedulers.MainThreadScheduler)
                .DisposeWith(Disposables);

            var canInteractUi = this.WhenAnyValue(x => x.IsExpiredSaved, expired => !expired);
            ShowAddCourseDialogCommand = ReactiveCommand.CreateFromTask(OpenAddCourseDialog, canInteractUi)
                .DisposeWith(Disposables);

            var canReloadAllTimetable = _connectivityService.IsInternetAvailable
                .DistinctUntilChanged()
                .CombineLatest(canInteractUi, (isOnline, canInteract) => isOnline && canInteract)
                .ObserveOn(RxSchedulers.MainThreadScheduler);

            ReloadAllTimetableCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (IsEmptyTimetableLayouts) return;
                var result = await _scheduleSyncService.RefreshCoursesAsync();

                result.Match(
                    () => UserInteractionService.Toast.Light.Success("Cập nhật thông tin thời khóa biểu thành công!"),
                    (errors, kind) =>
                    {
                        var messages = string.Join("\n", errors.Select(e => e.FormattedMessage));
                        if (kind == OperationFailureReason.UserAction) return;

                        UserInteractionService.Notification.Light.Error(messages);
                    }
                );
            }, canReloadAllTimetable).DisposeWith(Disposables);

            DeleteSelectedTimetablesCommand = ReactiveCommand.CreateFromTask(async () =>
                {
                    using var confirmViewModel = new ConfirmDialogViewModel
                    {
                        Title = "Xóa thời khóa biểu",
                        Message = "Bạn có chắc chắn xóa các thời khóa biểu đã chọn ?",
                        ConfirmText = "Xóa",
                        CancelText = "Không",
                        IsDestructive = true
                    };

                    var options = new DialogOptions()
                    {
                        SizeMode = DialogSizeMode.Content,
                        CanLightDismiss = true,
                        HostId = DialogIds.Root
                    };

                    var result =
                        await UserInteractionService.Dialog
                            .ShowModal<ConfirmDialogViewModel, bool>(confirmViewModel, options);

                    if (result)
                    {
                        foreach (var timetable in TimetableLayouts.Where(x => x.IsSelected))
                        {
                            _scheduleRegistrationService.UnregisterProfile(timetable.ScheduleProfile);
                        }
                    }

                    return result;
                }, this.WhenAnyValue(x => x.HasSelectedTimetable))
                .DisposeWith(Disposables);

            ExportSelectedTimetablesCommand = ReactiveCommand.CreateFromTask(async () =>
                {
                    var selectedTimetables = TimetableLayouts.Where(x => x.IsSelected).ToList();
                    if (selectedTimetables.Count == 0) return;

                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    string fileName = $"CTU_Scheduler_TKB_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    string fullPath = System.IO.Path.Combine(desktopPath, fileName);

                    var blueprints = selectedTimetables
                        .Select((layout, index) =>
                        {
                            var sheetName = string.IsNullOrWhiteSpace(layout.Name)
                                ? $"TKB_{index + 1}"
                                : layout.Name;
                            return (Blueprint: layout.ToScheduleBlueprint(), SheetName: sheetName);
                        })
                        .ToList();

                    await _excelExporterService.ExportTimetablesAsync(blueprints, fullPath);
                }, this.WhenAnyValue(x => x.HasSelectedTimetable))
                .DisposeWith(Disposables);

            ShowTimetableDetailsCommand = ReactiveCommand.CreateFromTask<TimetableLayoutBaseViewModel>(async
                    timetableLayoutViewModel =>
                {
                    var options = new DialogOptions()
                    {
                        SizeMode = DialogSizeMode.Responsive,
                        CanLightDismiss = true,
                        HostId = DialogIds.Root
                    };
                    await UserInteractionService.Dialog.ShowModal<TimetableLayoutBaseViewModel, Unit>(
                        timetableLayoutViewModel, options);
                })
                .DisposeWith(Disposables);

            SaveScheduleCommand = ReactiveCommand.CreateFromTask<IStorageFile>(async file =>
            {
                var filePath = file.Path.LocalPath;
                var result = await Task.Run(() => workspaceStore.SaveAsync(filePath));

                result.Match(
                    onSuccess: () =>
                    {
                        Logger.LogInformation("Lưu thời khóa biểu thành công vào {Path}", filePath);
                        UserInteractionService.Toast.Light.Success("Lưu dữ liệu thành công!");
                    },
                    onFailure: (_, _) =>
                        UserInteractionService.Notification.Light.Error("Lỗi lưu file", result.FormattedErrorMessage));
            }).DisposeWith(Disposables);

            LoadScheduleCommand = ReactiveCommand.CreateFromTask<IReadOnlyList<IStorageFile>>(async files =>
                {
                    var file = files.FirstOrDefault();
                    if (file is null) return;

                    var filePath = file.Path.LocalPath;

                    var result = await Task.Run(() => workspaceStore.LoadAsync(filePath));

                    result.Match(
                        onSuccess: () =>
                        {
                            Logger.LogInformation("Nạp thời khóa biểu thành công từ {Path}", filePath);
                            UserInteractionService.Toast.Light.Success("Nạp dữ liệu thành công!");
                        },
                        onFailure: (_, _) =>
                            UserInteractionService.Notification.Light.Error("Lỗi nạp file",
                                result.FormattedErrorMessage)
                    );
                })
                .DisposeWith(Disposables);
        }

        private async Task OpenAddCourseDialog()
        {
            using var viewModel = _viewModelFactory.Create<SchedulingDialogViewModel>();
            var options = new DialogOptions()
            {
                SizeMode = DialogSizeMode.Content,
                IsCloseButtonVisible = false,
                CanLightDismiss = false,
                HostId = DialogIds.Root
            };
            await UserInteractionService.Dialog.ShowModal<SchedulingDialogViewModel, Unit>(viewModel, options);
        }

        protected override Task<OperationResult> ExecuteSyncTaskAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(OperationResult.Success());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_bindableTimetableLayouts != null)
                {
                    foreach (var layout in _bindableTimetableLayouts)
                    {
                        layout.Dispose();
                    }
                }
            }
            base.Dispose(disposing);
        }

        private string FormatTime(DateTimeOffset time)
        {
            return time.Humanize(culture: new CultureInfo("vi-VN"));
        }
    }
}