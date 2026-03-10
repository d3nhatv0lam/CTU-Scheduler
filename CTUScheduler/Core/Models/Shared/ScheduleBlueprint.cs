using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;

namespace CTUScheduler.Core.Models.Shared;

public record ScheduleBlueprint
{
    public IReadOnlyList<Course> Courses { get; private set; }
    public ScheduleProfile Metadata { get; private set; }
    private readonly Lazy<bool> _isConsistent;
    public bool IsConsistent => _isConsistent.Value;
    
    private ScheduleBlueprint(IReadOnlyList<Course> courses, ScheduleProfile metadata, bool preCalculatedConsistency)
    {
        Courses = courses;
        Metadata = metadata;
        _isConsistent = new(() => preCalculatedConsistency);
    }
    public ScheduleBlueprint(IReadOnlyList<Course> courses, ScheduleProfile metadata)
    {
        Courses = courses ?? throw new ArgumentNullException(nameof(courses));
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));

        _isConsistent = new(() => CheckConsistency(courses, metadata));
    }

    public void Deconstruct(out IReadOnlyList<Course> courses, out ScheduleProfile metadata)
    {
        courses = Courses;
        metadata = Metadata;
    }

    /// <summary>
    ///  Remove all course sections that are not in the saved course group keys.
    /// </summary>
    /// <param name="result">ScheduleBlueprint trimmed when valid</param>
    /// <returns></returns>
    public bool TryTrim([NotNullWhen(true)] out ScheduleBlueprint? result)
    {
        if (!IsConsistent)
        {
            result = null;
            return false;
        }

        var allowedKeys = Metadata.SavedCourseGroupKeys
            .Select(kvp => (Code: kvp.Key, Group: kvp.Value))
            .ToHashSet();

        var trimmedCourses = new List<Course>(Courses.Count);

        foreach (var course in Courses)
        {
            var validSections = new List<CourseSection>();

            foreach (var section in course.Sections)
            {
                if (allowedKeys.Contains((course.Code, section.Group)))
                    validSections.Add(section);
            }

            if (validSections.Count > 0)
            {
                var newCourse = course.WithSections(validSections);
                trimmedCourses.Add(newCourse);
            }
        }
        
        result = new ScheduleBlueprint(trimmedCourses, this.Metadata, preCalculatedConsistency: true);
        return true;
    }

    private static bool CheckConsistency(IReadOnlyList<Course> courses, ScheduleProfile metadata)
    {
        if (courses.Count == 0) return false;

        var availableKeys = courses
            .SelectMany(c => c.Sections.Select(s => (c.Code, s.Group)))
            .ToHashSet();

        return metadata.SavedCourseGroupKeys
            .All(kvp => availableKeys.Contains((kvp.Key, kvp.Value)));
    }

    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine($"ScheduleBlueprint | Consistency Status: {(IsConsistent ? "Consistent" : "Inconsistent")}");
    
    
        var shortId = Metadata.Id.ToString()[..8]; 
        sb.AppendLine($"[Profile] Name: {Metadata.Name} (ID: {shortId}...) | Last Updated: {Metadata.LastUpdated:MM/dd/yyyy HH:mm}");
        sb.AppendLine($"[Profile] Saved course keys count: {Metadata.SavedCourseGroupKeys.Count}");

   
        sb.AppendLine($"[Courses] Total courses available: {Courses.Count}");
        foreach (var course in Courses)
        {
            var groupNames = course.Sections.Count > 0 
                ? string.Join(", ", course.Sections.Select(s => s.Group)) 
                : "Empty (No sections)";

            sb.AppendLine($"  - [{course.Code}] {course.Name_VN} ({course.Credits} Credits) | Current Groups: {groupNames}");
        }
        
        return sb.ToString().TrimEnd();
    }
}