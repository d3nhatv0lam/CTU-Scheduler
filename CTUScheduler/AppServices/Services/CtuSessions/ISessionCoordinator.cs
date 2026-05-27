using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Core.Networking;

namespace CTUScheduler.AppServices.Services.CtuSessions;

public interface ISessionCoordinator
{
    Task<OperationResult> StartSessionAsync(string username, string password, CancellationToken ct = default);

    Task EndSessionAsync();
    
    Task<CtuSession?> RefreshSessionAsync(CtuSession expiredSession, CancellationToken ct);

    IObservable<Unit> SessionExpired { get; }
}