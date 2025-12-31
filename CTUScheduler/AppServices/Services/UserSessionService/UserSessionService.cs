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
    private readonly BehaviorSubject<RegistrationInformation?> _liveInfoSubject = new(null);
    private readonly BehaviorSubject<RegistrationContext?> _localContextSubject = new(null);
    private readonly BehaviorSubject<DateTimeOffset?> _lastSavedSubject = new(null);

    public IObservable<RegistrationContext?> LocalContext => _localContextSubject.AsObservable();
    public IObservable<RegistrationInformation?> RegistrationInfo => _liveInfoSubject.AsObservable();
    public IObservable<bool> IsReadonly { get; }
    public IObservable<DateTimeOffset?> LastSaved { get; }

    public UserSessionService(IProfileQueryService profileQueryService)
    {
        _liveInfoSubject.DisposeWith(_disposable);
        _localContextSubject.DisposeWith(_disposable);
        _lastSavedSubject.DisposeWith(_disposable);
        
        var isContextMismatch = _localContextSubject
            .CombineLatest(_liveInfoSubject, (local, live) =>
            {
                if (local is null) return false;
                if (live is null) return true; 
                return local.GetContextId() != live.ToContext().GetContextId();
            });
        
        var isProfileEmpty = profileQueryService.ConnectProfiles()
            .Count()
            .Select(x => x == 0);
        
        IsReadonly = isContextMismatch
            .CombineLatest(isProfileEmpty, (mismatch, empty) => mismatch || empty)
            .DistinctUntilChanged()
            .Replay(1)
            .RefCount();
        
        LastSaved = _lastSavedSubject.AsObservable();
    }
    
    public void SetContext(RegistrationContext context)
    {
        _localContextSubject.OnNext(context);
    }

    public void UpdateLiveInfo(RegistrationInformation info)
    {
        _liveInfoSubject.OnNext(info);
    }
    public void NotifySaved()
    {
        _lastSavedSubject.OnNext(DateTimeOffset.Now);
    }
    public void SetLastSaved(DateTimeOffset time)
    {
        _lastSavedSubject.OnNext(time);
    }

    public void Dispose()
    {
        _disposable.Dispose();
    }
}