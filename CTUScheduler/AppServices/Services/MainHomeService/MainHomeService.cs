using System;
using System.Reactive.Linq;
using CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;
using CTUScheduler.Infrastructure.Sites.CTU.Factory;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.MainHomeService;

public class MainHomeService: IMainHomeService
{
    private readonly ICtuSitePageFactory _ctuSitePageFactory;
    private readonly ILogger<MainHomeService> _logger;
    private readonly IMainPage _mainPage;

    public IObservable<string> StudentIdChanges { get; } 
    public MainHomeService(ICtuSitePageFactory ctuSitePageFactory, ILogger<MainHomeService> logger)
    {
        _ctuSitePageFactory = ctuSitePageFactory;
        _logger = logger;
        _mainPage = _ctuSitePageFactory.GetPage<IMainPage>();
        
        StudentIdChanges = _mainPage.UserInfoChanges
            .Where(x => !string.IsNullOrWhiteSpace(x));
    }
    
}