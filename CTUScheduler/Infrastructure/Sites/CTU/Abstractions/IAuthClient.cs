using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Networking;

namespace CTUScheduler.Infrastructure.Sites.CTU.Abstractions;

public interface IAuthClient
{
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