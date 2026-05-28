using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared.Results;

namespace CTUScheduler.AppServices.Abstractions;

public interface ITuitionFeeRefactorService
{
    public Task<OperationResult> RefreshTuitionFeeAsync(
        int? academicYear = null,
        int? semester = null,
        CancellationToken cancellationToken = default);
}