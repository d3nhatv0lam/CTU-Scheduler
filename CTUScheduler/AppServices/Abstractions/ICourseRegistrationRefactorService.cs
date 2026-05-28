using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared.Results;

namespace CTUScheduler.AppServices.Abstractions;

public interface ICourseRegistrationRefactorService
{
    Task<OperationResult> RefreshPlannedCourseAsync(CancellationToken cancellationToken = default);
}