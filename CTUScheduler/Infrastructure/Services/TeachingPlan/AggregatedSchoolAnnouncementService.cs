using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Core.Models.Academic;
using CTUScheduler.Core.Models.Shared.Results;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Services.TeachingPlan;

public class AggregatedSchoolAnnouncementService : ISchoolAnnouncementService
{
    private readonly HtqlHtmlAnnouncementService _htmlService;
    private readonly HtqlJsonAnnouncementService _jsonService;
    private readonly ILogger<AggregatedSchoolAnnouncementService> _logger;

    public AggregatedSchoolAnnouncementService(
        HtqlHtmlAnnouncementService htmlService,
        HtqlJsonAnnouncementService jsonService,
        ILogger<AggregatedSchoolAnnouncementService> logger)
    {
        ArgumentNullException.ThrowIfNull(htmlService);
        ArgumentNullException.ThrowIfNull(jsonService);
        ArgumentNullException.ThrowIfNull(logger);

        _htmlService = htmlService;
        _jsonService = jsonService;
        _logger = logger;
    }

    public async Task<OperationResult<IReadOnlyList<SchoolAnnouncement>>> FetchAnnouncementsAsync(
        CancellationToken cancellationToken = default)
    {
        var htmlTask = _htmlService.FetchAnnouncementsAsync(cancellationToken);
        var jsonTask = _jsonService.FetchAnnouncementsAsync(cancellationToken);

        try
        {
            await Task.WhenAll(htmlTask, jsonTask);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return OperationResult<IReadOnlyList<SchoolAnnouncement>>.Failed("Tác vụ tổng hợp thông báo đã bị hủy.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Có lỗi xảy ra trong quá trình gọi đồng thời các nguồn thông báo");
        }

        var htmlResult = htmlTask.IsCompletedSuccessfully ? htmlTask.Result : null;
        var jsonResult = jsonTask.IsCompletedSuccessfully ? jsonTask.Result : null;

        var htmlOk = htmlResult is { IsSuccess: true, Content: not null };
        var jsonOk = jsonResult is { IsSuccess: true, Content: not null };

        if (!htmlOk && !jsonOk)
        {
            var htmlError = htmlResult?.FirstErrorMessage ?? "Nguồn HTML không phản hồi";
            var jsonError = jsonResult?.FirstErrorMessage ?? "Nguồn JSON không phản hồi";
            _logger.LogError("Cả 2 nguồn thông báo HTML và JSON đều thất bại. HTML: {HtmlErr}, JSON: {JsonErr}",
                htmlError, jsonError);

            var isCaptcha = htmlResult?.Errors.Any(e => e.Code == HtqlHtmlAnnouncementService.CaptchaErrorCode) == true ||
                            jsonResult?.Errors.Any(e => e.Code == HtqlJsonAnnouncementService.CaptchaErrorCode) == true;

            if (isCaptcha)
            {
                return OperationResult<IReadOnlyList<SchoolAnnouncement>>.Failed(
                    new OperationError(HtqlHtmlAnnouncementService.CaptchaErrorCode, HtqlHtmlAnnouncementService.CaptchaErrorMessage),
                    OperationFailureReason.Network);
            }

            return OperationResult<IReadOnlyList<SchoolAnnouncement>>.Failed(
                "Không thể tải thông báo từ tất cả các nguồn.");
        }

        var combined = new List<SchoolAnnouncement>();
        if (htmlOk)
        {
            combined.AddRange(htmlResult!.Content!);
        }

        if (jsonOk)
        {
            combined.AddRange(jsonResult!.Content!);
        }

        var distinct = combined
            .Where(a => !string.IsNullOrWhiteSpace(a.Link))
            .DistinctBy(a => a.Link.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToList();

        _logger.LogInformation("Tổng hợp thành công {Count} thông báo (HTML: {HtmlCount}, JSON: {JsonCount})",
            distinct.Count,
            htmlOk ? htmlResult!.Content!.Count : 0,
            jsonOk ? jsonResult!.Content!.Count : 0);

        return OperationResult<IReadOnlyList<SchoolAnnouncement>>.Success(distinct);
    }
}
