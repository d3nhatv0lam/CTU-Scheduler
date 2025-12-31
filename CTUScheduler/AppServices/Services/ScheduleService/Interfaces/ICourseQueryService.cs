using System;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Models;
using DynamicData;

namespace CTUScheduler.AppServices.Services.ScheduleService.Interfaces;

public interface ICourseQueryService
{
    IObservable<IChangeSet<RuntimeCourse, string>> ConnectCourses();
    Task RefreshCoursesAsync(CancellationToken token = default);
}