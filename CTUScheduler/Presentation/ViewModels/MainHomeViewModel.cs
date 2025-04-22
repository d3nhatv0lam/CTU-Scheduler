using CTUScheduler.Presentation.ViewModels.Base;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Presentation.ViewModels
{
    public class MainHomeViewModel : ViewModelBase, IRoutableViewModel
    {
        public string? UrlPathSegment => "MainHome view";

        public IScreen HostScreen { get; }

        public MainHomeViewModel()
        {
        }

        public MainHomeViewModel(IScreen hostScreen)
        {
            HostScreen = hostScreen;
        }
    }
}
