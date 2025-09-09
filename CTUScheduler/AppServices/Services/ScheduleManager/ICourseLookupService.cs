using System.Collections.Generic;
using System.Collections.Specialized;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.UserSaves;

namespace CTUScheduler.AppServices.Services.ScheduleManager;

public interface ICourseLookupService
{
    void BuildLookupMaps(ScheduleSave data);
    Course? GetCourse(string code);
    CourseSection? GetSection(string courseCode, string group);
    List<CourseSection?> GetScheduledCourses(ScheduleTable scheduleTable);
}