using System;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration;
using CTUScheduler.Core.Models.Shared.Results;

namespace CTUScheduler.AppServices.Abstractions;

[Obsolete]
public interface OldIRegistrationRulesService
{
    IObservable<RegistrationInformation> RegistrationInfoChanged { get; }
    
    Task<OperationResult> EnsureReadyAsync();

    Task<RegistrationInformation> FetchRegistrationInfoAsync(
        CancellationToken cancellationToken = default,
        TimeSpan? timeout = null
    );
}