using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Networking;

namespace CTUScheduler.AppServices.Services.CtuSessions;

public interface ICtuSessionStore : ICtuSessionAccessor, IStateStore<CtuSession>
{
}