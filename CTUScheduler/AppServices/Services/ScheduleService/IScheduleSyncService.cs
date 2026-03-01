using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Services.ScheduleService;

public interface IScheduleSyncService
{
    Task RefreshCoursesAsync(CancellationToken token = default);
    IObservable<Unit> CoursesRefreshed { get; }
}