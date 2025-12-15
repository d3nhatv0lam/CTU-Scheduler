using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Infrastructure.DriverCore;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Sites.CTU.Pages.Registration;

public abstract class RegistrationSpa<T>: CtuBasePage<T> where T: class
{
    protected override string PageUrl => "https://dkmhfe.ctu.edu.vn/dangkyhocphan/sinhvien/quydinhdangky";
    protected override string PathRegexPattern { get; } = "/dangkyhocphan/sinhvien";
    protected Sidebar Sidebar = new();
    
    protected RegistrationSpa(IWebDriverService webDriverService, ILogger<T> logger) : base(webDriverService, logger) { }
    
    public override async Task NavigateToAsync(int maxRetries = 3, CancellationToken cancellationToken = default)
    {
        if (await IsActive.FirstAsync())
        {
            await NavigateToViaSidebarAsync(cancellationToken);
            return;
        }
        await WebDriverService.GoToPageAsync(PageUrl);
        EnsureSessionValid();
    }
    
    protected abstract Task NavigateToViaSidebarAsync(CancellationToken cancellationToken = default);
}