using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.WebResponse;

namespace CTUScheduler.AppServices.Services.Auth;

public interface ILoginService
{
    /// <summary>
    ///  Navigate to login page
    /// </summary>
    /// <returns></returns>
    Task<OperationResult> NavigateToAsync();
    Task<OperationResult> LoginAsync(string username, string password);
}