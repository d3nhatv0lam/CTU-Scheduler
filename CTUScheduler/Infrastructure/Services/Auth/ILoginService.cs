using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared.Results;

namespace CTUScheduler.Infrastructure.Services.Auth;

public interface ILoginService
{
    Task<OperationResult> EnsureReadyAsync();
    Task<OperationResult> LoginAsync(string username, string password);
}