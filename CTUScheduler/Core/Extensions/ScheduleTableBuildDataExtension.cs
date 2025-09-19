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
    public static ScheduleTableBuildData Trim(this ScheduleTableBuildData buildData)
    {
        if (!buildData.IsValid())
            return buildData;

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
                course.Sections = validSections;
                trimmedCourses.Add(course);
            }
        }

        return buildData with { Courses = trimmedCourses };
    }
}