using System.Collections.Generic;
using CTUScheduler.AppServices.Services.ScheduleService.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;

namespace CTUScheduler.AppServices.Services.ScheduleService;

public interface IScheduleManager :
    IScheduleRegistrationService,
    ICourseQueryService,
    IProfileQueryService
{
    void ImportSchedule(IEnumerable<Course> courses, IEnumerable<ScheduleProfile> profiles);
}