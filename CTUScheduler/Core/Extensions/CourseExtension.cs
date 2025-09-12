using System.Collections.Generic;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;

namespace CTUScheduler.Core.Extensions;

public static class CourseExtension
{
    public static Course CloneWithNewCourseSections(this Course course, IEnumerable<CourseSection> sections)
    {
        return new Course
        {
            Code = course.Code,
            Name_VN = course.Name_VN,
            Credit = course.Credit,
            TheorySessions = course.TheorySessions,
            PracticalSessions = course.PracticalSessions,
            Sections = new List<CourseSection>(sections)
        };
    }
}