using System;
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

public class TuitionFeeService : ITuitionFeeService
{
    private readonly ICourseRegistrationClient _client;
    private readonly ITuitionFeeStore _tuitionFeeStore;
    private readonly IUserSessionService _userSessionService;
    private readonly ILogger<TuitionFeeService> _logger;

    public TuitionFeeService(ICourseRegistrationClient client,
        ITuitionFeeStore tuitionFeeStore,
        IUserSessionService userSessionService,
        ILogger<TuitionFeeService> logger)
    {
        _client = client;
        _tuitionFeeStore = tuitionFeeStore;
        _userSessionService = userSessionService;
        _logger = logger;
    }

    public async Task<OperationResult> RefreshTuitionFeeAsync(
        int? academicYear = null,
        int? semester = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var resolvedYear = academicYear;
            var resolvedSemester = semester;

            if (resolvedYear is null || resolvedSemester is null)
            {
                var context = _userSessionService.CurrentContext;
                if (context is not null)
                {
                    resolvedYear = context.AcademicYear;
                    resolvedSemester = context.Semester;
                }
            }

            if (resolvedYear is null || resolvedSemester is null)
            {
                _logger.LogWarning("Năm học hoặc học kỳ không hợp lệ: năm - {year}, học kỳ - {semester}.", resolvedYear,
                    resolvedSemester);
                return OperationResult.Failed(
                    "Không thể xác định Năm học hoặc Học kỳ. Vui lòng đăng nhập hoặc chọn một học kỳ hợp lệ.",
                    kind: OperationFailureReason.Validation
                );
            }

            var rawTuitionFee = await _client.GetTuitionFeeRawAsync(
                resolvedYear.Value,
                resolvedSemester.Value,
                cancellationToken);

            var summary = rawTuitionFee.ToSummary(_logger);

            if (summary is null)
            {
                return OperationResult.Failed(
                    $"Không thể phân tích thông tin học phí.",
                    "TuitionFee.MappingError",
                    OperationFailureReason.System);
            }

            _tuitionFeeStore.Update(summary);
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
                _logger.LogDebug("Dừng đồng bộ học phí bởi người dùng.");
                return OperationResult.Failed("Đã hủy yêu cầu.", kind: OperationFailureReason.UserAction);
            }

            _logger.LogWarning(ex, "Yêu cầu bị quá thời gian (Timeout).");
            return OperationResult.Failed(
                "Thời gian kết nối đến máy chủ trường quá lâu. Vui lòng thử lại.",
                kind: OperationFailureReason.Network
            );
        }
        catch (CtuDataContractException ex)
        {
            _logger.LogError(ex, "Cấu trúc dữ liệu học phí trả về từ CTU không hợp lệ.");
            return OperationResult.Failed(
                "Lỗi đồng bộ dữ liệu trường. Vui lòng thử lại sau.",
                kind: OperationFailureReason.System
            );
        }
        catch (CtuApiException ex)
        {
            _logger.LogWarning(ex, "Lỗi từ hệ thống CTU khi tải thông tin học phí: {Message}", ex.Message);
            return OperationResult.Failed(ex.Message, kind: OperationFailureReason.System);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không xác định khi Refresh Tuition Fee");
            return OperationResult.FromException(
                ex,
                "Đồng bộ thông tin học phí thất bại do lỗi hệ thống chưa xác định.",
                kind: OperationFailureReason.System
            );
        }
    }
}