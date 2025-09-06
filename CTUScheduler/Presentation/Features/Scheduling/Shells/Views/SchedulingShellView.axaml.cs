using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.Features.Scheduling.Shells.ViewModels;

namespace CTUScheduler.Presentation.Features.Scheduling.Shells.Views;

public partial class SchedulingShellView : ReactiveUserControl<SchedulingShellViewModel>
{
    public SchedulingShellView()
    {
        InitializeComponent();
    }
}