using System;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Models;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using DynamicData;

namespace CTUScheduler.AppServices.Services.RuntimeCourseService;

public interface ICourseStateService
{
    Task RefreshCourseAsync(CancellationToken token = default);
    IObservable<IChangeSet<RuntimeCourse, string>> Connect();
}