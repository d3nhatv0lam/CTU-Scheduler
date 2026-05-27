using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.CtuSessions;

public class SessionHeartbeatService : ISessionHeartbeatService, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private IDisposable? _heartbeatSubscription;
    private readonly TimeSpan _heartbeatInterval = TimeSpan.FromMinutes(10);
    private readonly BehaviorSubject<bool> _isSessionExpiredSubject = new(false);

    private readonly IConnectivityService _connectivityService;
    private readonly ICtuSessionStore _sessionStore;
    private readonly IAuthClient _authClient;
    private readonly ILogger<SessionHeartbeatService> _logger;

    private readonly Lock _gate = new();
    private bool _isStarted;

    public SessionHeartbeatService(
        ICtuSessionStore sessionStore,
        IAuthClient authClient,
        IConnectivityService connectivityService,
        ILogger<SessionHeartbeatService> logger)
    {
        _sessionStore = sessionStore;
        _authClient = authClient;
        _connectivityService = connectivityService;
        _logger = logger;

        _isSessionExpiredSubject.DisposeWith(_disposables);
    }

    public void Start()
    {
        lock (_gate)
        {
            if (_isStarted) return;
            _isSessionExpiredSubject.OnNext(false);

            _heartbeatSubscription = Observable.Interval(_heartbeatInterval, scheduler: TaskPoolScheduler.Default)
                .WithLatestFrom(_connectivityService.IsInternetAvailable, (_, isAvailable) => isAvailable)
                .Where(isAvailable => isAvailable)
                .Select(_ => _sessionStore.Current)
                .Where(session => session is not null && !session.IsExpired)
                .Select(session => Observable.FromAsync(async ct =>
                {
                    try
                    {
                        bool isAlive = await _authClient.PingSessionAsync(ct);
                        return (Session: session, IsAlive: isAlive);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi xảy ra khi gửi yêu cầu giữ nhịp (Ping) phiên làm việc.");
                        return (Session: session, IsAlive: true); // ví dụ còn sống khi lỗi mạng tạm thời
                    }
                }))
                .Concat()
                .Where(x => !x.IsAlive)
                .Select(state =>
                    Observable.FromAsync(async ct =>
                    {
                        _logger.LogWarning(
                            "Phiên của sinh viên {StudentId} đã hết hạn. Đang thực hiện tự động cứu phiên ngầm...",
                            state.Session!.StudentId);

                        try
                        {
                            var refreshedSession =
                                await _authClient.TrySilentReAuthAsync(state.Session, ct);

                            if (refreshedSession != null)
                            {
                                _logger.LogInformation(
                                    "Tự động cứu phiên ngầm thành công cho {StudentId}",
                                    refreshedSession.StudentId);

                                _sessionStore.Update(refreshedSession);
                            }
                            else
                            {
                                _logger.LogError(
                                    "Tự động cứu phiên ngầm thất bại cho {StudentId}",
                                    state.Session.StudentId);

                                _sessionStore.Clear();
                                _isSessionExpiredSubject.OnNext(true);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "Lỗi nghiêm trọng trong quá trình tự động cứu phiên ngầm.");

                            _sessionStore.Clear();
                            _isSessionExpiredSubject.OnNext(true);
                        }

                        return Unit.Default;
                    }))
                .Concat()
                .Subscribe();

            _isStarted = true;
        }
    }

    public void Stop()
    {
        _heartbeatSubscription?.Dispose();
        _heartbeatSubscription = null;
        _isStarted = false;
    }

    public void Dispose()
    {
        Stop();
        _disposables.Dispose();
    }
}