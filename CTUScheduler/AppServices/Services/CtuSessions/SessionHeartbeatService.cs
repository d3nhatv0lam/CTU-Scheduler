using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.CtuSessions;

public class SessionHeartbeatService : ISessionHeartbeatService, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private IDisposable? _heartbeatSubscription;
    private readonly TimeSpan _heartbeatInterval = TimeSpan.FromMinutes(10);
    private readonly Subject<Unit> _sessionExpiredSubject = new();

    private readonly IConnectivityService _connectivityService;
    private readonly ICtuSessionStore _sessionStore;
    private readonly IAuthClient _authClient;
    private readonly Lazy<ISessionCoordinator> _sessionCoordinatorLazy;
    private readonly ILogger<SessionHeartbeatService> _logger;

    private readonly Lock _gate = new();
    private bool _isStarted;

    public IObservable<Unit> SessionExpired => _sessionExpiredSubject;

    public SessionHeartbeatService(
        ICtuSessionStore sessionStore,
        IAuthClient authClient,
        IConnectivityService connectivityService,
        Lazy<ISessionCoordinator> sessionCoordinatorLazy,
        ILogger<SessionHeartbeatService> logger)
    {
        _sessionStore = sessionStore;
        _authClient = authClient;
        _connectivityService = connectivityService;
        _sessionCoordinatorLazy = sessionCoordinatorLazy;
        _logger = logger;

        _sessionExpiredSubject.DisposeWith(_disposables);
    }

    public void Start()
    {
        lock (_gate)
        {
            if (_isStarted) return;

            _heartbeatSubscription = Observable.Interval(_heartbeatInterval, scheduler: TaskPoolScheduler.Default)
                .WithLatestFrom(_connectivityService.IsInternetAvailable, (_, isAvailable) => isAvailable)
                .Where(isAvailable => isAvailable)
                .Select(_ => Observable.FromAsync(async ct =>
                {
                    try
                    {
                        await ProcessHeartbeatTickAsync(ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi không xác định phát sinh trong một tick của Heartbeat.");
                    }
                    return Unit.Default;
                }))
                .Concat()
                .Subscribe(
                    _ => {},
                    ex => _logger.LogCritical(ex, "Chuỗi đăng ký Heartbeat bị lỗi chí mạng và đã kết thúc vĩnh viễn.")
                );

            _isStarted = true;
            _logger.LogInformation("Tiến trình Heartbeat ngầm đã khởi động.");
        }
    }

    private async Task ProcessHeartbeatTickAsync(CancellationToken ct)
    {
        var currentSession = _sessionStore.Current;
        if (currentSession == null)
        {
            _logger.LogInformation("Không tìm thấy phiên làm việc hoạt động. Tự động dừng Heartbeat ngầm.");
            Stop();
            return;
        }

        // Kiểm tra hết hạn cứng lý thuyết -> không cứu
        if (currentSession.IsExpired)
        {
            _logger.LogWarning("Phiên SSO hoặc JWT của {StudentId} đã hết hạn hoàn toàn lý thuyết. Tiến hành đăng xuất...", currentSession.StudentId);
            HandleSessionExpired();
            return;
        }

        // Ping kiểm tra thực tế sức khỏe của PHP Session
        bool isPhpSessionAlive = false;
        bool connectionFailed = false;
        try
        {
            isPhpSessionAlive = await _authClient.PingSessionAsync(ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Gửi yêu cầu Ping bị quá thời gian (Timeout). Bảo lưu phiên.");
            connectionFailed = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gửi yêu cầu Ping bị lỗi kết nối mạng. Bảo lưu phiên.");
            connectionFailed = true;
        }

        // Nếu mất kết nối mạng tạm thời, không phán quyết session chết
        if (connectionFailed) return;

        // Nếu PHP Session bị hủy trên server -> Gọi cứu phiên qua Lock trung tâm của Coordinator
        if (!isPhpSessionAlive)
        {
            _logger.LogWarning("PHP Session của {StudentId} không còn hoạt động trên server. Gọi khôi phục ngầm qua Lock trung tâm...", currentSession.StudentId);
            try
            {
                var refreshed = await _sessionCoordinatorLazy.Value.RefreshSessionAsync(currentSession, ct);
                
                if (refreshed is null)
                {
                    _logger.LogError("Cứu phiên HTQL ngầm thất bại. Dừng heartbeat...");
                    Stop();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Tiến trình cứu phiên ngầm bị hủy do timeout mạng. Bảo lưu phiên.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng khi cố gắng tự động cứu phiên ngầm. Vô hiệu hóa phân hệ HtqlSession...");
                if (currentSession.Htql is not null)
                {
                    _sessionCoordinatorLazy.Value.InvalidateHtqlSession(currentSession.Htql);
                }
                Stop();
            }
        }
        else
        {
            _logger.LogDebug("Heartbeat thành công, Session vẫn còn sống!");
        }
    }

    private void HandleSessionExpired()
    {
        _sessionExpiredSubject.OnNext(Unit.Default);
        Stop();
    }

    public void Stop()
    {
        lock (_gate)
        {
            if (!_isStarted) return;
            
            _heartbeatSubscription?.Dispose();
            _heartbeatSubscription = null;
            _isStarted = false;
            _logger.LogInformation("Tiến trình Heartbeat ngầm đã dừng.");
        }
    }

    public void Dispose()
    {
        Stop();
        _disposables.Dispose();
    }
}