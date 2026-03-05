using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Infrastructure.Sites.Base;

namespace CTUScheduler.Infrastructure.Sites.CTU.Abstractions;

public interface ILoginPage: ISitePage
{
    public Task LoginAsync(string username, string password, CancellationToken cancellationToken = default);
}