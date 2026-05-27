using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Services.CtuSessions;
using CTUScheduler.Core.Networking;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Services.Network;

public class CtuSessionRecoveryHandler : DelegatingHandler
{
    private readonly ICtuSessionStore _sessionStore;
    private readonly ISessionCoordinator _sessionCoordinator;
    private readonly ILogger<CtuSessionRecoveryHandler> _logger;

    public CtuSessionRecoveryHandler(
        ICtuSessionStore sessionStore,
        ISessionCoordinator sessionCoordinator,
        ILogger<CtuSessionRecoveryHandler> logger)
    {
        _sessionStore = sessionStore;
        _sessionCoordinator = sessionCoordinator;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // 1. Kiểm tra xem đây có phải là một request đang được retry lần thứ nhất hay không
        bool isRetryAttempt = request.Headers.Contains("X-Ctu-Retry-Attempt");

        var response = await base.SendAsync(request, cancellationToken);

        // 2. Phát hiện session chết thực tế (JWT 401 hoặc Legacy HTML Redirect của PHP Web cũ)
        if (await IsSessionExpiredResponseAsync(response, cancellationToken))
        {
            // 3. Chống vòng lặp vô hạn retry nếu request retry vẫn bị báo hết hạn
            if (isRetryAttempt)
            {
                _logger.LogError("Yêu cầu gửi lại (Retry) vẫn tiếp tục nhận lỗi xác thực từ máy chủ CTU. Chặn đứng để tránh vòng lặp vô hạn.");
                await _sessionCoordinator.EndSessionAsync();
                return response;
            }

            var session = _sessionStore.Current;

            // 4. Nếu đã hết hạn cứng lý thuyết (12 tiếng) -> Tuyệt đối không cố cứu
            if (session == null || session.IsExpired)
            {
                _logger.LogWarning("Phát hiện phiên làm việc bị từ chối nhưng phiên đã hết hạn cứng lý thuyết. Tiến hành đăng xuất cưỡng bức...");
                await _sessionCoordinator.EndSessionAsync();
                return response;
            }

            _logger.LogWarning("Phát hiện phiên làm việc bị từ chối trên máy chủ! Gọi khôi phục ngầm qua Lock tập trung...");

            CtuSession? refreshedSession = null;
            try
            {
                // Gọi qua lock tập trung duy nhất của SessionCoordinator
                refreshedSession = await _sessionCoordinator.RefreshSessionAsync(session, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi mạng hoặc sự cố chập chờn khi khôi phục phiên ngầm qua lock tập trung.");
                // Trả về response lỗi cũ thay vì logout, để request sau tự phục hồi khi có mạng ổn định
            }

            // 5. Cứu phiên thành công -> Thực hiện Retry lần duy nhất
            if (refreshedSession != null)
            {
                _logger.LogInformation("Khôi phục phiên ngầm tập trung thành công! Tiến hành tạo bản sao và gửi lại request...");

                // Tạo request mới kiên cố
                var clonedRequest = await CloneHttpRequestMessageAsync(request);
                
                // Đánh dấu đây là request retry lần 1 để chặn vòng lặp vô hạn
                clonedRequest.Headers.TryAddWithoutValidation("X-Ctu-Retry-Attempt", "1");
                
                // Giải phóng response lỗi cũ và gửi lại (Handler con Jwt/LegacyCookie bên dưới sẽ tự động inject Cookie mới vào clonedRequest)
                response.Dispose();
                return await base.SendAsync(clonedRequest, cancellationToken);
            }
            else
            {
                // SSO từ chối bắt tay thực tế -> Đăng xuất
                _logger.LogError("Server SSO từ chối bắt tay. Không thể cứu phiên ngầm. Đăng xuất...");
                await _sessionCoordinator.EndSessionAsync();
            }
        }

        return response;
    }

    private async Task<bool> IsSessionExpiredResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        // Phân hệ API mới (JWT): Trả về 401 Unauthorized hoặc 403 Forbidden
        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
        {
            return true;
        }

        // Phân hệ Web cũ (PHP): Trả về 200 OK kèm HTML chứa script chuyển hướng logout
        if (response.IsSuccessStatusCode && response.Content != null)
        {
            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (mediaType != null && mediaType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
            {
                var html = await response.Content.ReadAsStringAsync(ct);
                if (html.Contains("location.href='../logout.php'") || html.Contains("../dang_nhap.php"))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage req)
    {
        var clone = new HttpRequestMessage(req.Method, req.RequestUri);

        // 1. Sao chép Headers
        foreach (var header in req.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        foreach (var property in req.Options)
            clone.Options.Set(new HttpRequestOptionsKey<object?>(property.Key), property.Value);

        // 2. Sao chép Properties
        clone.Version = req.Version;

        // 3. Sao chép Content (Body) và buffer lại stream an toàn
        if (req.Content != null)
        {
            var ms = new MemoryStream();
            await req.Content.CopyToAsync(ms);
            ms.Position = 0;
            
            clone.Content = new StreamContent(ms);
            
            // Sao chép content headers
            foreach (var header in req.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}
