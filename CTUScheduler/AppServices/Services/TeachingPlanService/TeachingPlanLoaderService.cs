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
        _announcementService = announcementService;
        _pdfService = pdfService;
        _logger = logger;
    }

    public async Task<OperationResult<TeachingPlanData>> LoadLatestAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting to load latest teaching plan...");

            //  Lấy danh sách thông báo dạng JSON qua HttpClient
            var announcementResult = await _announcementService.FetchAnnouncementsAsync(cancellationToken);
            if (announcementResult.IsFailed || announcementResult.Content is null ||
                announcementResult.Content.Count == 0)
            {
                return OperationResult<TeachingPlanData>.Failed(
                    new OperationError("TeachingPlan.NotificationsMissing",
                        "Không lấy được danh sách thông báo từ hệ thống trường"),
                    OperationFailureReason.Network);
            }

            //  Định vị thông báo kế hoạch giảng dạy chính và thông báo đóng website phụ
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

            _logger.LogDebug("Found latest teaching plan announcement: '{Title}' at index {Index}", target.Title,
                targetIndex);

            // Tìm kiếm thông báo đóng website phụ xuất hiện sau (có chỉ số index nhỏ hơn trong danh sách sắp xếp giảm dần)
            SchoolAnnouncement? closingTarget = null;
            for (int i = 0; i < targetIndex; i++)
            {
                var item = announcementResult.Content[i];
                if (!string.IsNullOrWhiteSpace(item.Title) &&
                    item.Title.Contains("đóng website", StringComparison.OrdinalIgnoreCase) &&
                    item.Title.Contains("kế hoạch học tập", StringComparison.OrdinalIgnoreCase))
                {
                    closingTarget = item;
                    break;
                }
            }

            string? mainPdfPath;
            DateTime? preciseClosingDateTime = null;

            if (closingTarget is not null)
            {
                _logger.LogDebug("Detected matching closing notice: '{Title}'. Launching parallel downloads.",
                    closingTarget.Title);

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
                    var closingPdfPath = closingResult.Content;
                    _logger.LogDebug("Extracting precise closing date/time from secondary notice PDF...");
                    preciseClosingDateTime = await _pdfService.ExtractClosingNoticeDateTimeAsync(closingPdfPath);
                }
            }
            else
            {
                _logger.LogDebug("No matching closing notice found. Downloading main plan PDF only.");

                var downloadResult = await _pdfService.DownloadPdfAsync(target.Link, cancellationToken);
                if (downloadResult.IsFailed || string.IsNullOrWhiteSpace(downloadResult.Content))
                {
                    return OperationResult<TeachingPlanData>.FailureFrom(downloadResult);
                }

                mainPdfPath = downloadResult.Content;
            }

            _logger.LogDebug("Parsing main teaching plan PDF...");
            var data = await _pdfService.ExtractTeachingPlanAsync(mainPdfPath, preciseClosingDateTime);

            if (data.RegistrationTimeline.Count == 0)
            {
                return OperationResult<TeachingPlanData>.Failed(
                    new OperationError("TeachingPlan.ExtractFailed",
                        "Không thể trích xuất dữ liệu từ file PDF kế hoạch"),
                    OperationFailureReason.System);
            }

            data = data with { PdfUrl = target.Link };

            _logger.LogInformation(
                "Successfully parsed teaching plan with {TimelineCount} timeline nodes and {DetailCount} adjustment groups.",
                data.RegistrationTimeline.Count, data.AdjustmentDetails.Count);
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
}