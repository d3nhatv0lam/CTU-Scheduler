using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared.Results;

namespace CTUScheduler.AppServices.Abstractions;

public interface IRegistrationRulesService
{
    Task<OperationResult> RefreshRegistrationAsync(CancellationToken cancellationToken = default);
}