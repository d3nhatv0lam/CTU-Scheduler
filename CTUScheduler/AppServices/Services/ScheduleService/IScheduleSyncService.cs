using System;
using System.Threading;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Services.ScheduleService;

public interface IScheduleSyncService
{
    Task RefreshCoursesAsync(CancellationToken token = default);
    IObservable<bool> IsRefreshing { get; }
}