using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.TimetableRefactor.Models;
using DynamicData;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;

public class TimetableViewModel: ViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly ReadOnlyObservableCollection<ScheduleCellUi> _scheduleCells;
    private readonly ReadOnlyObservableCollection<ScheduleGroupCellShared> _courseList;
    private readonly ReadOnlyObservableCollection<ScheduleGroupCellShared> _unscheduledCourses;
    
    public ReadOnlyObservableCollection<ScheduleCellUi> ScheduleCells => _scheduleCells;
    public ReadOnlyObservableCollection<ScheduleGroupCellShared> CourseList => _courseList;
    public ReadOnlyObservableCollection<ScheduleGroupCellShared> UnscheduledCourses => _unscheduledCourses;

    public TimetableViewModel(IObservable<IChangeSet<TimetableRenderItem>> renderStream)
    {
        renderStream
            .TransformMany(item => item.Cells) 
            .Bind(out _scheduleCells)
            .Subscribe()
            .DisposeWith(_disposables);
        
        renderStream
            .Transform(item => item.SharedData)
            .Bind(out _courseList)
            .Subscribe()
            .DisposeWith(_disposables);

        renderStream
            .Filter(item => item.Cells is { Count: 0 })
            .Transform(item => item.SharedData)
            .Bind(out _unscheduledCourses)
            .Subscribe()
            .DisposeWith(_disposables);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}