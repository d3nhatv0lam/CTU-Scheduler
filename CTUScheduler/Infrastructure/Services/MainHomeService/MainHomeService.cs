using System;
using System.Reactive.Linq;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Factory;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Services.MainHomeService;

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