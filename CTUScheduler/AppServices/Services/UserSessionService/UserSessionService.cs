using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CTUScheduler.AppServices.Extensions;
using CTUScheduler.AppServices.Services.ScheduleService;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration;
using CTUScheduler.Core.Models.Settings;
using DynamicData.Aggregation;

namespace CTUScheduler.AppServices.Services.UserSessionService;

public class UserSessionService : IUserSessionService, IDisposable
{
    private readonly CompositeDisposable _disposable = new();
    private readonly BehaviorSubject<RegistrationInformation?> _serverInfoSubject = new(null);
    private readonly BehaviorSubject<RegistrationContext?> _localContextSubject = new(null);
    private readonly BehaviorSubject<DateTimeOffset?> _lastSavedSubject = new(null);

    public IObservable<RegistrationContext?> LocalContextChanged { get; }

    public IObservable<RegistrationInformation?> RegistrationInfoChanged { get; }
    public IObservable<bool> IsReadonly { get; }
    public IObservable<DateTimeOffset?> LastSaved { get; }

    public RegistrationInformation? CurrentRegistrationInfo => _serverInfoSubject.Value;
    public RegistrationContext? CurrentContext => _localContextSubject.Value ?? _serverInfoSubject.Value?.ToContext();

    public UserSessionService(IProfileQueryService profileQueryService)
    {
        _serverInfoSubject.DisposeWith(_disposable);
        _localContextSubject.DisposeWith(_disposable);
        _lastSavedSubject.DisposeWith(_disposable);

        LocalContextChanged = _localContextSubject.DistinctUntilChanged().AsObservable();
        RegistrationInfoChanged = _serverInfoSubject.AsObservable();

        var isEmptyProfiles = profileQueryService.ConnectProfiles()
            .SubscribeOn(TaskPoolScheduler.Default)
            .Count()
            .Select(x => x == 0)
            .DistinctUntilChanged()
            .Replay(1)
            .RefCount();


        IsReadonly = _localContextSubject
            .CombineLatest(_serverInfoSubject, isEmptyProfiles,
                (local, serverInfo, empty) =>
                {
                    if (empty) return false;
                    if (local is null || serverInfo is null) return false;

                    // Sử dụng tính năng so sánh (value equality) của Record thay vì GetContextId()
                    return local != serverInfo.ToContext();
                })
            .DistinctUntilChanged()
            .Replay(1)
            .RefCount();

        isEmptyProfiles
            .Where(empty => empty)
            .WithLatestFrom(_serverInfoSubject, (_, serverInfo) => serverInfo)
            .WithLatestFrom(_localContextSubject, (serverInfo, localCtx) => (serverInfo, localCtx))
            .Subscribe(state =>
            {
                if (state.serverInfo is null) return;
                var serverCtx = state.serverInfo.ToContext();

                if (state.localCtx != serverCtx)
                {
                    _localContextSubject.OnNext(serverCtx);
                }
            })
            .DisposeWith(_disposable);

        LastSaved = _lastSavedSubject.AsObservable();
    }


    public void SetLocalContext(RegistrationContext? context)
    {
        if (_localContextSubject.Value == context)
            return;
        _localContextSubject.OnNext(context);
    }

    public void UpdateServerInfo(RegistrationInformation info)
    {
        _serverInfoSubject.OnNext(info);
    }

    public void NotifyModified()
    {
        _lastSavedSubject.OnNext(DateTimeOffset.Now);
    }

    public void SetLastModified(DateTimeOffset? time)
    {
        if (time.HasValue && time.Value == DateTimeOffset.MinValue)
            _lastSavedSubject.OnNext(null);
        else
            _lastSavedSubject.OnNext(time);
    }

    public void Dispose()
    {
        _disposable.Dispose();
    }
}