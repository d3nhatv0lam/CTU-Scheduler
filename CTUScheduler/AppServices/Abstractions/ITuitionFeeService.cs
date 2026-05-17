using System;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration;
using CTUScheduler.Core.Models.Shared.Results;

namespace CTUScheduler.AppServices.Abstractions;

public interface ITuitionFeeService
{
    Task<OperationResult<TuitionFeeSummary>> FetchTuitionFeeAsync(CancellationToken cancellationToken = default,
        TimeSpan? timeout = null);
}