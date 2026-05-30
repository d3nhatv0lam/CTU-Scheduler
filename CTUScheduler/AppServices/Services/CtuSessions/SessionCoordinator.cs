using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Core.Networking;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.CtuSessions;

public class SessionCoordinator : ISessionCoordinator
{
    private readonly IAuthClient _authClient;
    private readonly ICtuSessionStore _sessionStore;
    private readonly ISessionHeartbeatService _heartbeatService;
    private readonly Lazy<IEnumerable<ICleanup>> _cleanupServices;
    private readonly Lazy<IEnumerable<ICleanupAsync>> _asyncCleanupServices;
    private readonly ILogger<SessionCoordinator> _logger;

    private readonly SemaphoreSlim _reAuthLock = new(1, 1);
    private readonly SemaphoreSlim _logoutSemaphore = new SemaphoreSlim(1, 1);
    private readonly Subject<Unit> _sessionExpiredSubject = new();

    public IObservable<Unit> SessionExpired => _sessionExpiredSubject;

    public SessionCoordinator(
        IAuthClient authClient,
        ICtuSessionStore sessionStore,
        ISessionHeartbeatService heartbeatService,
        Lazy<IEnumerable<ICleanup>> cleanupServices,
        Lazy<IEnumerable<ICleanupAsync>> asyncCleanupServices,
        ILogger<SessionCoordinator> logger)
    {
        _authClient = authClient;
        _sessionStore = sessionStore;
        _heartbeatService = heartbeatService;
        _cleanupServices = cleanupServices;
        _asyncCleanupServices = asyncCleanupServices;
        _logger = logger;

        _heartbeatService.SessionExpired.Subscribe(async _ =>
        {
            await _logoutSemaphore.WaitAsync();
            try
            {
                _logger.LogWarning("Nhận được tín hiệu hết hạn phiên từ Heartbeat ngầm. Tiến hành dọn dẹp hệ thống...");
                await EndSessionAsync();
            }
            finally
            {
                _logoutSemaphore.Release();
            }
        });
    }

    public async Task<OperationResult> StartSessionAsync(string username, string password,
        CancellationToken ct = default)
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
            return OperationResult.FromException(ex, "Lỗi đăng nhập không xác định", "Auth.Unexpected",
                OperationFailureReason.System);
        }
    }

    public async Task<CtuSession?> RefreshSessionAsync(CtuSession expiredSession, CancellationToken ct)
    {
        var current = _sessionStore.Current;

        // Tránh chạy lại nếu session đã được xoá (đã logout)
        if (current is null) return null;

        await _reAuthLock.WaitAsync(ct);
        try
        {
            // lấy current lại vì đa luồng có thể đã thay đổi ở luồng khác
            current = _sessionStore.Current;
            if (current is not null && current != expiredSession && !current.IsExpired)
            {
                _logger.LogInformation(
                    "Phiên làm việc đã được khôi phục bởi một tác vụ khác trước đó. Sử dụng phiên mới.");
                return current;
            }

            //  chưa được refresh, tiến hành Re-Auth
            _logger.LogWarning("Bắt đầu tiến trình khôi phục phiên ngầm tập trung...");
            var refreshedSession = await _authClient.TrySilentReAuthAsync(expiredSession, ct);

            if (refreshedSession is not null)
            {
                _logger.LogInformation("Khôi phục phiên ngầm tập trung thành công!");
                _sessionStore.Update(refreshedSession);
                return refreshedSession;
            }
            else
            {
                _logger.LogError("Yêu cầu Re-Auth bị server từ chối. Đăng xuất...");
                await EndSessionAsync();
                return null;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Tiến trình khôi phục phiên ngầm bị hủy do timeout mạng.");
            throw; // Ném ra để luồng gọi tự xử lý bảo lưu
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không xác định trong tiến trình khôi phục phiên ngầm tập trung.");
            throw;
        }
        finally
        {
            _reAuthLock.Release();
        }
    }

    public async Task EndSessionAsync()
    {
        _heartbeatService.Stop();

        foreach (var cleanup in _cleanupServices.Value)
        {
            try
            {
                cleanup.Cleanup();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup");
            }
        }

        foreach (var asyncCleanup in _asyncCleanupServices.Value)
        {
            try
            {
                await asyncCleanup.CleanupAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanupAsync");
            }
        }

        _sessionExpiredSubject.OnNext(Unit.Default);
        _logger.LogInformation("Logged out! Session data cleared.");
    }
}