using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DynamicData;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Timetable.Models;

public class TimetableRenderItem: IDisposable
{
    private readonly SourceList<ScheduleCellUi> _sourceList;
    public ScheduleGroupCellShared SharedData { get; }
    public ReadOnlyObservableCollection<ScheduleCellUi> Cells { get; }
    
    private readonly IDisposable _cleanUp;

    public TimetableRenderItem(ScheduleGroupCellShared shared, IEnumerable<ScheduleCellUi> cells)
    {
        SharedData = shared;
        _sourceList = new SourceList<ScheduleCellUi>();
        _sourceList.AddRange(cells);

        _cleanUp = _sourceList.Connect()
            .Bind(out var readOnlyCells)
            .Subscribe();

        Cells = readOnlyCells;
    }

    public void Dispose()
    {
        _cleanUp.Dispose();
        _sourceList.Dispose();
        SharedData.Dispose();
    }
}