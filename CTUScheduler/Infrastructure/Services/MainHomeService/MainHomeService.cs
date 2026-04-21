using System;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using System.Threading;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.DriverCore.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Services.MainHomeService;

public class MainHomeService: IMainHomeService
{
    private readonly ILogger<MainHomeService> _logger;
    private readonly IWebDriverService _webDriverService;
    private readonly ICtuPageFactory _pageFactory;
    
    public MainHomeService(
        IWebDriverService webDriverService,
        ICtuPageFactory pageFactory,
        ILogger<MainHomeService> logger)
    {
        _logger = logger;
        _webDriverService = webDriverService;
        _pageFactory = pageFactory;
    }
    
    public async Task<OperationResult> EnsureReadyAsync()
    {
        var mainPage = _pageFactory.GetPage<IMainPage>(_webDriverService.MainTab);
        try
        {
            await mainPage.NavigateToAsync();
            await mainPage.WaitForReadyAsync();
            await mainPage.CheckSessionAndThrowAsync();
            return OperationResult.Success();
        }
        catch (SessionExpiredException ex)
        {
            _logger.LogDebug(ex, "Session expired");
            return OperationResult.Failed(ex.Message, kind: OperationFailureReason.Unauthorized);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,"Lỗi không xác định");
            return  OperationResult.Failed("Lỗi hệ thống!", kind: OperationFailureReason.System);
        }
    }
    
    public async Task<string> GetStudentIdAsync(CancellationToken cancellationToken = default)
    {
        var mainPage = _pageFactory.GetPage<IMainPage>(_webDriverService.MainTab);
        if (!await mainPage.IsActiveAsync()) return string.Empty;
        return await mainPage.GetUserInfoAsync(cancellationToken);
    }
}