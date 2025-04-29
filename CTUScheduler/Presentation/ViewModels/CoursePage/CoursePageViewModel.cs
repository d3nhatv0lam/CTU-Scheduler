using CTUScheduler.Presentation.ViewModels.Base;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Presentation.ViewModels.CoursePage
{
    public class CoursePageViewModel: ViewModelBase, IRoutableViewModel
    {
        public string? UrlPathSegment => "CoursePageViewModel";
        public IScreen HostScreen { get; }
        public CoursePageViewModel()
        {
        }
        public CoursePageViewModel(IScreen hostScreen)
        {
            HostScreen = hostScreen;
        }
    }
}
