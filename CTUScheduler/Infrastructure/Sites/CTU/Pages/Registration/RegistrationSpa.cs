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
    protected override string PathRegexPattern => "/dangkyhocphan/sinhvien";
    private const string InvalidSessionSelector = ".ant-modal-confirm-error";
    
    protected RegistrationSpa(IWebDriverService webDriverService, ILoggerFactory logger) : base(webDriverService, logger) { }
    
    public override async Task NavigateToAsync(int maxRetries = 3, CancellationToken cancellationToken = default)
    {
        if (await IsActive.FirstAsync())
        {
            await NavigateToViaSidebarAsync(cancellationToken);
            return;
        }
        await WebDriverService.GoToPageAsync(PageUrl);
        await EnsureSessionValid();
    }

    protected override async Task<bool> IsSessionInvalidOnPage()
    {
        // nhìn thấy InvalidSessionSelector -> Invalid session
        return await WebDriverService.GetLocator(InvalidSessionSelector).IsVisibleAsync();
    }

    protected abstract Task NavigateToViaSidebarAsync(CancellationToken cancellationToken = default);
}