using System;
using System.Collections.Generic;
using System.Linq;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;

namespace CTUScheduler.Core.Helpers;

public static class ScheduleDataPruner
{
    /// <summary>
    /// Optimizes the provided list of courses by removing unnecessary data based on the specified schedule profiles.
    /// Returns a new list of pruned courses.
    /// </summary>
    /// <param name="courses">The list of courses to be trimmed.</param>
    /// <param name="profiles">The list of schedule profiles used to determine which courses and sections to keep. This list will be altered by removing profiles with no saved course group keys.</param>
    /// <returns>A new list of courses containing only relevant sections.</returns>
    public static List<Course> Prune(IEnumerable<Course> courses, List<ScheduleProfile> profiles)
    {
        if (courses is null || profiles is null) return new List<Course>();
        
        profiles.RemoveAll(t => t.SavedCourseGroupKeys.Count == 0);

        if (profiles.Count == 0)
        {
            return new List<Course>();
        }
        
        var requiredKeys = profiles
            .SelectMany(t => t.SavedCourseGroupKeys)
            .Select(kvp => (Code: kvp.Key, Group: kvp.Value))
            .ToHashSet();
        
        return courses
            .Select(course =>
            {
                var validSections = course.Sections
                    .Where(section => requiredKeys.Contains((course.Code, section.Group)))
                    .ToList();

                if (validSections.Count > 0)
                {
                    return course with { Sections = validSections };
                }
                return null;
            })
            .Where(c => c is not null)
            .Cast<Course>()
            .ToList();
    }
}