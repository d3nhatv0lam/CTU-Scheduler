using System;
using System.Threading.Tasks;
using CTUScheduler.Infrastructure.Sites.Base;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace CTUScheduler.Infrastructure.DriverCore.Refactor.Page;

public abstract class AppPage: ISitePageRefactor
{
    protected readonly IWebTab Tab;
    protected readonly ILogger Logger;
    
    public abstract string PageUrl { get; }
    protected abstract string PageReadySelector { get; }
    
    public string CurrentUrl => Tab.CurrentUrl;

    protected AppPage(IWebTab tab, ILoggerFactory loggerFactory)
    {
        Tab = tab;
        Logger = loggerFactory.CreateLogger(GetType());
    }
    
    public virtual async Task NavigateToAsync() => await Tab.GoToAsync(PageUrl);
    
    public virtual async Task WaitForReadyAsync(int timeoutMs = 15000)
    {
        var locator = Tab.GetLocator(PageReadySelector);
        await locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = timeoutMs });
    }
    
    public virtual async Task<bool> IsActiveAsync()
    {
        try
        {
            if (!Tab.CurrentUrl.Contains(PageUrl, StringComparison.OrdinalIgnoreCase)) return false;
            return await Tab.GetLocator(PageReadySelector).IsVisibleAsync();
        }
        catch { return false; }
    }
}