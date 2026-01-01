using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CTUScheduler.AppServices.Services.ScheduleService.Interfaces;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;
using CTUScheduler.Core.Models.UserSaves;
using DynamicData.Aggregation;

namespace CTUScheduler.AppServices.Services.UserSessionService;

public class UserSessionService: IUserSessionService, IDisposable
{
    private readonly CompositeDisposable _disposable = new();
    private readonly BehaviorSubject<RegistrationInformation?> _serverInfoSubject = new(null);
    private readonly BehaviorSubject<RegistrationContext?> _localContextSubject = new(null);
    private readonly BehaviorSubject<DateTimeOffset?> _lastSavedSubject = new(null);
    public IObservable<RegistrationContext?> LocalContext => _localContextSubject.AsObservable();
    public RegistrationContext? CurrentContext => _localContextSubject.Value ?? _serverInfoSubject.Value?.ToContext();
    public IObservable<RegistrationInformation?> RegistrationInfo => _serverInfoSubject.AsObservable();
    public RegistrationInformation? CurrentRegistrationInfo => _serverInfoSubject.Value;
    public IObservable<bool> IsReadonly { get; }
    public IObservable<DateTimeOffset?> LastSaved { get; }

    public UserSessionService(IProfileQueryService profileQueryService)
    {
        _serverInfoSubject.DisposeWith(_disposable);
        _localContextSubject.DisposeWith(_disposable);
        _lastSavedSubject.DisposeWith(_disposable);
        
        var isProfileEmpty = profileQueryService.ConnectProfiles()
            .Count()
            .Select(x => x == 0);
        
        IsReadonly = _localContextSubject
            .CombineLatest(_serverInfoSubject, isProfileEmpty, 
                (local, serverInfo, empty) =>
            {
                if (empty) return false; 
                
                if (local is null || serverInfo is null) return false;
                
                return local.GetContextId() != serverInfo.ToContext().GetContextId();
            })
            .DistinctUntilChanged()
            .Replay(1)
            .RefCount();
        
        isProfileEmpty
            .Where(empty => empty)
            .WithLatestFrom(_serverInfoSubject, (_, serverInfo) => serverInfo)
            .WithLatestFrom(_localContextSubject, (serverInfo, localCtx) => (serverInfo, localCtx))
            .Subscribe(state => 
            {
                if (state.serverInfo == null) return;
                var serverCtx = state.serverInfo.ToContext();
                // không có local   // server và local chưa đồng bộ
                if (state.localCtx is null || state.localCtx.GetContextId() != serverCtx.GetContextId())
                {
                    _localContextSubject.OnNext(serverCtx);
                }
            })
            .DisposeWith(_disposable);
        
        LastSaved = _lastSavedSubject.AsObservable();
    }
    
    public void SetContext(RegistrationContext context)
    {
        _localContextSubject.OnNext(context);
    }

    public void UpdateServerInfo(RegistrationInformation info)
    {
        _serverInfoSubject.OnNext(info);
    }
    public void NotifySaved()
    {
        _lastSavedSubject.OnNext(DateTimeOffset.Now);
    }
    public void SetLastModified(DateTimeOffset time)
    {
        _lastSavedSubject.OnNext(time);
    }

    public void Dispose()
    {
        _disposable.Dispose();
    }
}