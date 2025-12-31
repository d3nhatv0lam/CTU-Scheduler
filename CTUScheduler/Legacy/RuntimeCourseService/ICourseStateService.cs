using System;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Models;
using DynamicData;

namespace CTUScheduler.Legacy.RuntimeCourseService;

/// <summary>
/// Expose to UI
/// </summary>
public interface ICourseStateService
{
    Task RefreshCourseAsync(CancellationToken token = default);
    IObservable<IChangeSet<RuntimeCourse, string>> Connect();
}