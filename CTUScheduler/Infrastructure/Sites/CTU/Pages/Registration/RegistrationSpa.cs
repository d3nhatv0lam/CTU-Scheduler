using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Infrastructure.DriverCore;
using CTUScheduler.Infrastructure.Sites.CTU.Pages.Base;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Sites.CTU.Pages.Registration;

public abstract class RegistrationSpa: CtuBasePage
{
    protected readonly Sidebar Sidebar = new();
    protected override string PageUrl => "https://dkmhfe.ctu.edu.vn/dangkyhocphan/sinhvien/quydinhdangky";
    private const string InvalidSessionSelector = ".ant-modal-confirm-error";
    protected const string SPA_REGEX_PATTERN = "/dangkyhocphan/sinhvien";
    protected RegistrationSpa(IWebDriverService webDriverService, ILoggerFactory logger) : base(webDriverService, logger) { }
    
    public override async Task NavigateToAsync(bool allowRedirection = true, CancellationToken cancellationToken = default)
    {
        if (IsInPage())
        {
            await NavigateToViaSidebarAsync(cancellationToken);
            return;
        }
        if (allowRedirection)
            await WebDriverService.GoToPageAsync(PageUrl);
        await EnsureSessionValid();
    }

    protected override async Task<bool> IsSessionInvalidOnPage()
    {
        // nhìn thấy InvalidSessionSelector -> Invalid session
        return await WebDriverService.GetLocator(InvalidSessionSelector).IsVisibleAsync();
    }

    protected abstract Task NavigateToViaSidebarAsync(CancellationToken cancellationToken = default);

    private bool IsInPage()
    {
        return IsMatchUrl(WebDriverService.PageUrl, UriHost, SPA_REGEX_PATTERN);
    }
}