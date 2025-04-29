using CTUScheduler.Presentation.ViewModels.Base;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Presentation.ViewModels.SettingsPage
{
    public class SettingsPageViewModel: ViewModelBase, IRoutableViewModel
    {
        public string? UrlPathSegment => "SettingsPageViewModel";
        public IScreen HostScreen { get; }
        public SettingsPageViewModel()
        {
        }
        public SettingsPageViewModel(IScreen hostScreen)
        {
            HostScreen = hostScreen;
        }
    }
}
