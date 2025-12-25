using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.AppServices.Services.RuntimeCourseService;

public interface IRuntimeCourseService
{
    void RegisterTimetable(ScheduleBlueprint blueprint);
    void UnregisterTimetable(ScheduleProfile profile);
    void ClearAll();
    
}