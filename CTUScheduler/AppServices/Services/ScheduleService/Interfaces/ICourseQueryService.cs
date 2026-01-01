using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Models;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using DynamicData;

namespace CTUScheduler.AppServices.Services.ScheduleService.Interfaces;

public interface ICourseQueryService
{
    IObservable<IChangeSet<RuntimeCourse, string>> ConnectCourses();
    IEnumerable<Course> GetCourseSnapshot();
    Task RefreshCoursesAsync(CancellationToken token = default);
}