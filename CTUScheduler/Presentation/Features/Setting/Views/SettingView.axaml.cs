using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.Features.Setting.ViewModels;

namespace CTUScheduler.Presentation.Features.Setting.Views;

public partial class SettingView : ReactiveUserControl<SettingViewModel>
{
    public SettingView()
    {
        InitializeComponent();
    }
}
