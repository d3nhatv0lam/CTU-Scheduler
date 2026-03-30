using System;
using System.Collections.Generic;
using CTUScheduler.AppServices.Models;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using DynamicData;

namespace CTUScheduler.AppServices.Services.ScheduleService;

public interface ICourseQueryService
{
    IObservable<IChangeSet<RuntimeCourse, string>> ConnectCourses();
    IEnumerable<Course> GetCoursesSnapshot();
    Course? GetCourseSnapshot(string courseCode);
}