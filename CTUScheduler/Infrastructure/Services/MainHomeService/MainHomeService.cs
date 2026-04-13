using System;
using System.Reactive.Linq;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Infrastructure.DriverCore.Refactor;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Pages.Main;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Services.MainHomeService;

public class MainHomeService: IMainHomeService
{
    private readonly ILogger<MainHomeService> _logger;
    private readonly MainPage _mainPage;

    public IObservable<string> StudentIdChanges { get; }

    public MainHomeService(
        IWebDriverService webDriverService,
        ICtuPageFactory pageFactory,
        ILogger<MainHomeService> logger)
    {
        _logger = logger;
        _mainPage = pageFactory.GetPage<MainPage>(webDriverService.MainTab);

        StudentIdChanges = _mainPage.UserInfoChanges
            .Where(x => !string.IsNullOrWhiteSpace(x));
    }
}