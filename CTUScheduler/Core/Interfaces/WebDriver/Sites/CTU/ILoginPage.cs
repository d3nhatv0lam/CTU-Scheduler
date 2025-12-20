using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.WebResponse;

namespace CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;

public interface ILoginPage: ISitePage
{
    public Task LoginAsync(string username, string password, CancellationToken cancellationToken = default);
}