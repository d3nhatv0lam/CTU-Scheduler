using System;
using System.Reactive;

namespace CTUScheduler.AppServices.Services.CtuSessions;

public interface ISessionHeartbeatService
{
    void Start();
    void Stop();
    
    IObservable<Unit> SessionExpired { get; }
}