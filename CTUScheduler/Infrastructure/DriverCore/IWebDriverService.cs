using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace CTUScheduler.Infrastructure.DriverCore;

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
    IObservable<JsonElement> JsonResponse { get; }
    
    Task InitWebDriverService();
    Task<bool> TryWaitForUrlAsync(string url, int timeout = 10000);
    Task GoToPageAsync(string url);
    Task RefreshPageAsync();
    ILocator GetLocator(string selector);
    Task ClickNavigateElementAsync(ILocator element, LocatorClickOptions? options = null,
        LoadState loadState = LoadState.Load);
    Task ClickNavigateElementAsync(string selector, LocatorClickOptions? options = null,
        LoadState loadState = LoadState.Load);
}