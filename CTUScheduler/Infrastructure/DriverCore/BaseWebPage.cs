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
        .Select(url => IsMatchUrl(url, UriHost, PathRegexPattern))
        .DistinctUntilChanged();
    
    protected BaseWebPage(IWebDriverService webDriverService,ILoggerFactory loggerFactory) : base(webDriverService, loggerFactory)
    {
    }

    public async Task<bool> TryWaitForActiveAsync(int stabilityMs = 1000, int timeout = 10000)
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

    public abstract Task NavigateToAsync(bool allowRedirection = true,
        CancellationToken cancellationToken = default);



    protected virtual bool IsMatchUrl(string currentUrl, string uriHost, string pathRegexPattern)
    {
        if (string.IsNullOrWhiteSpace(currentUrl) 
            || string.IsNullOrWhiteSpace(uriHost)
            || string.IsNullOrWhiteSpace(pathRegexPattern)) return false;
        try
        {
            var uri = new Uri(currentUrl);
            var isHostMatch = uri.Host.Contains(uriHost, StringComparison.OrdinalIgnoreCase);
            var isPathMatch = Regex.IsMatch(uri.AbsolutePath, pathRegexPattern, RegexOptions.IgnoreCase);
            return isHostMatch && isPathMatch;
        }
        catch
        {
            return false;
        }
    }
}