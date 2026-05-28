using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Services.UserSessionService;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Extensions;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.StudentAcademicService;

public class CourseRegistrationService : ICourseRegistrationRefactorService
{
    private readonly ICourseRegistrationClient _client;
    private readonly IPlannedCourseStore _plannedCourseStore;
    private readonly ILogger<CourseRegistrationService> _logger;

    public CourseRegistrationService(ICourseRegistrationClient client,
        IPlannedCourseStore plannedCourseStore,
        ILogger<CourseRegistrationService> logger)
    {
        _client = client;
        _plannedCourseStore = plannedCourseStore;
        _logger = logger;
    }

    public async Task<OperationResult> RefreshPlannedCourseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var rawPlannedCourses = await _client.GetPlannedCoursesRawAsync(cancellationToken);

            var plannedCourses = rawPlannedCourses
                .Select(x => x.ToPlannedCourse())
                .ToList();

            _plannedCourseStore.Update(plannedCourses);
            return OperationResult.Success();
        }
        catch (SessionExpiredException ex)
        {
            _logger.LogWarning(ex, "Phiên làm việc đã hết hạn.");
            return OperationResult.Failed(
                "Phiên đăng ký của bạn đã hết hạn trên hệ thống trường. Vui lòng đăng nhập lại.",
                kind: OperationFailureReason.Unauthorized
            );
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Lỗi kết nối mạng hoặc không có Internet.");
            return OperationResult.Failed(
                "Không có kết nối Internet hoặc máy chủ trường không phản hồi.",
                kind: OperationFailureReason.Network
            );
        }
        catch (OperationCanceledException ex)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Tác vụ bị hủy bởi người dùng.");
                return OperationResult.Failed("Đã hủy quá trình đồng bộ.", kind: OperationFailureReason.UserAction);
            }

            _logger.LogWarning(ex, "Yêu cầu bị quá thời gian (Timeout).");
            return OperationResult.Failed(
                "Thời gian kết nối đến máy chủ trường quá lâu. Vui lòng thử lại.",
                kind: OperationFailureReason.Network
            );
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Dữ liệu trả về từ CTU không thể phân rã.");
            return OperationResult.Failed(
                "Hệ thống không thể phân tích dữ liệu của trường. Nhà trường có thể đã cập nhật API mới.",
                kind: OperationFailureReason.System
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không xác định khi Refresh Registration");
            return OperationResult.FromException(
                ex,
                "Đồng bộ học phần của sinh viên thất bại do lỗi hệ thống chưa xác định.",
                kind: OperationFailureReason.System
            );
        }
    }
}