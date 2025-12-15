using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.WebResponse;

namespace CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;

public interface ILoginPage: ISitePage
{
    public Task<LoginResult> TryLoginAsync(string username, string password, CancellationToken cancellationToken = default);
}