using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CTUScheduler.AppServices.Services.Network;
using CTUScheduler.AppServices.Services.ScheduleManager;
using CTUScheduler.AppServices.Services.WebDriver;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Scheduling.Shells.ViewModels;
using CTUScheduler.Presentation.Features.Timetable.ViewModels;
using CTUScheduler.Presentation.Services.Adapter;
using CTUScheduler.Presentation.Services.Dialogs;
using CTUScheduler.Presentation.Services.TimetableDialog;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.TimetableManager.ViewModels
{
    public class TimetableManagerViewModel : ViewModelBase, IRoutableViewModel, IDisposable
    {
        private readonly CompositeDisposable _disposables = new ();
        private readonly ICTUWebDriverService _CTUWebDriverService;
        private readonly IScheduleService _scheduleService;
        private readonly ITimetableLayoutAdapter _timetableLayoutVmAdapter;
        private readonly IDialogHostService _dialogHostService;
        private readonly ITimetableDialogService _timetableDialogService;
        private readonly IConnectivityService  _connectivityService;

        private readonly ReadOnlyObservableCollection<TimetableLayoutViewModel> _bindableTimetableLayouts =
            ReadOnlyObservableCollection<TimetableLayoutViewModel>.Empty;

        private readonly ObservableAsPropertyHelper<int> _timetableLayoutsCount;
        private readonly ObservableAsPropertyHelper<bool> _isEmptyTimetableLayouts;
        private readonly ObservableAsPropertyHelper<bool> _isExpiredSaved;
        private readonly ObservableAsPropertyHelper<DateTime?> _lastSaved;
        public string? UrlPathSegment => "TimetableManagerViewModel";
        public IScreen HostScreen { get; }
        public ReadOnlyObservableCollection<TimetableLayoutViewModel> TimetableLayouts => _bindableTimetableLayouts;
        public int TimetableLayoutsCount => _timetableLayoutsCount.Value;
        public bool IsEmptyTimetableLayouts => _isEmptyTimetableLayouts.Value;
        public bool IsExpiredSaved => _isExpiredSaved.Value;
        public DateTime? LastSaved => _lastSaved.Value;

        public ReactiveCommand<Unit, Unit> ShowAddCourseDialogCommand { get; }
        public ReactiveCommand<IStorageFile[], Unit> LoadScheduleCommand { get; }
        public ReactiveCommand<IStorageFile, Unit> SaveScheduleCommand { get; }
        public ReactiveCommand<Unit, Unit> ReloadAllTimetableCommand { get; }
        public ReactiveCommand<TimetableLayoutViewModel, Unit> ShowTimetableDetailsCommand { get; }
        

        public TimetableManagerViewModel()
        {
        }

        public TimetableManagerViewModel(IScreen hostScreen)
        {
            HostScreen = hostScreen;
            _dialogHostService = App.ServiceProvider.GetRequiredService<IDialogHostService>();
            _timetableDialogService = App.ServiceProvider.GetRequiredService<ITimetableDialogService>();
            _timetableLayoutVmAdapter = App.ServiceProvider.GetRequiredService<ITimetableLayoutAdapter>();
            _CTUWebDriverService = App.ServiceProvider.GetRequiredService<ICTUWebDriverService>();
            _scheduleService = App.ServiceProvider.GetRequiredService<IScheduleService>();
            _connectivityService = App.ServiceProvider.GetRequiredService<IConnectivityService>();

            _scheduleService.TimetableChanges
                .Transform(x => _timetableLayoutVmAdapter.GetOrCreateLayout(x))
                .DisposeMany()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _bindableTimetableLayouts)
                .Subscribe()
                .DisposeWith(_disposables);

            _timetableLayoutsCount = _scheduleService.TimetableCountChanges
                .ToProperty(this, nameof(TimetableLayoutsCount), scheduler: RxApp.MainThreadScheduler)
                .DisposeWith(_disposables);

            _isEmptyTimetableLayouts = this.WhenAnyValue(x => x.TimetableLayoutsCount)
                .Select(count => count == 0)
                .ToProperty(this, nameof(IsEmptyTimetableLayouts), scheduler: RxApp.MainThreadScheduler)
                .DisposeWith(_disposables);

            _isExpiredSaved = _scheduleService.IsExpiredSaved
                .ToProperty(this, nameof(IsExpiredSaved), scheduler: RxApp.MainThreadScheduler)
                .DisposeWith(_disposables);

            _lastSaved = _scheduleService.LastSaveChanged
                .ToProperty(this, nameof(LastSaved), scheduler: RxApp.MainThreadScheduler)
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
                await _timetableLayoutVmAdapter.UpdateAsync();
            },canReloadAllTimetable).DisposeWith(_disposables);

            ShowTimetableDetailsCommand = ReactiveCommand.CreateFromTask<TimetableLayoutViewModel>(async
                    timetableLayoutViewModel =>
                    await _timetableDialogService.ShowTimetableDetails(timetableLayoutViewModel))
                .DisposeWith(_disposables);

            SaveScheduleCommand = ReactiveCommand.CreateFromTask<IStorageFile>(async file =>
            {
                var filePath = file.Path.LocalPath;
                if (await _scheduleService.TrySaveScheduleAsync(filePath))
                {
                    
                }
                else
                {
                    
                }
            }).DisposeWith(_disposables);

            LoadScheduleCommand = ReactiveCommand.CreateFromTask<IStorageFile[]>(async files =>
                {
                    foreach (var file in files)
                    {
                        var filePath = file.Path.LocalPath;
                        var isLoaded = await Task.Run(async () => await _scheduleService.TryLoadScheduleAsync(filePath));
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
            var viewModel = new DialogShellViewModel();
            await _dialogHostService.ShowDialogAsync<DialogShellViewModel, Unit>(viewModel,
                DialogIdentifier.MainLayout);
        }

        private void GoToCourseCatalogPage()
        {
            try
            {
                _CTUWebDriverService.GoToCourseCatalogPage();
            }
            catch
            {
                // ignored
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}