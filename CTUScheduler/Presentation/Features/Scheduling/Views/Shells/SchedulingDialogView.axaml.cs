using CTUScheduler.Presentation.Features.Scheduling.ViewModels.Shells;
using ReactiveUI.Avalonia;

namespace CTUScheduler.Presentation.Features.Scheduling.Views.Shells;

public partial class SchedulingDialogView : ReactiveUserControl<SchedulingDialogViewModel>
{
    public SchedulingDialogView()
    {
        InitializeComponent();
    }
}