using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.DriverCore.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Extensions;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Services.Registration;

public class TuitionFeeService : ITuitionFeeService
{
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);
    private readonly IWebDriverService _webDriverService;
    private readonly ICtuPageFactory _factory;
    private readonly ILogger<TuitionFeeService> _logger;

    public TuitionFeeService(IWebDriverService webDriverService, ICtuPageFactory factory,
        ILogger<TuitionFeeService> logger)
    {
        _webDriverService = webDriverService;
        _factory = factory;
        _logger = logger;
    }

    public async Task<OperationResult<TuitionFeeSummary>> FetchTuitionFeeAsync(
        CancellationToken cancellationToken = default, TimeSpan? timeout = null)
    {
        var finalTimeout = timeout ?? _defaultTimeout;

        try
        {
            await using var tab = await _webDriverService.CreateTabAsync();
            var page = _factory.GetPage<ISchedulePage>(tab);

            var connectableStream = page.TuitionFeeResponse
                .Select(payload => payload.ToSummary())
                .Timeout(finalTimeout)
                .Replay(1);

            var isPageReady = await this.ExecuteNavigationAsync(tab);
            if (isPageReady.IsFailed)
                return OperationResult<TuitionFeeSummary>.FailureFrom(isPageReady);

            using var dataSubscription = connectableStream.Connect();

            await page.NavigateToTuitionFeeAsync();
            
            var tuitionFeeSummary = await connectableStream.FirstAsync().ToTask(cancellationToken);
            if (tuitionFeeSummary is null)
                return OperationResult<TuitionFeeSummary>.Failed("Lấy được thông tin học phí thất bại!",
                    kind: OperationFailureReason.System);
            
            return tuitionFeeSummary;
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Timeout waiting for tuition fee response.");
            return OperationResult<TuitionFeeSummary>.Failed(
                "Quá thời gian phản hồi từ hệ thống đăng ký!",
                kind: OperationFailureReason.Network);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail while fetching tuition fee data");
            return OperationResult<TuitionFeeSummary>.Failed("Lấy thông tin học phần không thành công!");
        }
    }
    
    private async Task<OperationResult> ExecuteNavigationAsync(IWebTab tab)
    {
        try
        {
            var page = _factory.GetPage<ISchedulePage>(tab);

            await page.NavigateToAsync();
            await page.WaitForReadyAsync();
            await page.CheckSessionAndThrowAsync();

            return OperationResult.Success();
        }
        catch (NoInternetException ex)
        {
            return OperationResult.Failed(ex.Message, kind: OperationFailureReason.Network);
        }
        catch (InvalidOperationException ex)
        {
            return OperationResult.Failed(ex.Message, kind: OperationFailureReason.Network);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout while navigate schedule page status");
            return OperationResult.Failed("Quá thời gian phản hồi từ hệ thống!", kind: OperationFailureReason.Network);
        }
        catch (SessionExpiredException ex)
        {
            return OperationResult.Failed(ex.Message, kind: OperationFailureReason.Unauthorized);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while navigate to schedule page status");
            return OperationResult.FromException(ex, "Lỗi hệ thống chưa xác định!",
                kind: OperationFailureReason.System);
        }
    }
}