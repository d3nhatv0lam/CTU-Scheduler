using System;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Presentation.Features.Timetable.ViewModels;

namespace CTUScheduler.Presentation.Shared.Models;

public class SelectableTimetableLayout: SelectableItem<TimetableLayoutViewModel>
{
    public SelectableTimetableLayout(TimetableLayoutViewModel vm) : base(vm)
    {
    }
}