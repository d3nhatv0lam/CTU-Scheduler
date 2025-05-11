using CTUScheduler.AppServices.Services.Interfaces;
using CTUScheduler.Presentation.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Presentation.ViewModels.CoursePage
{
    public class CoursePageViewModel: ViewModelBase, IRoutableViewModel
    {
        private readonly ICTUWebDriverService _CTUWebDriverService;
        public string? UrlPathSegment => "CoursePageViewModel";
        public IScreen HostScreen { get; }

        public ObservableCollection<string> CourseList { get; set; } = new ObservableCollection<string>() { };
        public CoursePageViewModel()
        {
        }
        public CoursePageViewModel(IScreen hostScreen)
        {
            HostScreen = hostScreen;
            _CTUWebDriverService = App.ServiceProvider!.GetRequiredService<ICTUWebDriverService>();

            GoToCourseCatalogPage();
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
