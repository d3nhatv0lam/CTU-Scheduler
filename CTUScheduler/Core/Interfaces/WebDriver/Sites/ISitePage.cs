using System;
using System.Threading;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Interfaces.WebDriver.Sites;

public interface ISitePage
{
    IObservable<bool> IsActive { get; }
    Task NavigateToAsync(int maxRetries = 3, CancellationToken cancellationToken = default);
}