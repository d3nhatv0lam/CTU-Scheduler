using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
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
    public class TimetableManagerViewModel : ViewModelBase, IRoutableViewModel, IActivatableViewModel, IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly ICTUWebDriverService _CTUWebDriverService;
        private readonly IScheduleService _scheduleService;
        private readonly ITimetableLayoutAdapter _timetableLayoutVmAdapter;
        private readonly IDialogHostService _dialogHostService;
        private readonly ITimetableDialogService _timetableDialogService;

        private readonly ReadOnlyObservableCollection<TimetableLayoutViewModel> _bindableTimetableLayouts =
            ReadOnlyObservableCollection<TimetableLayoutViewModel>.Empty;

        private readonly ObservableAsPropertyHelper<int> _timetableLayoutsCount;
        private readonly ObservableAsPropertyHelper<bool> _isEmptyTimetableLayouts;
        public string? UrlPathSegment => "TimetableManagerViewModel";
        public IScreen HostScreen { get; }
        public ViewModelActivator Activator { get; } = new();

        public ReadOnlyObservableCollection<TimetableLayoutViewModel> TimetableLayouts => _bindableTimetableLayouts;
        public int TimetableLayoutsCount => _timetableLayoutsCount.Value;
        public bool IsEmptyTimetableLayouts => _isEmptyTimetableLayouts.Value;

        public ReactiveCommand<Unit, Unit> ShowAddCourseDialogCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveScheduleCommand { get; }
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

            GoToCourseCatalogPage();
            ShowAddCourseDialogCommand = ReactiveCommand.CreateFromTask(OpenAddCourseDialog)
                .DisposeWith(_disposables);
            
            ReloadAllTimetableCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (IsEmptyTimetableLayouts) return;
                await _timetableLayoutVmAdapter.UpdateAsync();
            }).DisposeWith(_disposables);

            ShowTimetableDetailsCommand = ReactiveCommand.CreateFromTask<TimetableLayoutViewModel>(async
                    timetableLayoutViewModel =>
                    await _timetableDialogService.ShowTimetableDetails(timetableLayoutViewModel))
                .DisposeWith(_disposables);

            SaveScheduleCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await _scheduleService.TrySaveScheduleAsync();
            }).DisposeWith(_disposables);
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