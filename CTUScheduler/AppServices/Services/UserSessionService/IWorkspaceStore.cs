using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared.Results;

namespace CTUScheduler.AppServices.Services.UserSessionService;

public interface IWorkspaceStore
{
    Task<OperationResult> SaveAsync(string filePath, CancellationToken ct = default);
    Task<OperationResult> LoadAsync(string filePath, CancellationToken ct = default);
}
