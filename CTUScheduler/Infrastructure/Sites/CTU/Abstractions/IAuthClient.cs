using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Networking;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Auth;

namespace CTUScheduler.Infrastructure.Sites.CTU.Abstractions;

public interface IAuthClient
{
    /// <summary>
    /// Ping the server to check if it is reachable.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>
    /// true if the server is reachable, false otherwise (reason: your internet is captcha detected, or the server is down).
    /// </returns>
    Task<AuthServerStatus> PingAuthServerAsync(CancellationToken cancellationToken = default);
    
    Task<CtuSession> AuthenticateAsync(string username, string password, CancellationToken ct = default);
    
    /// <summary>
    /// Gửi một request siêu nhẹ (HEAD/GET) để giữ nhịp (heartbeat), 
    /// tránh cho phiên làm việc PHP (PHPSESSID) bị hết hạn do không hoạt động.
    /// </summary>
    Task<bool> PingSessionAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Thực hiện bắt tay SSO trung tâm ngầm định bằng Cookie SESSISID để làm mới phiên PHP (PHPSESSID) mà không cần mật khẩu.
    /// </summary>
    Task<CtuSession?> TrySilentReAuthAsync(CtuSession currentSession, CancellationToken ct = default);
}