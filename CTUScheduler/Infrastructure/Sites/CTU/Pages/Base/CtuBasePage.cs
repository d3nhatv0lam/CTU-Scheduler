using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Infrastructure.DriverCore;
using CTUScheduler.Infrastructure.Sites.CTU.Pages.Login;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Sites.CTU.Pages.Base;

public abstract class CtuBasePage : BaseWebPage
{
    protected override string UriHost => "ctu.edu.vn";
    protected const string LOGIN_PAGE_URL = "https://htql.ctu.edu.vn/";
    protected const string LOGIN_URL_PATTERN = @"/authenticationendpoint/login\.do";
    protected CtuBasePage(IWebDriverService webDriverService, ILoggerFactory loggerFactory) : base(webDriverService, loggerFactory)
    {
    }
    
    /// <summary>
    /// Check if the session is still valid.
    /// </summary>
    /// <exception cref="SessionExpiredException"></exception>
    protected virtual async Task EnsureSessionValid()
    {
        var isSessionDead = await IsSsrSessionInvalidOnPage();
        
        if (!isSessionDead)
            isSessionDead = await IsSessionInvalidOnPage();
        
        if (isSessionDead)
        {
            await OnSessionExpired();
            throw new SessionExpiredException();
        }
    }

    /// <summary>
    ///  Server side rendering session is invalid on login page.
    ///  Solve ssr page of Ctu
    /// </summary>
    /// <returns>bool</returns>
    private Task<bool> IsSsrSessionInvalidOnPage()
    {
        return Task.FromResult(IsLoginPage() && this is not LoginPage);
    }
    /// <summary>
    ///  Check if the session is invalid on the current page. <br/>
    ///  Default return false - session is valid.
    /// </summary>
    /// <returns></returns>
    protected virtual Task<bool> IsSessionInvalidOnPage()
    {
        return Task.FromResult(false);
    }

    protected virtual Task OnSessionExpired()
    {
        WebDriverService.GoToPageAsync(LOGIN_PAGE_URL);
        return Task.CompletedTask;
    }

    private bool IsLoginPage()
    {
        return IsMatchUrl(WebDriverService.PageUrl, UriHost, LOGIN_URL_PATTERN);
    }
}