using Microsoft.Playwright;

namespace RxAmethyst.PlaywrightWrapper.Abstractions;

/// <summary>
/// Interface cho các page objects trong kiến trúc mới
/// </summary>
public interface ISitePage
{
    string PageUrl { get; }
    string CurrentUrl { get; }
    Task NavigateToAsync(PageGotoOptions? options = null);
    Task WaitForReadyAsync(int timeoutMs = 30000);
    Task CheckSessionAndThrowAsync();
    Task<bool> IsActiveAsync();
}