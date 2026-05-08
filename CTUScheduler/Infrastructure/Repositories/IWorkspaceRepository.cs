using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Core.Models.Shared.Results;

namespace CTUScheduler.Infrastructure.Repositories;

public interface IWorkspaceRepository
{
    Task<OperationResult> SaveAsync(WorkspaceSnapshot snapshot, string filePath, CancellationToken ct = default);
    Task<OperationResult<WorkspaceSnapshot>> LoadAsync(string filePath, CancellationToken ct = default);
}
