using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Core.Models.TeachingPlan;

namespace CTUScheduler.AppServices.Services.TeachingPlanService;

public class TeachingPlanLoaderService : ITeachingPlanLoaderService
{
    private const string TeachingPlanKeyword = "kế hoạch giảng dạy";
    private readonly ISchoolAnnouncementService _announcementService;
    private readonly ITeachingPlanPdfService _pdfService;

    public TeachingPlanLoaderService(
        ISchoolAnnouncementService announcementService,
        ITeachingPlanPdfService pdfService)
    {
        _announcementService = announcementService ?? throw new ArgumentNullException(nameof(announcementService));
        _pdfService = pdfService ?? throw new ArgumentNullException(nameof(pdfService));
    }

    public async Task<OperationResult<TeachingPlanData>> LoadLatestAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Gọi trực tiếp API lấy thông báo dạng JSON qua HttpClient (truyền token hủy)
            var announcementResult = await _announcementService.FetchAnnouncementsAsync(cancellationToken);
            if (announcementResult.IsFailed || announcementResult.Content is null || announcementResult.Content.Count == 0)
            {
                return OperationResult<TeachingPlanData>.Failed(
                    new OperationError("TeachingPlan.NotificationsMissing", "Không lấy được danh sách thông báo từ hệ thống trường"),
                    OperationFailureReason.Network);
            }
            
            // 2. Logic lọc tìm kiếm kế hoạch giảng dạy
            var target = announcementResult.Content.FirstOrDefault(item =>
                !string.IsNullOrWhiteSpace(item.Title) &&
                item.Title.Contains(TeachingPlanKeyword, StringComparison.OrdinalIgnoreCase));

            if (target is null)
            {
                return OperationResult<TeachingPlanData>.Failed(
                    new OperationError("TeachingPlan.NotFound", "Không tìm thấy thông báo kế hoạch giảng dạy phù hợp"),
                    OperationFailureReason.NotFound);
            }

            // 3. Tải PDF về máy thông qua HttpClient (truyền token hủy)
            var downloadResult = await _pdfService.DownloadPdfAsync(target.Link, cancellationToken);
            if (downloadResult.IsFailed || string.IsNullOrWhiteSpace(downloadResult.Content))
            {
                return OperationResult<TeachingPlanData>.FailureFrom(downloadResult);
            }

            // 4. Trích xuất thời khóa biểu học phần từ PDF thô (CPU-bound chạy offline)
            var data = await _pdfService.ExtractTeachingPlanAsync(downloadResult.Content);
            if (data is null || data.RegistrationTimeline.Count == 0)
            {
                return OperationResult<TeachingPlanData>.Failed(
                    new OperationError("TeachingPlan.ExtractFailed", "Không thể trích xuất dữ liệu từ file PDF kế hoạch"),
                    OperationFailureReason.System);
            }

            return OperationResult<TeachingPlanData>.Success(data);
        }
        catch (OperationCanceledException)
        {
            return OperationResult<TeachingPlanData>.Failed(
                new OperationError("TeachingPlan.Canceled", "Tác vụ nạp kế hoạch giảng dạy đã bị hủy."),
                OperationFailureReason.System);
        }
        catch (Exception ex)
        {
            return OperationResult<TeachingPlanData>.FromException(
                ex,
                "Có lỗi xảy ra trong quá trình xử lý nạp kế hoạch giảng dạy",
                "TeachingPlan.Unexpected",
                OperationFailureReason.System);
        }
    }
}