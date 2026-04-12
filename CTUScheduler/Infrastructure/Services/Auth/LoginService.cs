using System;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.DriverCore.Refactor;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Factory;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Services.Auth;

public class LoginService: ILoginService
{
    private readonly IWebDriverService _playwrightService;
    private readonly ICtuPageFactory _ctuSitePageFactory;
    private readonly IWebSessionManager _sessionManager;
    private readonly ILogger<LoginService> _logger;
    
    public LoginService(IWebDriverService playwrightService, ICtuPageFactory ctuSitePageFactory, IWebSessionManager sessionManager, ILogger<LoginService> logger)
    {
        _playwrightService = playwrightService;
        _ctuSitePageFactory = ctuSitePageFactory;
        _sessionManager = sessionManager;
        _logger = logger;
    }

    public async Task<OperationResult> EnsureReadyAsync()
    {
        try
        {
            await using var tab = await  _playwrightService.CreateTabAsync();

            var page = _ctuSitePageFactory.GetPage<ILoginPage>(tab);
            return await _sessionManager.NavigateSafelyAsync(page);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to navigate to login page");
            return OperationResult.Failed("Trang đăng nhập không phản hồi!", kind:OperationFailureReason.Unauthorized);
        }
    }
    
    public async Task<OperationResult> LoginAsync(string username, string password)
    {
        await using var tab = await _playwrightService.CreateTabAsync();
        var page = _ctuSitePageFactory.GetPage<ILoginPage>(tab);
        await _sessionManager.NavigateSafelyAsync(page);
        
        return await page.PerformLoginActionAsync(username, password);
    }
    
}