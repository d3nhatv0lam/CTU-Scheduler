using System.Threading.Tasks;

namespace CTUScheduler.Infrastructure.DriverCore.Refactor;

public interface IWebDriverService
{
    Task InitBrowserAsync();
    Task<IWebTab> CreateTabAsync();
}