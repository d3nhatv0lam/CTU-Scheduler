using CTUScheduler.AppServices.Services.Abstractions;
using CTUScheduler.Core.Networking;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.CtuSessions;

public class CtuSessionStore: StateStore<CtuSession>, ICtuSessionStore
{
    public CtuSessionStore(ILogger<StateStore<CtuSession>> logger) : base(logger)
    {
    }
}