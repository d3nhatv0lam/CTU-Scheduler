// =========================================================================================
// LegacyAuthClient.cs
// Created: 2026-11-16
// Context: Emergency fallback authentication client created during CTU SSO WSO2 maintenance.
// Reason:  CTU temporarily reverted from centralized WSO2 SAML SSO (accounts.ctu.edu.vn)
// Flow:    1. Direct POST to /htql/sinhvien/dang_nhap.php -> receives PHPSESSID & WAF cookies.
//          2. Direct POST to /htql/dkmh/student/dang_nhap.php -> receives JWT access_token directly.
// Note:    Isolated from AuthClient.cs to preserve clean SSO architecture and prevent regressions.
// =========================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Networking;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Auth;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Sites.CTU.Clients;

internal sealed class LegacyAuthClient : IAuthClient
{
    private readonly HttpClient _pingHttpClient;
    private readonly ILogger<LegacyAuthClient> _logger;

    public LegacyAuthClient(HttpClient pingHttpClient, ILogger<LegacyAuthClient> logger)
    {
        _pingHttpClient = pingHttpClient;
        _logger = logger;
    }

    public async Task<AuthServerStatus> PingAuthServerAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Ping CTU Legacy Auth Server ({Url})...", HtqlEndpoints.LegacyLogin);
        try
        {
            using var response = await _pingHttpClient.GetAsync(HtqlEndpoints.LegacyLogin, cancellationToken);
            if ((int)response.StatusCode >= 500)
            {
                _logger.LogWarning("CTU Legacy Server trả về mã lỗi {StatusCode}", response.StatusCode);
                return AuthServerStatus.Maintenance;
            }

            return response.IsSuccessStatusCode ? AuthServerStatus.Healthy : AuthServerStatus.Unreachable;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Lỗi kết nối khi ping máy chủ CTU Legacy.");
            return AuthServerStatus.Unreachable;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Lỗi không xác định khi ping máy chủ CTU Legacy.");
            return AuthServerStatus.Unreachable;
        }
    }

    public async Task<CtuSession> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Bắt đầu xác thực cơ chế cũ (Legacy Direct Form) cho tài khoản {Username}...", username);

        var cookiesContainer = new CookieContainer();
        using var httpHandler = new HttpClientHandler { CookieContainer = cookiesContainer };
        using var httpClient = new HttpClient(httpHandler);

        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        httpClient.DefaultRequestHeaders.Add("Accept-Language", "vi-VN,vi;q=0.9,en-US;q=0.8,en;q=0.7");

        // Gửi thông tin đăng nhập trực tiếp lấy PHPSESSID và cookie WAF
        using var loginRequest = new HttpRequestMessage(HttpMethod.Post, HtqlEndpoints.LegacyLogin);
        loginRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "txtDinhDanh", username },
            { "txtMatKhau", password },
            { "login", string.Empty }
        });

        using var loginResponse = await httpClient.SendAsync(loginRequest, cancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        // Kích hoạt phiên trên phân hệ ĐKMH để nhận cookie access_token (JWT) trực tiếp
        using var jwtRequest = new HttpRequestMessage(HttpMethod.Post, DkmhEndpoints.Login);
        jwtRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "txtTaiKhoan", username },
            { "txtMatKhau", "p" }
        });

        using var jwtResponse = await httpClient.SendAsync(jwtRequest, cancellationToken);
        jwtResponse.EnsureSuccessStatusCode();

        // Trích xuất access_token (JWT)
        var targetDomains = new[]
        {
            DkmhEndpoints.BaseDomain,
            HtqlEndpoints.BaseDomain
        };

        string dkmhJwtToken = targetDomains
            .SelectMany(domain => cookiesContainer.GetCookies(domain))
            .FirstOrDefault(c => c.Name.Equals("access_token", StringComparison.OrdinalIgnoreCase))
            ?.Value ?? string.Empty;

        if (string.IsNullOrEmpty(dkmhJwtToken))
        {
            throw new InvalidOperationException("Không tìm thấy Cookie 'access_token' trong CookieContainer sau khi đăng nhập cơ chế cũ.");
        }

        // Tái sử dụng logic trích xuất legacy cookies và parse JWT từ AuthClient
        var (legacyCookies, cookiesExpiresAt) = AuthClient.ExtractLegacyCookies(cookiesContainer, targetDomains);
        var (studentProfile, dkmhJwtExpiresAt) = AuthClient.ParseJwt(dkmhJwtToken);

        _logger.LogInformation("Xác thực Legacy thành công cho sinh viên {StudentName} ({StudentId})!", studentProfile.Name, studentProfile.Mssv);

        return new CtuSession(
            Dkmh: new DkmhSession(dkmhJwtToken, dkmhJwtExpiresAt, studentProfile),
            Htql: new HtqlSession(legacyCookies, cookiesExpiresAt)
        );
    }

    public async Task<bool> PingSessionAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _pingHttpClient.GetAsync(HtqlEndpoints.StudentHome, cancellationToken);
        if (!response.IsSuccessStatusCode) return false;

        var htmlContent = await response.Content.ReadAsStringAsync(cancellationToken);
        return !htmlContent.Contains("location.href='../logout.php'");
    }

    public Task<CtuSession?> TrySilentReAuthAsync(CtuSession currentSession, CancellationToken cancellationToken = default)
    {
        // Cơ chế cũ không có cookie trung gian SSO (SESSISID) để xin lại session bằng mật khẩu rỗng.
        // Trả về null để Coordinator xử lý Graceful Degradation (vô hiệu hóa HTQL nhưng vẫn giữ JWT).
        return Task.FromResult<CtuSession?>(null);
    }
}
