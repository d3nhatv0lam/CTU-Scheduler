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

namespace CTUScheduler.Presentation.Features.TimeTableManager.ViewModels
{
    public class TimeTableManagerViewModel: ViewModelBase, IRoutableViewModel, IActivatableViewModel
    {
        private readonly ICTUWebDriverService _CTUWebDriverService;
        private readonly IDialogHostService _dialogHostService;
        public string? UrlPathSegment => "TimeTableManagerViewModel";
        public IScreen HostScreen { get; }
        public ViewModelActivator Activator { get; } = new ViewModelActivator();

        public ObservableCollection<string> CourseList { get; set; } = new ObservableCollection<string>() { };

        public ReactiveCommand<Unit, Unit> OpenAddCourseDialogCommand { get; protected set; }

        
        public TimeTableManagerViewModel()
        {
        }
        public TimeTableManagerViewModel(IScreen hostScreen)
        {
            HostScreen = hostScreen;
            _dialogHostService = App.ServiceProvider!.GetRequiredService<IDialogHostService>();
            _CTUWebDriverService = App.ServiceProvider!.GetRequiredService<ICTUWebDriverService>();

            GoToCourseCatalogPage();
            OpenAddCourseDialogCommand = ReactiveCommand.Create(OpenAddCourseDialog);

            this.WhenActivated((CompositeDisposable disposeables) => 
            {
                Debug.Write("actived!!!!!");
                OpenAddCourseDialogCommand.DisposeWith(disposeables);
            });
            
        }

        private void OpenAddCourseDialog()
        {
            var viewModel = new DialogViewModel(DialogHostService.DialogIdentifier.MainLayout);
            _dialogHostService.ShowDialog<Unit>(viewModel, DialogHostService.DialogIdentifier.MainLayout);
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
    }
}
