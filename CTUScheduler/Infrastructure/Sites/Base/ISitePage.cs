using System;
using System.Threading;
using System.Threading.Tasks;

namespace CTUScheduler.Infrastructure.Sites.Base;

public interface ISitePage
{
    IObservable<bool> IsActive { get; }
    Task<bool> TryWaitForActiveAsync(int stabilityMs = 1000, int timeout = 10000);
    Task NavigateToAsync(bool allowRedirection = true, CancellationToken cancellationToken = default);
}

public interface ISitePageRefactor
{
    string PageUrl { get; }
    string CurrentUrl { get; }
    Task NavigateToAsync();
    Task WaitForReadyAsync(int timeoutMs = 15000);
    Task<bool> IsActiveAsync();
}