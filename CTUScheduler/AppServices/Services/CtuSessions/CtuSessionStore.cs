using CTUScheduler.AppServices.Services.Abstractions;
using CTUScheduler.Core.Networking;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.CtuSessions;

public sealed class CtuSessionStore: StateStore<CtuSession>, ICtuSessionStore
{
    public CtuSessionStore(ILogger<CtuSessionStore> logger) : base(logger)
    {
    }
}