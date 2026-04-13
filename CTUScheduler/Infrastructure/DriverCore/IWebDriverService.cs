using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Infrastructure.DriverCore.Response;
using Microsoft.Playwright;

namespace CTUScheduler.Infrastructure.DriverCore;

/// <summary>
/// DEPRECATED: Use CTUScheduler.Infrastructure.DriverCore.Refactor.IWebDriverService instead
/// This interface is kept for backward compatibility and will be removed in future versions
/// </summary>
[Obsolete("Use CTUScheduler.Infrastructure.DriverCore.Refactor.IWebDriverService with WebDriverServiceAdapter instead")]
public interface IWebDriverService
{
    IPage? CurrentPage { get; }
    string PageUrl { get; }
    IObservable<string> InstallationStatus { get; }
    IObservable<bool> IsInstalling { get; }
    IObservable<string> InstallationProgress { get; }
    IObservable<string> MainFrameUrlChanges { get; } 
    IObservable<DialogInfo> AlertReceived { get; }
    IObservable<DialogInfo> ConfirmReceived { get; }
    IObservable<DialogInfo> PromptReceived { get; }
    IObservable<NetworkPacket> JsonResponse { get; }
    
    Task InitWebDriverService(CancellationToken cancellationToken = default);
    Task<bool> TryWaitForUrlAsync(string url, int timeout = 10000);
    Task GoToPageAsync(string url);
    Task RefreshPageAsync();
    ILocator GetLocator(string selector);
    Task ClickNavigateElementAsync(ILocator element, LocatorClickOptions? options = null,
        LoadState loadState = LoadState.Load);
    Task ClickNavigateElementAsync(string selector, LocatorClickOptions? options = null,
        LoadState loadState = LoadState.Load);
}