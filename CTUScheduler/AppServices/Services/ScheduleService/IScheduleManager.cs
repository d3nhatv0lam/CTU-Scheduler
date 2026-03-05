using System.Collections.Generic;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;

namespace CTUScheduler.AppServices.Services.ScheduleService;

public interface IScheduleManager :
    IScheduleRegistrationService,
    ICourseQueryService,
    IProfileQueryService,
    IScheduleSyncService
{
    void ImportSchedule(IEnumerable<Course> courses, IEnumerable<ScheduleProfile> profiles);
}