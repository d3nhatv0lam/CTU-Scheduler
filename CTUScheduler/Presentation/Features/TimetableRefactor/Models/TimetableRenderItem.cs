using System;
using System.Collections.Generic;
using System.Linq;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.Models;

public class TimetableRenderItem: IDisposable
{
    public ScheduleGroupCellShared SharedData { get; }
    public IReadOnlyList<ScheduleCellUi> Cells { get; } 

    public TimetableRenderItem(ScheduleGroupCellShared shared, IEnumerable<ScheduleCellUi> cells)
    {
        SharedData = shared;
        Cells = cells.ToList(); 
    }

    public void Dispose()
    {
        SharedData.Dispose();
    }
}