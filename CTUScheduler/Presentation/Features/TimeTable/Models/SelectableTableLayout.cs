using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Presentation.Features.TimeTable.ViewModels;

namespace CTUScheduler.Presentation.Features.TimeTable.Models;

public class SelectableTimetableLayout: SelectableItem<TimeTableLayoutViewModel>
{
    public SelectableTimetableLayout(TimeTableLayoutViewModel vm) : base(vm)
    {
        
    }
}