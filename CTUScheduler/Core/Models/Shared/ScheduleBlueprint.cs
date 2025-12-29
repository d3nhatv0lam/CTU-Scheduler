using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;

namespace CTUScheduler.Core.Models.Shared;

public record ScheduleBlueprint(IReadOnlyList<Course> Courses, ScheduleProfile Metadata)
{
    public IReadOnlyList<Course> Courses { get; init; } = Courses ?? throw new ArgumentNullException(nameof(Courses));
    public ScheduleProfile Metadata { get; init; } = Metadata ?? throw new ArgumentNullException(nameof(Metadata));
    public bool IsEmpty => !Courses.Any();

     public bool IsConsistent
     {
         get
         {
             if (IsEmpty) return false;

             var availableKeys = Courses
                 .SelectMany(c => c.Sections.Select(s => (c.Code, s.Group)))
                 .ToHashSet();

             return Metadata.SavedCourseGroupKeys
                 .All(kvp => availableKeys.Contains((kvp.Key, kvp.Value)));
         }
     }
     
     /// <summary>
     ///  Remove all course sections that are not in the saved course group keys.
     /// </summary>
     /// <param name="result">ScheduleBlueprint trimmed when valid</param>
     /// <returns></returns>
     public bool TryTrim([NotNullWhen(true)] out ScheduleBlueprint? result)
     {
         if (IsEmpty)
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
         result = this with { Courses = trimmedCourses };
         return true;
     }
}