using CTUScheduler.Presentation.ViewModels.Base;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Presentation.ViewModels.HomePage
{
    public class HomePageViewModel : ViewModelBase, IRoutableViewModel
    {
        public string? UrlPathSegment => "HomeViewModel";

        public IScreen HostScreen { get; }

        public HomePageViewModel()
        {
        }

        public HomePageViewModel(IScreen hostScreen)
        {
            HostScreen = hostScreen;
        }
    }
}
