using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace CTUScheduler.Infrastructure.Sites.Base;

public interface ISitePage
{
    IObservable<bool> IsActive { get; }
    Task<bool> TryWaitForActiveAsync(int stabilityMs = 1000, int timeout = 10000);
    Task NavigateToAsync(bool allowRedirection = true, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface cho các page objects trong kiến trúc mới
/// </summary>
public interface ISitePageRefactor
{
    string PageUrl { get; }
    string CurrentUrl { get; }
    Task NavigateToAsync(PageGotoOptions? options = null);
    Task WaitForReadyAsync(int timeoutMs = 15000);
    Task CheckSessionAndThrowAsync();
    Task<bool> IsActiveAsync();
}