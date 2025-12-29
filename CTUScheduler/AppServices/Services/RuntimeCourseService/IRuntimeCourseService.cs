using System.Collections.Generic;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.AppServices.Services.RuntimeCourseService;

internal interface IRuntimeCourseService: ICourseStateService
{
    internal bool RegisterTimetable(ScheduleBlueprint blueprint);
    void RegisterTimetables(IEnumerable<ScheduleBlueprint> blueprints);
    bool UnregisterTimetable(ScheduleProfile profile);
    void ClearAll();
}