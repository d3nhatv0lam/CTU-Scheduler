using System;
using CTUScheduler.AppServices.Services.WebDriver.Core;
using CTUScheduler.AppServices.Services.WebDriver.Interfaces;
using CTUScheduler.AppServices.Services.WebDriver.Sites.CTU.Pages.Login;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.WebDriver.Sites.CTU.Adapters;

public class CtuDriverAdapter: ICtuDriverAdapter
{
    private readonly ILogger<CtuDriverAdapter> _logger;
    private readonly IWebDriverService _webDriverService;
    private readonly Lazy<ICtuLoginPage> _lazyLoginPage;
    
    public string SiteName { get; } = "CTU";
    public ICtuLoginPage CtuLoginPage => _lazyLoginPage.Value;

    public CtuDriverAdapter(IWebDriverService webDriverService, 
        ILogger<CtuDriverAdapter> logger, 
        Lazy<ICtuLoginPage> lazyLoginPage)
    {
        _webDriverService = webDriverService;
        _logger = logger;

        _lazyLoginPage = lazyLoginPage;
    }
    
    // public T GetPage<T>() where T : ISitePage 
    // {
    //     return _serviceProvider.GetRequiredService<T>();
    // }
}