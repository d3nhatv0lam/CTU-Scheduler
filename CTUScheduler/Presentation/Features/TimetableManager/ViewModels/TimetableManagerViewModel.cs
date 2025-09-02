using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using CTUScheduler.AppServices.Services.Dialogs;
using CTUScheduler.AppServices.Services.WebDriver;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.TimetableManager.ViewModels
{
    public class TimetableManagerViewModel: ViewModelBase, IRoutableViewModel, IActivatableViewModel, IDisposable
    {
        private readonly CompositeDisposable _disposable = new CompositeDisposable();
        private readonly ICTUWebDriverService _CTUWebDriverService;
        private readonly IDialogHostService _dialogHostService;
        public string? UrlPathSegment => "TimetableManagerViewModel";
        public IScreen HostScreen { get; }
        public ViewModelActivator Activator { get; } = new ViewModelActivator();

        public ObservableCollection<string> CourseList { get; set; } = new ObservableCollection<string>() { };

        public ReactiveCommand<Unit, Unit> OpenAddCourseDialogCommand { get; protected set; }
        
        public TimetableManagerViewModel()
        {
        }
        public TimetableManagerViewModel(IScreen hostScreen)
        {
            HostScreen = hostScreen;
            _dialogHostService = App.ServiceProvider!.GetRequiredService<IDialogHostService>();
            _CTUWebDriverService = App.ServiceProvider!.GetRequiredService<ICTUWebDriverService>();

            GoToCourseCatalogPage();
            OpenAddCourseDialogCommand = ReactiveCommand.Create(OpenAddCourseDialog)
                .DisposeWith(_disposable);
            
        }

        private void OpenAddCourseDialog()
        {
            var viewModel = new DialogViewModel();
            _dialogHostService.ShowDialogAsync<Unit>(viewModel, DialogHostService.DialogIdentifier.MainLayout);
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
            _disposable.Dispose();
        }
    }
}
