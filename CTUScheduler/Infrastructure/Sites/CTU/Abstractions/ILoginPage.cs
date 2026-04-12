using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.Sites.Base;

namespace CTUScheduler.Infrastructure.Sites.CTU.Abstractions;

public interface ILoginPage: ISitePageRefactor
{
    Task FillCredentialsAsync(string username, string password);
    Task SubmitAsync();
    Task<bool> HasErrorVisibleAsync();
    Task<string> GetErrorMessageAsync();
    bool IsLoginSuccess();
    
    Task<OperationResult> PerformLoginActionAsync(string username, string password);
}