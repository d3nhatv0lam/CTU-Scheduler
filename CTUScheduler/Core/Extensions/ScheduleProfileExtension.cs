using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Core.Extensions;

public static class ScheduleProfileExtension
{
    public static bool TryAddToScheduleProfile(this ScheduleProfile timetable, SectionChoice sectionChoice)
    {
        var courseCode = sectionChoice.Course.Code;
        var group = sectionChoice.Section.Group;
        
        return timetable.SavedCourseGroupKeys.TryAdd(courseCode, group);
    }
}