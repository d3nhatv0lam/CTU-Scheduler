using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.Scheduling.ViewModels;

namespace CTUScheduler.Presentation.Scheduling.Views;

public partial class TimeTableSchedulerView : ReactiveUserControl<TimeTableSchedulerViewModel>
{
    public TimeTableSchedulerView()
    {
        InitializeComponent();
    }
}