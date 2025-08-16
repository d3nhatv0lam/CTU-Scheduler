using CTUScheduler.Presentation.Base;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Setting.ViewModels
{
    public class SettingViewModel: ViewModelBase, IRoutableViewModel
    {
        public string? UrlPathSegment => "SettingViewModel";
        public IScreen HostScreen { get; }
        public SettingViewModel()
        {
        }
        public SettingViewModel(IScreen hostScreen)
        {
            HostScreen = hostScreen;
        }
    }
}
