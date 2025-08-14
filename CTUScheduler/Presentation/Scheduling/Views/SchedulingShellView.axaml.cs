using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.Scheduling.ViewModels;

namespace CTUScheduler.Presentation.Scheduling.Views;

public partial class SchedulingShellView : ReactiveUserControl<SchedulingShellViewModel>
{
    public SchedulingShellView()
    {
        InitializeComponent();
    }
}