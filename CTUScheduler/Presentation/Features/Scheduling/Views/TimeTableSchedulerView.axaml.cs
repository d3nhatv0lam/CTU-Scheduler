using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels;

namespace CTUScheduler.Presentation.Features.Scheduling.Views;

public partial class TimeTableSchedulerView : ReactiveUserControl<TimeTableSchedulerViewModel>
{
    public TimeTableSchedulerView()
    {
        InitializeComponent();
    }
}