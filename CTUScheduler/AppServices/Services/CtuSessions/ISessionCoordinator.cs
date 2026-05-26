using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared.Results;

namespace CTUScheduler.AppServices.Services.CtuSessions;

public interface ISessionCoordinator
{
    Task<OperationResult> LoginAsync(string username, string password, CancellationToken ct = default);

    Task LogoutAsync();
}