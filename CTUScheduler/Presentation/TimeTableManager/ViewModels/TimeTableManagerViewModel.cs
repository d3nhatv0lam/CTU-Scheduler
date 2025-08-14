using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using CTUScheduler.AppServices.Services.Interfaces;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Scheduling.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace CTUScheduler.Presentation.TimeTableManager.ViewModels
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
            var viewModel = new DialogViewModel("MainLayoutDialog");
            _dialogHostService.ShowDialog<Unit>(viewModel, "MainLayoutDialog");
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
