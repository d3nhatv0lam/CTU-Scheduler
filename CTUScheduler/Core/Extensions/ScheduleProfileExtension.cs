using System;
using System.Collections.Generic;
using CTUScheduler.AppServices.Models;
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

    /// <summary>
    /// validate the schedule profile against the runtime courses.
    /// </summary>
    /// <param name="profile"></param>
    /// <param name="getRuntimeCourse"></param>
    /// <returns></returns>
    public static (bool isValid, IEnumerable<(string Code, string Group)> InvalidEntries) ValidateRuntimeState(
        this ScheduleProfile profile,
        Func<string, RuntimeCourse?> getRuntimeCourse)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(getRuntimeCourse);
        
        if (profile.SavedCourseGroupKeys is null)
            return (true, []);
        
        var invalidEntries = new List<(string Code, string Group)>();
        foreach (var (code, group) in profile.SavedCourseGroupKeys)
        {
            var runtimeCourse = getRuntimeCourse(code);
            if (runtimeCourse is null 
                || !runtimeCourse.IsSectionRegistered(group)) 
                invalidEntries.Add((code, group));
        }
        
        return (invalidEntries.Count == 0, invalidEntries);
    }
}