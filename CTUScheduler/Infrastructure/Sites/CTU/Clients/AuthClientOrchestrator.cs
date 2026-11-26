// =========================================================================================
// AuthClientOrchestrator.cs
// Created: 2026-11-16
// Context: Smart authentication orchestrator managing dual-mode routing between SSO and Legacy.
// Reason:  Eliminates network timeout delays by actively probing the server state during ping,
//          routing requests directly to the functional client (SSO WSO2 or Legacy Direct PHP)
// =========================================================================================

using System;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Networking;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Auth;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Sites.CTU.Clients;

internal sealed class AuthClientOrchestrator : IAuthClient
{
    private readonly AuthClient _ssoClient;
    private readonly LegacyAuthClient _legacyClient;
    private readonly ILogger<AuthClientOrchestrator> _logger;

    // Cờ trạng thái định tuyến tức thời, mặc định ưu tiên Legacy trong giai đoạn trường bảo trì
    private volatile bool _isLegacyActive = true;

    public AuthClientOrchestrator(
        AuthClient ssoClient,
        LegacyAuthClient legacyClient,
        ILogger<AuthClientOrchestrator> logger)
    {
        _ssoClient = ssoClient;
        _legacyClient = legacyClient;
        _logger = logger;
    }

    public async Task<AuthServerStatus> PingAuthServerAsync(CancellationToken cancellationToken = default)
    {
        // Kiểm tra cổng SSO trước
        var ssoStatus = await _ssoClient.PingAuthServerAsync(cancellationToken);
        if (ssoStatus == AuthServerStatus.Healthy)
        {
            _isLegacyActive = false;
            return AuthServerStatus.Healthy;
        }

        // Nếu SSO lỗi hoặc bảo trì, kiểm tra cổng Legacy
        _logger.LogInformation("Cổng SSO không khả dụng ({Status}), tiến hành kiểm tra cổng Legacy...", ssoStatus);
        var legacyStatus = await _legacyClient.PingAuthServerAsync(cancellationToken);
        if (legacyStatus == AuthServerStatus.Healthy)
        {
            _isLegacyActive = true;
            return AuthServerStatus.Healthy;
        }

        return ssoStatus == AuthServerStatus.CaptchaBlocked
            ? AuthServerStatus.CaptchaBlocked
            : legacyStatus;
    }

    public Task<CtuSession> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        // Định tuyến thẳng vào client đang hoạt động để loại bỏ độ trễ timeout mạng
        return _isLegacyActive
            ? _legacyClient.AuthenticateAsync(username, password, cancellationToken)
            : _ssoClient.AuthenticateAsync(username, password, cancellationToken);
    }

    public Task<bool> PingSessionAsync(CancellationToken cancellationToken = default)
    {
        // Cả 2 client đều ping chung trang chủ HtqlEndpoints.StudentHome
        return _legacyClient.PingSessionAsync(cancellationToken);
    }

    public async Task<CtuSession?> TrySilentReAuthAsync(
        CtuSession currentSession,
        CancellationToken cancellationToken = default)
    {
        if (!_isLegacyActive)
        {
            return await _ssoClient.TrySilentReAuthAsync(currentSession, cancellationToken);
        }

        return await _legacyClient.TrySilentReAuthAsync(currentSession, cancellationToken);
    }
}
