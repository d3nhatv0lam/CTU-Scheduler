using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels;

namespace CTUScheduler.Presentation.Features.Scheduling.Views;

public partial class SchedulingShellView : ReactiveUserControl<SchedulingShellViewModel>
{
    public SchedulingShellView()
    {
        InitializeComponent();
    }
}