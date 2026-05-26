using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.CtuSessions;

public class SessionCoordinator : ISessionCoordinator
{
    private readonly IAuthClient _authClient;
    private readonly ICtuSessionStore _sessionStore;
    private readonly ISessionHeartbeatService _heartbeatService;
    private readonly IEnumerable<ICleanup> _cleanupServices;
    private readonly IEnumerable<ICleanupAsync> _asyncCleanupServices;
    private readonly ILogger<SessionCoordinator> _logger;

    public SessionCoordinator(
        IAuthClient authClient,
        ICtuSessionStore sessionStore,
        ISessionHeartbeatService heartbeatService,
        IEnumerable<ICleanup> cleanupServices,
        IEnumerable<ICleanupAsync> asyncCleanupServices,
        ILogger<SessionCoordinator> logger)
    {
        _authClient = authClient;
        _sessionStore = sessionStore;
        _heartbeatService = heartbeatService;
        _cleanupServices = cleanupServices;
        _asyncCleanupServices = asyncCleanupServices;
        _logger = logger;
    }

    public async Task<OperationResult> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return OperationResult.Failed("Tên đăng nhập và mật khẩu không được để trống", "Auth.Validation",
                kind: OperationFailureReason.Validation);
        try
        {
            var session = await _authClient.AuthenticateAsync(username, password, ct);
            _sessionStore.Update(session);
            _heartbeatService.Start();
            _logger.LogInformation("Logged in successfully!");
            return OperationResult.Success();
        }
        catch (InvalidCredentialsException ex)
        {
            return OperationResult.Failed(ex.Message, "Auth.Validation", kind: OperationFailureReason.Validation);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to login");
            return OperationResult.Failed(ex.Message, "Auth.HandshakeFailed",
                kind: OperationFailureReason.Unauthorized);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to login");
            return OperationResult.Failed("Kết nối tới máy chủ CTU thất bại. Vui lòng kiểm tra lại mạng hoặc VPN.",
                "Auth.NetworkError", kind: OperationFailureReason.Network);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Login operation timed out (HttpClient Timeout).");
            return OperationResult.Failed(
                "Quá thời gian kết nối tới máy chủ trường (Timeout). Vui lòng kiểm tra lại VPN hoặc mạng.",
                "Auth.Timeout",
                kind: OperationFailureReason.Network
            );
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Login operation was canceled by the user.");
            return OperationResult.Failed(
                "Yêu cầu đăng nhập đã bị hủy bỏ.",
                "Auth.Canceled", kind: OperationFailureReason.UserAction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login");
            return OperationResult.FromException(ex, "Lỗi đăng nhập không xác dịnh", "Auth.Unexpected",
                OperationFailureReason.System);
        }
    }

    public async Task LogoutAsync()
    {
        foreach (var cleanup in _cleanupServices)
        {
            cleanup.Cleanup();
        }

        foreach (var asyncCleanup in _asyncCleanupServices)
        {
            await asyncCleanup.CleanupAsync();
        }

        _logger.LogInformation("Logged out! Session data cleared.");
    }
}