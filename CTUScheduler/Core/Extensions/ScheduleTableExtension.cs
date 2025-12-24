using System.Collections;
using System.Collections.Generic;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Core.Extensions;

public static class ScheduleTableExtension
{
    public static bool TryAddToScheduleData(this ScheduleProfile timetable, SectionChoice sectionChoice)
    {
        var courseCode = sectionChoice.Course.Code;
        var group = sectionChoice.Section.Group;
        
        return timetable.SavedCourseGroupKeys.TryAdd(courseCode, group);
    }
}