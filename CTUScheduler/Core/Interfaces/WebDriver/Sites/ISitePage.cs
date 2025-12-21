using System;
using System.Threading;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Interfaces.WebDriver.Sites;

public interface ISitePage
{
    IObservable<bool> IsActive { get; }
    Task<bool> TryWaitForActiveAsync(int stabilityMs = 2000, int timeout = 10000);
    Task NavigateToAsync(int maxRetries = 3, CancellationToken cancellationToken = default);
}