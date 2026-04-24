using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Core.Models.Shared.Results;

namespace CTUScheduler.Infrastructure.Repositories;

public interface IUserPreferencesRepository
{
    Task<OperationResult> SaveAsync(UserPreferences preferences, CancellationToken cancellationToken = default);
    Task<OperationResult<UserPreferences>> LoadAsync(CancellationToken cancellationToken = default);
}