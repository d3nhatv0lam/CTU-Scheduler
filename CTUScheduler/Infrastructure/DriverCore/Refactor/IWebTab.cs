using System;
using System.Threading.Tasks;
using CTUScheduler.Infrastructure.DriverCore.Response;
using Microsoft.Playwright;

namespace CTUScheduler.Infrastructure.DriverCore.Refactor;

public interface IWebTab : IAsyncDisposable
{
    string CurrentUrl { get; }
    IObservable<NetworkPacket> JsonResponse { get; }
    IObservable<string> UrlChanges { get; }
    
    IObservable<DialogInfo> AlertReceived { get; }
    IObservable<DialogInfo> ConfirmReceived { get; }
    IObservable<DialogInfo> PromptReceived { get; }

    ILocator GetLocator(string selector);
    Task GoToAsync(string url);
    Task<bool> IsVisibleAsync(string selector);
    Task FillAsync(string selector, string value);
    Task ClickAsync(string selector);
    Task RefreshAsync();
    Task ClickNavigateElementAsync(ILocator element, LocatorClickOptions? options = null, LoadState loadState = LoadState.NetworkIdle);
    Task ClickNavigateElementAsync(string element, LocatorClickOptions? options = null, LoadState loadState = LoadState.NetworkIdle);

    Task<string> GetTextAsync(string selector);
    
    // Cổng thao tác JS nội bộ mạnh mẽ
    Task<T> EvaluateAsync<T>(string script, object? args = null);
    Task EvaluateActionAsync(string script, object? args = null);
    
    // Đợi cho đến khi URL thỏa mãn một điều kiện
    Task WaitForUrlAsync(Func<string, bool> predicate, int timeoutMs = 10000);

    // Đợi cho đến khi một selector xuất hiện
    Task WaitForSelectorAsync(string selector, int timeoutMs = 10000);
}