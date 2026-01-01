using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Services.UserSessionService;

public interface IWorkspaceStore
{
    Task<bool> SaveAsync(string filePath);
    Task<bool> LoadAsync(string filePath);
}