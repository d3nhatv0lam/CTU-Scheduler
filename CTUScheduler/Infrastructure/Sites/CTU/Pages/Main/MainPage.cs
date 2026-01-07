using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;
using CTUScheduler.Infrastructure.DriverCore;
using CTUScheduler.Infrastructure.Sites.CTU.Pages.Base;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace CTUScheduler.Infrastructure.Sites.CTU.Pages.Main;

public class MainPage: CtuBasePage, IMainPage
{
    protected override string PageUrl => "https://dkmh.ctu.edu.vn/htql/sinhvien/hindex.php";
    protected override string PathRegexPattern => "/hindex";
    private const string UserInfoSelector = "#user-login";
    private const string DkmhButtonSelector = "img[src*=\"hetinchi.gif\"][onclick*=\"gotoDKindex\"]";
    public MainPage(IWebDriverService webDriverService, ILoggerFactory logger) : base(webDriverService, logger)
    {
    }

    public IObservable<string> UserInfoChanges => IsActive
        .Select(isActive =>
        {
            if (!isActive)
                return Observable.Return(string.Empty);

            return Observable.FromAsync(ct => GetUserInfoAsync(ct))
                .Retry(1)
                .Catch((Exception ex) =>
                {
                    Logger.LogWarning(ex, "fail when GetUserInfoAsync");
                    return Observable.Return(string.Empty);
                });
        })
        .Switch()
        .DistinctUntilChanged();
    
    
    public async Task<string> GetUserInfoAsync(CancellationToken cancellationToken = default)
    {
        await EnsurePageActivated();
        
        ILocator userInfo = WebDriverService.GetLocator(UserInfoSelector);

        var info = await userInfo.InnerTextAsync(new()
        {
            Timeout = 3000
        }).WaitAsync(cancellationToken)
        .ConfigureAwait(false);
        
        return info;
    }
    
    public override async Task NavigateToAsync(bool allowRedirection = true, CancellationToken cancellationToken = default)
    {
        if (allowRedirection)
            await WebDriverService.GoToPageAsync(PageUrl);
        await EnsureSessionValid();
    }

    public async Task NavigateToDkmhAsync()
    {
        await EnsurePageActivated();
        
        var dkmhButton = WebDriverService.GetLocator(DkmhButtonSelector);
        await WebDriverService.ClickNavigateElementAsync(dkmhButton,null, LoadState.NetworkIdle);
        
        await EnsureSessionValid();
    }
    

    private async Task EnsurePageActivated()
    {
        if (!await IsActive.FirstAsync())
            throw new InvalidOperationException($"Page is not active. Expected active state for '{PageUrl}', but current URL is '{WebDriverService.PageUrl}'");
    }
}