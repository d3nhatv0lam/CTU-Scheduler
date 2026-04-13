using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Infrastructure.DriverCore.Refactor;
using CTUScheduler.Infrastructure.Sites.CTU.Routes;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace CTUScheduler.Infrastructure.Sites.Base;

public abstract class AppPage : ISitePageRefactor
{
    protected readonly IWebTab Tab;
    protected readonly ILogger Logger;

    public abstract string PageUrl { get; }
    protected abstract string PageReadySelector { get; }

    public string CurrentUrl => Tab.CurrentUrl;

    protected virtual string ExpectedPath
    {
        get
        {
            try
            {
                return new Uri(PageUrl).AbsolutePath;
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    protected AppPage(IWebTab tab, ILoggerFactory loggerFactory)
    {
        Tab = tab;
        Logger = loggerFactory.CreateLogger(GetType());
    }

    public virtual async Task NavigateToAsync(PageGotoOptions? options = null) =>
        await Tab.NativePage.GotoAsync(PageUrl, options);

    public virtual async Task WaitForReadyAsync(int timeoutMs = 10000)
    {
        var waitReadyTask = Tab.NativePage.Locator(PageReadySelector)
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = timeoutMs });

        var waitSessionDeadTask =
            Tab.NativePage.WaitForURLAsync(CtuRoutes.AuthRedirectRegex, new() { Timeout = timeoutMs });

        var finishedTask = await Task.WhenAny(waitReadyTask, waitSessionDeadTask);

        if (finishedTask == waitSessionDeadTask)
        {
            throw new SessionExpiredException(
                $"Bị Server đóng phiên làm việc khi đang cố mở trang {GetType().Name}! (Bị đá về URL: {Tab.CurrentUrl})");
        }

        await waitReadyTask;
    }

    public virtual async Task<bool> IsActiveAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(ExpectedPath) ||
                !Tab.CurrentUrl.Contains(ExpectedPath, StringComparison.OrdinalIgnoreCase)) return false;

            return await Tab.NativePage.Locator(PageReadySelector).IsVisibleAsync();
        }
        catch
        {
            return false;
        }
    }

    public virtual async Task CheckSessionAndThrowAsync()
    {
        if (this is not IRequireSession) return;
        
        if (await IsSessionExpiredAsync())
        {
            throw new UnauthorizedAccessException($"Session chết tại {GetType().Name}");
        }
    }

    protected virtual Task<bool> IsSessionExpiredAsync()
    {
        var url = Tab.CurrentUrl;

        bool isExpired = CtuRoutes.AuthRedirectSignatures
            .Any(signature => url.Contains(signature, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(isExpired);
    }
}