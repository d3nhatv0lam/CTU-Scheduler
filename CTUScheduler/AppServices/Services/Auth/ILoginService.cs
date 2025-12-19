using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.WebResponse;

namespace CTUScheduler.AppServices.Services.Auth;

public interface ILoginService
{
    /// <summary>
    ///  Navigate to login page
    /// </summary>
    /// <param name="retryCount">-1 if INF</param>
    /// <returns></returns>
    Task<OperationResult> NavigateToAsync(int retryCount = -1);
    Task<OperationResult> LoginAsync(string username, string password);
}