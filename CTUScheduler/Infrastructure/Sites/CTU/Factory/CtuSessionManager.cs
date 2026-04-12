using System;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.DriverCore.Refactor.Page;
using CTUScheduler.Infrastructure.Sites.Base;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Sites.CTU.Factory;

public class CtuSessionManager: IWebSessionManager
{
    private readonly ILogger<CtuSessionManager> _logger;
    private const string LOGIN_PAGE_KEYWORD = "authenticationendpoint/login.do"; 

    public CtuSessionManager(ILogger<CtuSessionManager> logger)
    {
        _logger = logger;
    }
    
    public async Task<OperationResult> NavigateSafelyAsync(ISitePageRefactor page)
    {
        await page.NavigateToAsync();

        try
        {
            await page.WaitForReadyAsync(timeoutMs: 15000); 
        }
        catch (TimeoutException)
        {
            if (page is IRequireSession && page.CurrentUrl.Contains(LOGIN_PAGE_KEYWORD, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Phát hiện bị đá văng ra trang đăng nhập!");
                return OperationResult.Failed("Phiên đã hết hạn. Vui lòng đăng nhập lại.", "Auth.Expired", OperationFailureReason.Unauthorized);
            }
            
            return OperationResult.Failed($"Trang chưa sẵn sàng.", "Net.Timeout", OperationFailureReason.Network);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi điều hướng trang {Page}", page.GetType().Name);
            return OperationResult.Failed("Lỗi không xác định khi tải trang", kind: OperationFailureReason.System);
        }
        
        if (page is IRequireSession && page.CurrentUrl.Contains(LOGIN_PAGE_KEYWORD, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult.Failed("Phiên đã hết hạn. Vui lòng đăng nhập lại.", "Auth.Expired", OperationFailureReason.Unauthorized);
        }

        return OperationResult.Success();
    }
}