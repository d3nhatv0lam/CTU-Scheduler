using CTUScheduler.Presentation.Features.Timetable.ViewModels;
using CTUScheduler.Presentation.Shared.Models;

namespace CTUScheduler.Presentation.Features.Timetable.Models;

public class SelectableTimetableLayout: SelectableItem<TimetableLayoutViewModel>
{
    public SelectableTimetableLayout(TimetableLayoutViewModel vm) : base(vm)
    {
    }
}