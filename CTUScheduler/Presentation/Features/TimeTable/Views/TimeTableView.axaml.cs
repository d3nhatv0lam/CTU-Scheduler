using System.Collections;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Presentation.Features.TimeTable.ViewModels;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.TimeTable.Views;

public partial class TimeTableView : ReactiveUserControl<TimeTableViewModel>
{
    public TimeTableView()
    {
        InitializeComponent();
    }
}