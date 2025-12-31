using System.Collections.Generic;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Legacy.RuntimeCourseService;

public interface IRuntimeCourseService: ICourseStateService
{
    void RegisterTimetable(ScheduleBlueprint blueprint);
    void RegisterTimetable(IEnumerable<ScheduleBlueprint> blueprints);
    void UnregisterTimetable(ScheduleProfile profile);
    void ClearAll();
}