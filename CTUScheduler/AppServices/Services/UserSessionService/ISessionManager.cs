using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Services.UserSessionService;

public interface ISessionManager
{
    Task LogoutAsync();
}