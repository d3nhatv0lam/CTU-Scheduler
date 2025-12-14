using System;
using System.Threading;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Services.WebDriver.Interfaces;

public interface ISitePage
{
    IObservable<bool> IsActive { get; }
    Task NavigateToAsync(int maxRetries = 3, CancellationToken cancellationToken = default);
}