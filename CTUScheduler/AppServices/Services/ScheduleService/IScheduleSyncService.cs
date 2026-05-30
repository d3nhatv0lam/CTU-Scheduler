using System;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared.Results;

namespace CTUScheduler.AppServices.Services.ScheduleService;

public interface IScheduleSyncService
{
    Task<OperationResult> RefreshCoursesAsync(CancellationToken token = default);
    IObservable<bool> IsRefreshing { get; }
}