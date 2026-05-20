using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Core.Models.Academic;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Core.Models.TeachingPlan;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.TeachingPlanService;

public class TeachingPlanLoaderService : ITeachingPlanLoaderService
{
    private const string TeachingPlanKeyword = "kế hoạch giảng dạy";
    private readonly ISchoolAnnouncementService _announcementService;
    private readonly ITeachingPlanPdfService _pdfService;
    private readonly ILogger<TeachingPlanLoaderService> _logger;

    public TeachingPlanLoaderService(
        ISchoolAnnouncementService announcementService,
        ITeachingPlanPdfService pdfService,
        ILogger<TeachingPlanLoaderService> logger)
    {
        _announcementService = announcementService ?? throw new ArgumentNullException(nameof(announcementService));
        _pdfService = pdfService ?? throw new ArgumentNullException(nameof(pdfService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OperationResult<TeachingPlanData>> LoadLatestAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting to load latest teaching plan...");

            // 1. Lấy danh sách thông báo dạng JSON qua HttpClient
            var announcementResult = await _announcementService.FetchAnnouncementsAsync(cancellationToken);
            if (announcementResult.IsFailed || announcementResult.Content is null || announcementResult.Content.Count == 0)
            {
                return OperationResult<TeachingPlanData>.Failed(
                    new OperationError("TeachingPlan.NotificationsMissing", "Không lấy được danh sách thông báo từ hệ thống trường"),
                    OperationFailureReason.Network);
            }
            
            // 2. Định vị thông báo kế hoạch giảng dạy chính và thông báo đóng website phụ
            SchoolAnnouncement? target = null;
            int targetIndex = -1;

            for (int i = 0; i < announcementResult.Content.Count; i++)
            {
                var item = announcementResult.Content[i];
                if (!string.IsNullOrWhiteSpace(item.Title) &&
                    item.Title.Contains(TeachingPlanKeyword, StringComparison.OrdinalIgnoreCase))
                {
                    target = item;
                    targetIndex = i;
                    break;
                }
            }

            if (target is null)
            {
                return OperationResult<TeachingPlanData>.Failed(
                    new OperationError("TeachingPlan.NotFound", "Không tìm thấy thông báo kế hoạch giảng dạy phù hợp"),
                    OperationFailureReason.NotFound);
            }

            _logger.LogInformation("Found latest teaching plan announcement: '{Title}' at index {Index}", target.Title, targetIndex);

            // Tìm kiếm thông báo đóng website phụ xuất hiện sau (có chỉ số index nhỏ hơn trong danh sách sắp xếp giảm dần)
            SchoolAnnouncement? closingTarget = null;
            for (int i = 0; i < targetIndex; i++)
            {
                var item = announcementResult.Content[i];
                if (!string.IsNullOrWhiteSpace(item.Title) &&
                    (item.Title.Contains("đóng website", StringComparison.OrdinalIgnoreCase) ||
                     item.Title.Contains("đóng cổng", StringComparison.OrdinalIgnoreCase) ||
                     item.Title.Contains("dừng nhận", StringComparison.OrdinalIgnoreCase)) &&
                    item.Title.Contains("điều chỉnh", StringComparison.OrdinalIgnoreCase))
                {
                    // Thực hiện kiểm tra ngữ nghĩa học kỳ và năm học
                    if (AreAnnouncementsSemanticallyMatched(target.Title, item.Title))
                    {
                        closingTarget = item;
                        break;
                    }
                }
            }

            string? mainPdfPath = null;
            string? closingPdfPath = null;
            DateTime? preciseClosingDateTime = null;

            if (closingTarget != null)
            {
                _logger.LogInformation("Detected matching closing notice: '{Title}'. Launching parallel downloads.", closingTarget.Title);

                // Tải song song cả 2 file PDF để tối đa hóa hiệu năng
                var mainDownloadTask = _pdfService.DownloadPdfAsync(target.Link, cancellationToken);
                var closingDownloadTask = _pdfService.DownloadPdfAsync(closingTarget.Link, cancellationToken);

                await Task.WhenAll(mainDownloadTask, closingDownloadTask);

                var mainResult = await mainDownloadTask;
                var closingResult = await closingDownloadTask;

                if (mainResult.IsFailed || string.IsNullOrWhiteSpace(mainResult.Content))
                {
                    return OperationResult<TeachingPlanData>.FailureFrom(mainResult);
                }
                mainPdfPath = mainResult.Content;

                if (closingResult.IsSuccess && !string.IsNullOrWhiteSpace(closingResult.Content))
                {
                    closingPdfPath = closingResult.Content;
                    _logger.LogInformation("Extracting precise closing date/time from secondary notice PDF...");
                    preciseClosingDateTime = await _pdfService.ExtractClosingNoticeDateTimeAsync(closingPdfPath);
                }
            }
            else
            {
                _logger.LogInformation("No matching closing notice found. Downloading main plan PDF only.");
                
                var downloadResult = await _pdfService.DownloadPdfAsync(target.Link, cancellationToken);
                if (downloadResult.IsFailed || string.IsNullOrWhiteSpace(downloadResult.Content))
                {
                    return OperationResult<TeachingPlanData>.FailureFrom(downloadResult);
                }
                mainPdfPath = downloadResult.Content;
            }

            // 4. Trích xuất kế hoạch từ PDF và đồng bộ hóa ngày giờ đóng cổng
            _logger.LogInformation("Parsing main teaching plan PDF...");
            var data = await _pdfService.ExtractTeachingPlanAsync(mainPdfPath, preciseClosingDateTime);
            
            if (data is null || data.RegistrationTimeline.Count == 0)
            {
                return OperationResult<TeachingPlanData>.Failed(
                    new OperationError("TeachingPlan.ExtractFailed", "Không thể trích xuất dữ liệu từ file PDF kế hoạch"),
                    OperationFailureReason.System);
            }

            _logger.LogInformation("Successfully parsed teaching plan with {TimelineCount} timeline nodes and {DetailCount} adjustment groups.", data.RegistrationTimeline.Count, data.AdjustmentDetails.Count);
            return OperationResult<TeachingPlanData>.Success(data);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Teaching plan loader task canceled.");
            return OperationResult<TeachingPlanData>.Failed(
                new OperationError("TeachingPlan.Canceled", "Tác vụ nạp kế hoạch giảng dạy đã bị hủy."),
                OperationFailureReason.System);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading teaching plan.");
            return OperationResult<TeachingPlanData>.FromException(
                ex,
                "Có lỗi xảy ra trong quá trình xử lý nạp kế hoạch giảng dạy",
                "TeachingPlan.Unexpected",
                OperationFailureReason.System);
        }
    }

    private static bool AreAnnouncementsSemanticallyMatched(string mainTitle, string closingTitle)
    {
        // 1. Trích xuất các năm học học thuật (4 chữ số như 2025, 2026) từ thông báo chính
        var yearRegex = new Regex(@"\b(20\d{2})\b", RegexOptions.Compiled);
        var mainYears = yearRegex.Matches(mainTitle).Select(m => m.Value).Distinct().ToList();
        
        if (mainYears.Count > 0)
        {
            var closingYears = yearRegex.Matches(closingTitle).Select(m => m.Value).Distinct().ToList();
            // Nếu thông báo chính có năm, thông báo đóng cổng phải khớp ít nhất một năm học
            if (!mainYears.Any(y => closingYears.Contains(y)))
            {
                return false;
            }
        }

        // 2. Kiểm tra học kỳ (HK1, HK2, HK3 hoặc tương tự) để tránh nhầm giữa các học kỳ
        var semesterPatterns = new[] { 
            "học kỳ 1", "hk 1", "hk1", "học kỳ i ", "học kỳ i\b", 
            "học kỳ 2", "hk 2", "hk2", "học kỳ ii ", "học kỳ ii\b", 
            "học kỳ 3", "hk 3", "hk3", "học kỳ iii ", "học kỳ iii\b" 
        };
        
        var mainSemMatch = semesterPatterns.FirstOrDefault(p => mainTitle.Contains(p, StringComparison.OrdinalIgnoreCase));
        if (mainSemMatch != null)
        {
            string GetNormalizedSemester(string title)
            {
                if (title.Contains("học kỳ 1", StringComparison.OrdinalIgnoreCase) || title.Contains("hk 1", StringComparison.OrdinalIgnoreCase) || title.Contains("hk1", StringComparison.OrdinalIgnoreCase) || title.Contains("học kỳ i", StringComparison.OrdinalIgnoreCase))
                    return "HK1";
                if (title.Contains("học kỳ 2", StringComparison.OrdinalIgnoreCase) || title.Contains("hk 2", StringComparison.OrdinalIgnoreCase) || title.Contains("hk2", StringComparison.OrdinalIgnoreCase) || title.Contains("học kỳ ii", StringComparison.OrdinalIgnoreCase))
                    return "HK2";
                if (title.Contains("học kỳ 3", StringComparison.OrdinalIgnoreCase) || title.Contains("hk 3", StringComparison.OrdinalIgnoreCase) || title.Contains("hk3", StringComparison.OrdinalIgnoreCase) || title.Contains("học kỳ iii", StringComparison.OrdinalIgnoreCase))
                    return "HK3";
                return string.Empty;
            }

            var mainSemNorm = GetNormalizedSemester(mainTitle);
            var closingSemNorm = GetNormalizedSemester(closingTitle);
            if (!string.IsNullOrEmpty(mainSemNorm) && !string.IsNullOrEmpty(closingSemNorm) && mainSemNorm != closingSemNorm)
            {
                return false;
            }
        }

        return true;
    }
}