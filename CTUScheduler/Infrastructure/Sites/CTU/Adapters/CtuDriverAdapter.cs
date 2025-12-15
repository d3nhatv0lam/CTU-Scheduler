using System;
using CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;
using CTUScheduler.Infrastructure.DriverCore;
using CTUScheduler.Infrastructure.Sites.CTU.Pages.Login;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Sites.CTU.Adapters;

public class CtuDriverAdapter: ICtuDriverAdapter
{
    private readonly ILogger<CtuDriverAdapter> _logger;
    private readonly IWebDriverService _webDriverService;
    private readonly Lazy<ILoginPage> _lazyLoginPage;
    private readonly Lazy<IMainPage> _lazyMainPage;
    
    public string SiteName { get; } = "CTU";
    public ILoginPage LoginPage => _lazyLoginPage.Value;
    public IMainPage MainPage => _lazyMainPage.Value;

    public CtuDriverAdapter(IWebDriverService webDriverService, 
        ILogger<CtuDriverAdapter> logger, 
        Lazy<ILoginPage> lazyLoginPage,
        Lazy<IMainPage> lazyMainPage)
    {
        _webDriverService = webDriverService;
        _logger = logger;

        _lazyLoginPage = lazyLoginPage;
        _lazyMainPage = lazyMainPage;
    }
    
    // public T GetPage<T>() where T : ISitePage 
    // {
    //     return _serviceProvider.GetRequiredService<T>();
    // }
}