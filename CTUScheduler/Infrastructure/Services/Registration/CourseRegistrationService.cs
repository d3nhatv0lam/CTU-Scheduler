using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.DriverCore.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Extensions;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Services.Registration;

public class CourseRegistrationService : ICourseRegistrationService
{
    private readonly IWebDriverService _webDriverService;
    private readonly ILogger<CourseRegistrationService> _logger;
    private readonly ICtuPageFactory _factory;
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(15);

    public CourseRegistrationService(IWebDriverService webDriverService, ICtuPageFactory factory,
        ILogger<CourseRegistrationService> logger)
    {
        _webDriverService = webDriverService;
        _factory = factory;
        _logger = logger;
    }

    public Task<OperationResult> EnsureReadyAsync()
    {
        return Task.FromResult(OperationResult.Success());
    }

    public async Task<OperationResult<List<PlannedCourse>>> FetchPlannedCourseAsync(TimeSpan? timeout = null,
        CancellationToken token = default)
    {
        var finalTimeout = timeout ?? _defaultTimeout;
        try
        {
            await using var tab = await _webDriverService.CreateTabAsync();
            var page = _factory.GetPage<ICourseRegistrationPage>(tab);

            var connectableStream = page.CourseRegistrationResponse
                .Select(payloads => payloads.Select(x => x.ToPlannedCourse()).ToList())
                .Timeout(finalTimeout)
                .Replay(1);

            using var dataSubscription = connectableStream.Connect();

            var isPageReady = await this.ExecuteNavigationAsync(tab);
            if (isPageReady.IsFailed)
                return OperationResult<List<PlannedCourse>>.FailureFrom(isPageReady);

            var plannedCourses = await connectableStream.FirstAsync().ToTask(token);
            return plannedCourses;
        }
        catch (InvalidDataException ex)
        {
            // Lỗi do JSON map thiếu trường bắt buộc (ném từ hàm ToPlannedCourse)
            _logger.LogWarning(ex, "API trả về dữ liệu môn học lỗi hoặc thiếu.");
            return OperationResult<List<PlannedCourse>>.Failed(
                "Dữ liệu môn học từ trường không hợp lệ. Vui lòng thử lại.",
                code: "Mapping.InvalidCourseData",
                kind: OperationFailureReason.System);
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Timeout waiting for registration response.");
            return OperationResult<List<PlannedCourse>>.Failed(
                "Quá thời gian phản hồi từ hệ thống đăng ký!",
                kind: OperationFailureReason.Network);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail while fetching course registration data");
            return OperationResult<List<PlannedCourse>>.Failed("Lấy thông tin học phần không thành công!");
        }
    }

    private async Task<OperationResult> ExecuteNavigationAsync(IWebTab tab)
    {
        try
        {
            var page = _factory.GetPage<ICourseRegistrationPage>(tab);

            await page.NavigateToAsync();
            await page.WaitForReadyAsync();
            await page.CheckSessionAndThrowAsync();

            return OperationResult.Success();
        }
        catch (InvalidOperationException ex)
        {
            return OperationResult.Failed(ex.Message, kind: OperationFailureReason.Network);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout while checking course registration page status");
            return OperationResult.Failed("Quá thời gian phản hồi từ hệ thống!", kind: OperationFailureReason.Network);
        }
        catch (SessionExpiredException ex)
        {
            return OperationResult.Failed(ex.Message, kind: OperationFailureReason.Unauthorized);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while checking course registration page status");
            return OperationResult.FromException(ex, "Lỗi hệ thống chưa xác định!",
                kind: OperationFailureReason.System);
        }
    }
}