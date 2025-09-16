using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Presentation.Features.Timetable.ViewModels;

namespace CTUScheduler.Presentation.Services.Adapter;

public interface ITimetableLayoutAdapter: IAdapter<TimetableLayoutViewModel>
{
    TimetableLayoutViewModel GetOrCreateLayout(ScheduleTable table);
}