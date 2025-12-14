using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Services.WebDriver.Interfaces;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.WebDriver.Core;

public abstract class BasePage<TPage>: ISitePage where TPage: class
{
    protected readonly IWebDriverService _webDriverService;
    protected readonly ILogger<TPage> _logger;
    protected abstract string PageUrlPattern { get; }

    public IObservable<bool> IsActive => _webDriverService.MainFrameUrlChanges
        .StartWith(_webDriverService.PageUrl)
        .Select(IsMatchUrl)
        .DistinctUntilChanged();
        
    public BasePage(IWebDriverService webDriverService, ILogger<TPage> logger)
    {
        _webDriverService = webDriverService;
        _logger = logger;
    }
    
    // how to navigate to this page?
    public abstract Task NavigateToAsync(int maxRetries = 3, CancellationToken cancellationToken = default);

    protected abstract bool IsMatchUrl(string url);
}