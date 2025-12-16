using System;
using System.Reactive.Linq;
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

    public abstract Task NavigateToAsync(int maxRetries = 3, CancellationToken cancellationToken = default);

    protected virtual bool IsMatchUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        try
        {
            var uri = new Uri(url);
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