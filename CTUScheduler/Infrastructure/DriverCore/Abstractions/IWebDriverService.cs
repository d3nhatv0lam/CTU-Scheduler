using System.Threading.Tasks;

namespace CTUScheduler.Infrastructure.DriverCore.Abstractions;

public interface IWebDriverService
{
    IWebTab MainTab { get; }
    Task InitBrowserAsync();
    Task ResetBrowserAsync();
    Task<IWebTab> CreateTabAsync();
}