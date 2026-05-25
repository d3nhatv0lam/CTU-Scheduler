using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Networking;

namespace CTUScheduler.Infrastructure.Sites.CTU.Abstractions;

public interface IAuthClient
{
    Task<CtuSession> AuthenticateAsync(string username, string password, CancellationToken ct = default);
}