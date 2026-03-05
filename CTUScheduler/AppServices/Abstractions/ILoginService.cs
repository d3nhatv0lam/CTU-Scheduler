using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared.Results;

namespace CTUScheduler.AppServices.Abstractions;

public interface ILoginService
{
    Task<OperationResult> EnsureReadyAsync();
    Task<OperationResult> LoginAsync(string username, string password);
}