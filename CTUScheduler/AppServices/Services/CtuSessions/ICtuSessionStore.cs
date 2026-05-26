using System;
using CTUScheduler.Core.Networking;

namespace CTUScheduler.AppServices.Services.CtuSessions;

public interface ICtuSessionStore: ICtuSessionAccessor
{
    IObservable<CtuSession?> CtuSessionChanged { get; }
    CtuSession? CurrentSession { get; }
    void Update(CtuSession session);
    void Clear();
}