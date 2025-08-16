using System.Collections;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Presentation.Features.TimeTable.ViewModels;
using ReactiveUI;

namespace CTUScheduler.Presentation.TimeTable.Views;

public partial class TimeTableView : ReactiveUserControl<TimeTableViewModel>
{
    public TimeTableView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.OneWayBind<TimeTableViewModel, TimeTableView, ObservableCollection<ScheduleCell>, IEnumerable>(ViewModel, vm => vm.ScheduleTable.ScheduleCells, x => x.scheduleTable.ItemsSource).DisposeWith(disposables);
        });
    }
}