using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Services.ScheduleService;
using CTUScheduler.AppServices.Services.UserSessionService;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Infrastructure.Services.Network;
using CTUScheduler.Infrastructure.Services.Registration;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Scheduling.Shells.ViewModels;
using CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;
using CTUScheduler.Presentation.Services.Dialogs;
using CTUScheduler.Presentation.Services.Factories;
using CTUScheduler.Presentation.Services.TimetableDialog;
using CTUScheduler.Presentation.Shared.Dialogs.ViewModels;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System.Linq;

namespace CTUScheduler.Presentation.Features.TimetableManager.ViewModels
{
    public class TimetableManagerViewModel : ViewModelBase, IRoutableViewModel, IDisposable
    {
        private readonly CompositeDisposable _disposables = new ();
        private readonly IDialogHostService _dialogHostService;
        private readonly ITimetableDialogService _timetableDialogService;
        private readonly IConnectivityService  _connectivityService;
        private readonly ICourseCatalogService _courseCatalogService;
        private readonly IProfileQueryService _profileQueryService;
        private readonly IScheduleSyncService _scheduleSyncService;
        private readonly IScheduleRegistrationService _scheduleRegistrationService;
        private readonly IUserSessionService _userSessionService;
        private readonly IWorkspaceStore _workspaceStore;
        private readonly IViewModelFactory _viewModelFactory;

        private readonly ReadOnlyObservableCollection<TimetableEditorViewModel> _bindableTimetableLayouts =
            ReadOnlyObservableCollection<TimetableEditorViewModel>.Empty;

        private readonly ObservableAsPropertyHelper<int> _timetableLayoutsCount;
        private readonly ObservableAsPropertyHelper<bool> _isEmptyTimetableLayouts;
        private readonly ObservableAsPropertyHelper<bool> _isExpiredSaved;
        private readonly ObservableAsPropertyHelper<string> _lastSavedText;
        private readonly ObservableAsPropertyHelper<bool> _hasSelectedTimetable;
        public bool HasSelectedTimetable => _hasSelectedTimetable.Value;
        public string? UrlPathSegment => "TimetableManagerViewModel";
        public IScreen HostScreen { get; }
        public ReadOnlyObservableCollection<TimetableEditorViewModel> TimetableLayouts => _bindableTimetableLayouts;
        public int TimetableLayoutsCount => _timetableLayoutsCount.Value;
        public bool IsEmptyTimetableLayouts => _isEmptyTimetableLayouts.Value;
        public bool IsExpiredSaved => _isExpiredSaved.Value;
        public string LastSaved => _lastSavedText.Value;

        public ReactiveCommand<Unit, Unit> ShowAddCourseDialogCommand { get;  }
        public ReactiveCommand<IReadOnlyList<IStorageFile>, Unit> LoadScheduleCommand { get; }
        public ReactiveCommand<IStorageFile, Unit> SaveScheduleCommand { get; }
        public ReactiveCommand<Unit, Unit> ReloadAllTimetableCommand { get; }

        public ReactiveCommand<Unit, bool> DeleteSelectedTimetablesCommand { get;}
        public ReactiveCommand<TimetableLayoutBaseViewModel, Unit> ShowTimetableDetailsCommand { get; }
        
        

        public TimetableManagerViewModel(
            IScreen hostScreen,
            IConnectivityService connectivityService,
            ICourseCatalogService courseCatalogService,
            IUserSessionService userSessionService,
            IWorkspaceStore workspaceStore,
            IProfileQueryService profileQueryService,
            IScheduleSyncService scheduleSyncService,
            IScheduleRegistrationService scheduleRegistrationService,
            IViewModelFactory viewModelFactory,
            IDialogHostService dialogHostService,
            ITimetableDialogService timetableDialogService)
        {
            HostScreen = hostScreen;
            _dialogHostService = dialogHostService;
            _timetableDialogService = timetableDialogService;
            _connectivityService = connectivityService;
            _courseCatalogService = courseCatalogService;
            _userSessionService = userSessionService;
            _workspaceStore = workspaceStore;
            _profileQueryService =  profileQueryService;
            _scheduleSyncService = scheduleSyncService;
            _scheduleRegistrationService = scheduleRegistrationService;
            _viewModelFactory = viewModelFactory;
            
            
            _profileQueryService.ConnectProfiles()
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Transform(viewModelFactory.Create<TimetableEditorViewModel,ScheduleProfile>)
                .DisposeMany()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _bindableTimetableLayouts)
                .Subscribe()
                .DisposeWith(_disposables);
            
            _hasSelectedTimetable = TimetableLayouts.ToObservableChangeSet()
                .AutoRefresh(x => x.IsSelected)
                .ToCollection()
                .Select(items => items.Any(x => x.IsSelected))
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, nameof(HasSelectedTimetable))
                .DisposeWith(_disposables);
            
            _timetableLayoutsCount = _profileQueryService.ConnectProfiles()
                .Count()
                .ToProperty(this, nameof(TimetableLayoutsCount), scheduler: RxApp.MainThreadScheduler)
                .DisposeWith(_disposables);
            
            _lastSavedText = userSessionService.LastSaved
                .Select(savedTime =>
                {
                    if (savedTime is null) return Observable.Return("Chưa có sao lưu!");
                    return Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(60))
                        .Select(_ => FormatTime(savedTime.Value));
                })
                .Switch()
                .ToProperty(this, nameof(LastSaved), scheduler: RxApp.MainThreadScheduler)
                .DisposeWith(_disposables);
            
            _isEmptyTimetableLayouts = this.WhenAnyValue(x => x.TimetableLayoutsCount)
                .Select(count => count == 0)
                .ToProperty(this, nameof(IsEmptyTimetableLayouts), scheduler: RxApp.MainThreadScheduler)
                .DisposeWith(_disposables);
            
            _isExpiredSaved = userSessionService.IsReadonly
                .ToProperty(this, nameof(IsExpiredSaved), scheduler: RxApp.MainThreadScheduler)
                .DisposeWith(_disposables);
            
            GoToCourseCatalogPage();

            var canInteractUi = this.WhenAnyValue(x => x.IsExpiredSaved, expired => !expired);
            ShowAddCourseDialogCommand = ReactiveCommand.CreateFromTask(OpenAddCourseDialog,canInteractUi)
                .DisposeWith(_disposables);
            
            var canReloadAllTimetable = _connectivityService.IsInternetAvailable
                .DistinctUntilChanged()
                .CombineLatest(canInteractUi, (isOnline, canInteract) => isOnline && canInteract)
                .ObserveOn(RxApp.MainThreadScheduler);
            
            ReloadAllTimetableCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (IsEmptyTimetableLayouts) return;
                await _scheduleSyncService.RefreshCoursesAsync();
            },canReloadAllTimetable).DisposeWith(_disposables);
            
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

                var result = await _dialogHostService.ShowDialogAsync<ConfirmDialogViewModel, bool>(
                    confirmViewModel, DialogIdentifier.MainLayout, false);

                if (result)
                {
                    foreach (var timetable in TimetableLayouts.Where(x => x.IsSelected))
                    {
                        _scheduleRegistrationService.UnregisterProfile(timetable.ScheduleProfile);
                    }
                }
                
                return result;
            }, this.WhenAnyValue(x => x.HasSelectedTimetable))
            .DisposeWith(_disposables);

            ShowTimetableDetailsCommand = ReactiveCommand.CreateFromTask<TimetableLayoutBaseViewModel>(async
                    timetableLayoutViewModel =>
                    await _timetableDialogService.ShowTimetableDetails(timetableLayoutViewModel))
                .DisposeWith(_disposables);

            SaveScheduleCommand = ReactiveCommand.CreateFromTask<IStorageFile>(async file =>
            {
                var filePath = file.Path.LocalPath;
                if (await workspaceStore.SaveAsync(filePath))
                {
                    
                }
                else
                {
                    
                }
            }).DisposeWith(_disposables);

            LoadScheduleCommand = ReactiveCommand.CreateFromTask<IReadOnlyList<IStorageFile>>(async files =>
                {
                    foreach (var file in files)
                    {
                        var filePath = file.Path.LocalPath;
                        var isLoaded = await workspaceStore.LoadAsync(filePath);
                        if (isLoaded)
                        {
                            
                        }
                        else
                        {
                            
                        }
                        break;
                    }
                })
                .DisposeWith(_disposables);
        }

        private async Task OpenAddCourseDialog()
        {
            using var viewModel = _viewModelFactory.Create<DialogShellViewModel>();
            await _dialogHostService.ShowDialogAsync<DialogShellViewModel, Unit>(viewModel,
                DialogIdentifier.MainLayout, false);
        }

        private async void GoToCourseCatalogPage()
        {
            try
            {
                 await _courseCatalogService.EnsureReadyAsync();
            }
            catch
            {
                // ignored
            }
        }

        private string FormatTime(DateTimeOffset time)
        {
            return time.Humanize(culture: new CultureInfo("vi-VN"));
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}