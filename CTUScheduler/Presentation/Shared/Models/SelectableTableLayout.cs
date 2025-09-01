using System;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Presentation.Features.TimeTable.ViewModels;

namespace CTUScheduler.Presentation.Shared.Models;

public class SelectableTimetableLayout: SelectableItem<TimeTableLayoutViewModel>, IDisposable
{
    public SelectableTimetableLayout(TimeTableLayoutViewModel vm) : base(vm)
    {
    }

    public void Dispose()
    {
        Item?.Dispose();
    }
}