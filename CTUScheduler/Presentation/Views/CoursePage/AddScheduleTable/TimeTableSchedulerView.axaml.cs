using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.ViewModels.CoursePage.AddScheduleTable;

namespace CTUScheduler.Presentation.Views.CoursePage.AddScheduleTable;

public partial class TimeTableSchedulerView : ReactiveUserControl<TimeTableSchedulerViewModel>
{
    public TimeTableSchedulerView()
    {
        InitializeComponent();
    }
}