using System;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Core.Models.WebResponse;
using CTUScheduler.Infrastructure.Sites.CTU.Factory;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.Auth;

public class LoginService: ILoginService
{
    private readonly ICtuSitePageFactory _ctuSitePageFactory;
    private readonly ILogger<LoginService> _logger;
    
    public LoginService(ICtuSitePageFactory ctuSitePageFactory, ILogger<LoginService> logger)
    {
        _ctuSitePageFactory = ctuSitePageFactory;
        _logger = logger;
    }

    public async Task<OperationResult> NavigateToAsync()
    {
        try
        {
            await _ctuSitePageFactory.GetPage<ILoginPage>().NavigateToAsync();
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
        var loginPage = _ctuSitePageFactory.GetPage<ILoginPage>();
        try
        {
            await loginPage.LoginAsync(username, password);
            return OperationResult.Success();
        }
        catch (OperationCanceledException)
        {
            // throw;
            return OperationResult.Failed("Hủy đăng nhập", kind:OperationFailureReason.UserAction);
        }
        catch (NoInternetException)
        {
            return OperationResult.Failed("Không có kết nối mạng!", kind:OperationFailureReason.Network);
        }
        catch (InvalidOperationException)
        {
            return OperationResult.Failed("Trang đăng nhập chưa sẵn sàng", kind:OperationFailureReason.Unauthorized);
        }
        catch (TimeoutException)
        {
            return OperationResult.Failed("Quá thời gian phản hồi từ hệ thống!", kind:OperationFailureReason.Network);
        }
        catch (InvalidCredentialsException ex)
        {
            return OperationResult.Failed(ex.Message, kind:OperationFailureReason.Validation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to login");
            return OperationResult.Failed("Vấn đề chưa xác định, Bạn hãy liên hệ với nhà phát triển để tìm cách khắc phục",kind:OperationFailureReason.System);
        }
    }
    
}