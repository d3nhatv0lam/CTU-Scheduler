using System.Threading;
using System.Threading.Tasks;

namespace CTUScheduler.Infrastructure.DriverCore.Abstractions;

public interface IWebDriverService
{
    IWebTab MainTab { get; }
    Task InitBrowserAsync(CancellationToken cancellationToken = default);
    Task ResetBrowserAsync();
    Task<IWebTab> CreateTabAsync();
}