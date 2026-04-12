using System;
using System.Threading;
using System.Threading.Tasks;

namespace CTUScheduler.Infrastructure.DriverCore.Refactor;

public interface IWebDriverInstallerService
{
    IObservable<string> StatusMessage { get; }
    IObservable<bool> IsBusy { get; }
    IObservable<double?> ProgressPercentage { get; }
    IObservable<string> LogStream { get; }
    
    /// <summary>
    /// Ensures that the Playwright browser dependencies are installed and available for use.
    /// This method verifies the integrity of the Playwright browser, performs cleanup of old installations,
    /// and installs the necessary browser files if they are not already available.
    /// </summary>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> to observe while waiting for the task to complete. The token can be
    /// used to cancel the operation.
    /// </param>
    /// <exception cref="Exception">
    /// Thrown if the installation process reports success but fails to verify the browser's integrity or
    /// if any other error occurs during the process.
    /// </exception>
    /// <remarks>
    /// The method interacts with internal helper methods to verify browser integrity, clean up old files,
    /// and manage the installation process. It also updates the internal state and status messages
    /// observable properties during the operation.
    /// </remarks>
    Task EnsureBrowserInstalledAsync(CancellationToken cancellationToken = default);
}