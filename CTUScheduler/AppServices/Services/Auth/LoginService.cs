using System;
using System.Threading.Tasks;
using CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.WebResponse;
using CTUScheduler.Infrastructure.Sites.CTU.Factory;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.Auth;

public class LoginService: ILoginService
{
    private readonly ICtuSitePageFactory _ctuSitePageFactory;
    private readonly ILogger<LoginService> _logger;
    private const int MaxTimetableCountLimit = -1;
    
    public LoginService(ICtuSitePageFactory ctuSitePageFactory, ILogger<LoginService> logger)
    {
        _ctuSitePageFactory = ctuSitePageFactory;
        _logger = logger;
    }

    public async Task<OperationResult> NavigateToAsync(int tries = -1)
    {
        try
        {
            await _ctuSitePageFactory.GetPage<ILoginPage>().NavigateToAsync(tries);
            return OperationResult.Success();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to navigate to login page");
            return OperationResult.Failed("Trang đăng nhập không phản hồi!",);
        }
    }
    
    public async Task<OperationResult> LoginAsync(string username, string password)
    {
        var loginPage = _ctuSitePageFactory.GetPage<ILoginPage>();
        var res = await loginPage.LoginAsync(username, password);
        if (!res.IsSuccess)
        {
            _logger.LogWarning("User '{User}' failed to login. Reason: {Error}", username, res.ErrorMessage);
        }
        return res;
    }
    
}