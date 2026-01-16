using CTUScheduler.Core.Interfaces;
using CTUScheduler.Presentation.Base;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Setting.ViewModels
{
    public class SettingViewModel: ViewModelBase, IRoutableViewModel, INeedArgs<IScreen>
    {
        public string? UrlPathSegment => "SettingViewModel";
        public IScreen HostScreen { get; }
        
        public SettingViewModel(IScreen hostScreen)
        {
            HostScreen = hostScreen;
        }
    }
}
