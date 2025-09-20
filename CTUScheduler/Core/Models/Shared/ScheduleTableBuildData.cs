using System.Collections.Generic;
using System.Linq;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;

namespace CTUScheduler.Core.Models.Shared;

public record ScheduleTableBuildData(IEnumerable<Course> Courses, ScheduleTable ScheduleTable)
{
     public bool HasData()
     {
         // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
         return Courses != null && Courses.Any() // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                && ScheduleTable != null;
     }
     
     /// <summary>
     /// Check Key data match between Courses and ScheduleTable
     /// </summary>
     /// <returns></returns>
     public bool IsValid()
     {
         if (!HasData()) 
             return false;

         var courseKeySet = Courses
             .SelectMany(course => course.Sections.Select(section => (course.Code, section.Group)))
             .ToHashSet();

         var savedKeysSet = ScheduleTable.SavedCourseGroupKeys
             .Select(key => (key.Key, key.Value))
             .ToHashSet();

         return savedKeysSet.IsSubsetOf(courseKeySet);
     }
}