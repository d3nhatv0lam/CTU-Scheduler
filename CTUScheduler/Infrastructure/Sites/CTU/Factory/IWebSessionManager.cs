using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.DriverCore.Refactor.Page;
using CTUScheduler.Infrastructure.Sites.Base;

namespace CTUScheduler.Infrastructure.Sites.CTU.Factory;

public interface IWebSessionManager
{
    Task<OperationResult> NavigateSafelyAsync(ISitePageRefactor page);
}