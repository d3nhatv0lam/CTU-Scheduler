using System;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.DriverCore.Refactor;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace CTUScheduler.Infrastructure.Services.Auth;

public class LoginService: ILoginService
{
    private readonly IWebDriverService _playwrightService;
    private readonly ICtuPageFactory _ctuSitePageFactory;
    private readonly ILogger<LoginService> _logger;
    
    public LoginService(IWebDriverService playwrightService, ICtuPageFactory ctuSitePageFactory,  ILogger<LoginService> logger)
    {
        _playwrightService = playwrightService;
        _ctuSitePageFactory = ctuSitePageFactory;
        _logger = logger;
    }

    public async Task<OperationResult> EnsureReadyAsync()
    {
        var tab = _playwrightService.MainTab;
        try
        {
            var page = _ctuSitePageFactory.GetPage<ILoginPage>(tab);
            await page.NavigateToAsync(new ()
            {
                Timeout = 5000,
                WaitUntil = WaitUntilState.DOMContentLoaded
            });
            return OperationResult.Success();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to navigate to login page");
            return OperationResult.Failed("Trang đăng nhập không phản hồi!", kind:OperationFailureReason.Unauthorized);
        }
    }
    
    public async Task<OperationResult> LoginAsync(string username, string password)
    {
        var tab = _playwrightService.MainTab;
        var page = _ctuSitePageFactory.GetPage<ILoginPage>(tab);
        return await page.PerformLoginActionAsync(username, password);
    }
    
}