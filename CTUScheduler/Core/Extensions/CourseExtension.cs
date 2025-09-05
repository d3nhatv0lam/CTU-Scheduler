using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Extensions
{
    public static class CourseExtension
    {
        public static Course CloneWithoutCourseDatas(this Course course)
        {
            return new Course
            {
                Code = course.Code,
                Name_VN = course.Name_VN,
                Credit = course.Credit,
                TheorySessions = course.TheorySessions,
                PracticalSessions = course.PracticalSessions
            };
        }

        public static Course CloneWithNewCourseDatas(this Course course, IEnumerable<CourseSection> newCourseDatas)
        {
            return new Course
            {
                Code = course.Code,
                Name_VN = course.Name_VN,
                Credit = course.Credit,
                TheorySessions = course.TheorySessions,
                PracticalSessions = course.PracticalSessions,
                Sections = new List<CourseSection>(newCourseDatas)
            };
        }
    }
}
