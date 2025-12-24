using System;
using System.Collections.Generic;
using System.Linq;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;

namespace CTUScheduler.Core.Helpers;

public static class ScheduleOptimizer
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="courses"></param>
    /// <param name="tables"></param>
    public static void Trim(List<Course> courses, List<ScheduleProfile> tables)
    {
        if (courses is null || tables is null) return;
        
        tables.RemoveAll(t => t.SavedCourseGroupKeys.Count == 0);

        if (tables.Count == 0)
        {
            courses.Clear();
            return;
        }
        
        var requiredKeys = tables
            .SelectMany(t => t.SavedCourseGroupKeys)
            .Select(kvp => (Code: kvp.Key, Group: kvp.Value))
            .ToHashSet();
        
        courses.RemoveAll(course =>
        {
            var validSections = new List<CourseSection>();
            foreach (var section in course.Sections)
            {
                if (requiredKeys.Contains((course.Code, section.Group)))
                {
                    validSections.Add(section);
                }
            }
            
            if (validSections.Count > 0)
            {
                course.Sections = validSections; 
                return false;
            }
            return true;
        });
    }
}