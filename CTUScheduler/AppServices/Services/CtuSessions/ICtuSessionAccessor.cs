using CTUScheduler.Core.Networking;

namespace CTUScheduler.AppServices.Services.CtuSessions;

public interface ICtuSessionAccessor
{
    CtuSession? CurrentSession { get; }
}