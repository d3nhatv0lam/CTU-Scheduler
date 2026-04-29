using ReactiveUI.Avalonia;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels.Steps;

namespace CTUScheduler.Presentation.Features.Scheduling.Views.Steps;

public partial class TimetableSchedulerView : ReactiveUserControl<TimetableSchedulerViewModel>
{
    public TimetableSchedulerView()
    {
        InitializeComponent();
    }
}