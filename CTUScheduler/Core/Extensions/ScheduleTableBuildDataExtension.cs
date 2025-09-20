using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Core.Extensions;

public static class ScheduleTableBuildDataExtension
{
    /// <summary>
    ///  Remove all course sections that are not in the saved course group keys.
    /// </summary>
    /// <param name="buildData"></param>
    /// <param name="result">ScheduleTableBuildData trimmed when valid</param>
    /// <returns></returns>
    public static bool TryTrim(this ScheduleTableBuildData buildData, out ScheduleTableBuildData result)
    {
        if (!buildData.IsValid())
        {
            result = default!;
            return false;
        }

        var allowedKeys = buildData.ScheduleTable.SavedCourseGroupKeys
            .Select(key => (key.Key, key.Value))
            .ToHashSet();

        var trimmedCourses = new List<Course>();
    
        foreach (var course in buildData.Courses)
        {
            var validSections = new List<CourseSection>();
        
            foreach (var section in course.Sections)
            {
                if (allowedKeys.Contains((course.Code, section.Group)))
                {
                    validSections.Add(section);
                }
            }

            if (validSections.Count > 0)
            {
                var newCourse = course.CloneWithNewCourseSections(validSections);
                trimmedCourses.Add(newCourse);
            }
        }
        
        result = buildData with { Courses = trimmedCourses };
        return true;
    }
}