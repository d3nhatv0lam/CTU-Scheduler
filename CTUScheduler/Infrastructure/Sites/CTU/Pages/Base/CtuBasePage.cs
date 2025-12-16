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
    protected const string LOGIN_URL_PATTERN = @"/authenticationendpoint/login\.do";
    protected CtuBasePage(IWebDriverService webDriverService, ILoggerFactory loggerFactory) : base(webDriverService, loggerFactory)
    {
    }
    
    /// <summary>
    /// Check if the session is still valid.
    /// </summary>
    /// <exception cref="SessionExpiredException"></exception>
    protected virtual Task EnsureSessionValid()
    {
        if (IsLoginPage() && this is not LoginPage) 
        {
            throw new SessionExpiredException();
        }
        return Task.CompletedTask;
    }

    private bool IsLoginPage()
    {
        var url = WebDriverService.PageUrl;
        if (string.IsNullOrEmpty(url)) return false;
        try
        {
            var uri = new Uri(url);
            if (!uri.Host.Contains(UriHost, StringComparison.OrdinalIgnoreCase))
                return false;
            return Regex.IsMatch(uri.AbsolutePath, LOGIN_URL_PATTERN, RegexOptions.IgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}