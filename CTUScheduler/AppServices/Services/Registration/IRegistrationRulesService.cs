using System;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Shared.Results;

namespace CTUScheduler.AppServices.Services.Registration;

public interface IRegistrationRulesService
{
    IObservable<RegistrationInformation> RegistrationInfoChanges { get; }
    Task<OperationResult> EnsureReadyAsync();

    Task<RegistrationInformation> FetchRegistrationInfoAsync(
        CancellationToken cancellationToken = default,
        TimeSpan? timeout = null
    );
}