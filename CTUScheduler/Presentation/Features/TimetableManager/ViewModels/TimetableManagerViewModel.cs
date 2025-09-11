using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Services.ScheduleManager;
using CTUScheduler.AppServices.Services.WebDriver;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Scheduling.Shells.ViewModels;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels;
using CTUScheduler.Presentation.Features.Timetable.ViewModels;
using CTUScheduler.Presentation.Services.Dialogs;
using CTUScheduler.Presentation.Services.TimetableDialog;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.TimetableManager.ViewModels
{
    public class TimetableManagerViewModel: ViewModelBase, IRoutableViewModel, IActivatableViewModel, IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly ICTUWebDriverService _CTUWebDriverService;
        private readonly IScheduleManagerService _scheduleManagerService;
        private readonly IDialogHostService _dialogHostService;
        private readonly ITimetableDialogService _timetableDialogService;
        private ReadOnlyObservableCollection<TimetableLayoutViewModel> _bindableTimetableLayouts;
        public string? UrlPathSegment => "TimetableManagerViewModel";
        public IScreen HostScreen { get; }
        public ViewModelActivator Activator { get; } = new ();
        
        public ReadOnlyObservableCollection<TimetableLayoutViewModel> TimetableLayouts => _bindableTimetableLayouts;
        public ReactiveCommand<Unit, Unit> ShowAddCourseDialogCommand { get; protected set; }
        public ReactiveCommand<TimetableLayoutViewModel, Unit> ShowTimetableDetailsCommand { get; }
        
        public TimetableManagerViewModel()
        {
        }
        public TimetableManagerViewModel(IScreen hostScreen)
        {
            HostScreen = hostScreen;
            _dialogHostService = App.ServiceProvider!.GetRequiredService<IDialogHostService>();
            _timetableDialogService = App.ServiceProvider!.GetRequiredService<ITimetableDialogService>();
            _CTUWebDriverService = App.ServiceProvider!.GetRequiredService<ICTUWebDriverService>();
            _scheduleManagerService = App.ServiceProvider!.GetRequiredService<IScheduleManagerService>();
            
            this.WhenActivated((CompositeDisposable disposable) =>
            {
                _scheduleManagerService.Timetables
                    .Transform(x => new TimetableLayoutViewModel(x))
                    .DisposeMany()
                    .Bind(out _bindableTimetableLayouts)
                    .Subscribe()
                    .DisposeWith(_disposables);
            });

            GoToCourseCatalogPage();
            ShowAddCourseDialogCommand = ReactiveCommand.CreateFromTask(OpenAddCourseDialog)
                .DisposeWith(_disposables);
            
            ShowTimetableDetailsCommand =  ReactiveCommand.CreateFromTask<TimetableLayoutViewModel>(async timetableLayoutViewModel =>
                    await _timetableDialogService.ShowTimetableDetails(timetableLayoutViewModel))
                .DisposeWith(_disposables);
        }

        private async Task OpenAddCourseDialog()
        {
            var viewModel = new DialogShellViewModel();
            await _dialogHostService.ShowDialogAsync<DialogShellViewModel,Unit>(viewModel, DialogIdentifier.MainLayout);
        }

        public void GoToCourseCatalogPage()
        {
            try
            {
                _CTUWebDriverService.GoToCourseCatalogPage();
            }
            catch
            {

            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
