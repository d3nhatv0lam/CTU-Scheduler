using System.Threading;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Services.ScheduleService.Interfaces;

public interface IScheduleSyncService
{
    Task RefreshCoursesAsync(CancellationToken token = default);
}