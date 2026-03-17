using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;

namespace CTUScheduler.AppServices.Services.ScheduleService;

public interface IScheduleSyncService
{
    Task RefreshCoursesAsync(CancellationToken token = default);
    IObservable<Unit> CoursesRefreshed { get; }
}