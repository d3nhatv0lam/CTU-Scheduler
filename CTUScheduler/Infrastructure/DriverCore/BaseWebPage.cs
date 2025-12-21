using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Interfaces.WebDriver.Sites;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.DriverCore;

public abstract class BaseWebPage: BaseUiContext, ISitePage
{
    protected abstract string PageUrl { get; }
    protected virtual string UriHost
    {
        get
        {
            try
            {
                return new Uri(PageUrl).Host;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
    protected virtual string PathRegexPattern
    {
        get
        {
            try
            {
                string path = new Uri(PageUrl).AbsolutePath;
                return Regex.Escape(path);
            }
            catch
            {
                return ".*";
            }
        }
    }
    
    public IObservable<bool> IsActive => WebDriverService.MainFrameUrlChanges
        .StartWith(WebDriverService.PageUrl)
        .Select(IsMatchUrl)
        .DistinctUntilChanged();
    
    protected BaseWebPage(IWebDriverService webDriverService,ILoggerFactory loggerFactory) : base(webDriverService, loggerFactory)
    {
    }

    public async Task<bool> TryWaitForActiveAsync(int stabilityMs = 2000, int timeout = 10000)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            return await IsActive
                .Throttle(TimeSpan.FromMilliseconds(stabilityMs))
                .Where(x => x)
                .FirstAsync()
                .ToTask(cts.Token);
        }
        catch
        {
            return false;
        }
    }

    public abstract Task NavigateToAsync(int maxRetries = 3, CancellationToken cancellationToken = default);

    protected virtual bool IsMatchUrl(string currentUrl)
    {
        if (string.IsNullOrEmpty(currentUrl)) return false;
        try
        {
            var uri = new Uri(currentUrl);
            var isHostMatch = uri.Host.Contains(UriHost, StringComparison.OrdinalIgnoreCase);
            var isPathMatch = Regex.IsMatch(uri.AbsolutePath, PathRegexPattern, RegexOptions.IgnoreCase);
            return isHostMatch && isPathMatch;
        }
        catch
        {
            return false;
        }
    }
}