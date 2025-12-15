using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;
using CTUScheduler.Infrastructure.DriverCore;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace CTUScheduler.Infrastructure.Sites.CTU.Pages.Main;

public class MainPage: CtuBasePage<MainPage>, IMainPage
{
    protected override string PageUrl => "https://dkmh.ctu.edu.vn/htql/sinhvien/hindex.php";
    protected override string PathRegexPattern => "/hindex";
    private const string UserInfoSelector = "#user-login";
    private const string DkmhButtonSelector = "img[src*='hetinchi.gif']";
    
    public MainPage(IWebDriverService webDriverService, ILogger<MainPage> logger) : base(webDriverService, logger)
    {
    }
    
    public async Task<string> GetUserInfoAsync()
    {
        await EnsurePageActivated();
        
        ILocator userInfo = WebDriverService.GetLocator(UserInfoSelector);

        var info = await userInfo.InnerTextAsync(new()
        {
            Timeout = 5000
        });
        
        return info;
    }
    
    public override async Task NavigateToAsync(int maxRetries = 3, CancellationToken cancellationToken = default)
    {
        await WebDriverService.GoToPageAsync(PageUrl);
        EnsureSessionValid();
    }

    public async Task NavigateToDkmhAsync()
    {
        await EnsurePageActivated();
        
        var dkmhButton = WebDriverService.GetLocator(DkmhButtonSelector);
        await WebDriverService.ClickNavigateElementAsync(dkmhButton,null, LoadState.NetworkIdle);
        
        EnsureSessionValid();
    }
    

    private async Task EnsurePageActivated()
    {
        if (!await IsActive.FirstAsync())
            throw new InvalidOperationException($"Page is not active. Expected active state for '{PageUrl}', but current URL is '{WebDriverService.PageUrl}'");
    }
}