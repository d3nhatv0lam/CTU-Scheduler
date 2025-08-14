using CTUScheduler.AppServices.Services.Implementations;
using CTUScheduler.AppServices.Services.Interfaces;
using CTUScheduler.Presentation.ViewModels.Base;
using CTUScheduler.Presentation.ViewModels.CoursePage.AddScheduleTable;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Presentation.ViewModels.CoursePage
{
    public class CoursePageViewModel: ViewModelBase, IRoutableViewModel, IActivatableViewModel
    {
        private readonly ICTUWebDriverService _CTUWebDriverService;
        private readonly IDialogHostService _dialogHostService;
        public string? UrlPathSegment => "CoursePageViewModel";
        public IScreen HostScreen { get; }
        public ViewModelActivator Activator { get; } = new ViewModelActivator();

        public ObservableCollection<string> CourseList { get; set; } = new ObservableCollection<string>() { };

        public ReactiveCommand<Unit, Unit> OpenAddCourseDialogCommand { get; protected set; }

        
        public CoursePageViewModel()
        {
        }
        public CoursePageViewModel(IScreen hostScreen)
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
