using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.CtuSessions;

public class SessionHeartbeatService: ISessionHeartbeatService, IDisposable
{
   private readonly CompositeDisposable _disposables = new();
   private readonly TimeSpan _heartbeatInterval = TimeSpan.FromMinutes(10);
   private readonly BehaviorSubject<bool> _isSessionExpiredSubject = new(false);
   
   private readonly IConnectivityService _connectivityService;
   private readonly ICtuSessionStore _sessionStore;
   private readonly IAuthClient _authClient;
   private readonly ILogger<SessionHeartbeatService> _logger;
   
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
      if (_isStarted) return;
      _isSessionExpiredSubject.OnNext(false);

      var interval = Observable.Interval(_heartbeatInterval, scheduler: TaskPoolScheduler.Default)
         .CombineLatest(_connectivityService.IsInternetAvailable, (_, isAvailable) => isAvailable)
         .Where(isAvailable => isAvailable)
         .CombineLatest(_sessionStore.CtuSessionChanged, (_, session) => session)
         .SelectMany(session => Observable.FromAsync(ct => _authClient.PingSessionAsync(ct)))
         .Where(x => !x)
         .Subscribe(_ =>
         {
            _logger.LogWarning("Session has expired!.");
            
         });
      
       
        
      _isStarted = true;
   }

   public void Stop()
   {
      _isStarted = false;
   }

   public void Dispose()
   {
      _disposables.Dispose();
   }
}