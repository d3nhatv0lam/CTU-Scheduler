using System.Threading.Tasks;

namespace CTUScheduler.Infrastructure.DriverCore.Refactor;

public interface IWebDriverService
{
    IWebTab MainTab { get; }
    Task InitBrowserAsync();
    Task ResetBrowserAsync();
    Task<IWebTab> CreateTabAsync();
}