using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Services.UserSessionService;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Shared.Results;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.StudentAcademicService;

public class GradeService : IGradeService
{
    private readonly IAcademicGradeClient _academicGradeClient;
    private readonly IGradeStore _gradeStore;
    private readonly ILogger<GradeService> _logger;

    public GradeService(
        IAcademicGradeClient academicGradeClient,
        IGradeStore gradeStore,
        ILogger<GradeService> logger)
    {
        ArgumentNullException.ThrowIfNull(academicGradeClient);
        ArgumentNullException.ThrowIfNull(gradeStore);
        ArgumentNullException.ThrowIfNull(logger);

        _academicGradeClient = academicGradeClient;
        _gradeStore = gradeStore;
        _logger = logger;
    }

    public async Task<OperationResult> RefreshGradesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var transcript = await _academicGradeClient.FetchTranscriptAsync(cancellationToken);
            _gradeStore.Update(transcript);
            return OperationResult.Success();
        }
        catch (SessionExpiredException ex)
        {
            _logger.LogWarning(ex, "Phiên làm việc đã hết hạn khi tải bảng điểm.");
            return OperationResult.Failed(
                "Phiên đăng nhập của bạn đã hết hạn trên hệ thống trường. Vui lòng đăng nhập lại.",
                "Grades.Unauthorized",
                OperationFailureReason.Unauthorized);
        }
        catch (CaptchaBlockedException ex)
        {
            _logger.LogWarning(ex, "Bị chặn Captcha khi tải bảng điểm CTU.");
            return OperationResult.Failed(
                ex.Message,
                "Grades.CaptchaBlocked",
                OperationFailureReason.Network);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Lỗi kết nối mạng khi tải bảng điểm.");
            return OperationResult.Failed(
                "Không có kết nối Internet hoặc máy chủ trường không phản hồi.",
                "Grades.NetworkError",
                OperationFailureReason.Network);
        }
        catch (OperationCanceledException ex)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Dừng đồng bộ bảng điểm bởi người dùng.");
                return OperationResult.Failed(
                    "Đã hủy yêu cầu tải bảng điểm.",
                    "Grades.Canceled",
                    OperationFailureReason.UserAction);
            }

            _logger.LogWarning(ex, "Yêu cầu tải bảng điểm bị quá thời gian (Timeout).");
            return OperationResult.Failed(
                "Thời gian kết nối đến máy chủ trường quá lâu. Vui lòng thử lại.",
                "Grades.Timeout",
                OperationFailureReason.Network);
        }
        catch (InvalidDataException ex)
        {
            _logger.LogError(ex, "Dữ liệu HTML bảng điểm trả về từ CTU không hợp lệ.");
            return OperationResult.Failed(
                "Dữ liệu bảng điểm trả về từ trường không hợp lệ. Vui lòng thử lại sau.",
                "Grades.InvalidData",
                OperationFailureReason.System);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không xác định khi tải bảng điểm.");
            return OperationResult.FromException(
                ex,
                "Đồng bộ bảng điểm thất bại do lỗi hệ thống chưa xác định.",
                "Grades.Unexpected",
                OperationFailureReason.System);
        }
    }
}
