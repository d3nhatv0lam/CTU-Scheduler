using System;
using System.Linq;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Core.Models.TeachingPlan;

namespace CTUScheduler.AppServices.Services.TeachingPlanService;

public class TeachingPlanLoaderService : ITeachingPlanLoaderService
{
    private const string TeachingPlanKeyword = "kế hoạch giảng dạy";
    private readonly ITeachingPlanResourceService _resourceService;

    public TeachingPlanLoaderService(ITeachingPlanResourceService resourceService)
    {
        _resourceService = resourceService ?? throw new ArgumentNullException(nameof(resourceService));
    }

    public async Task<OperationResult<TeachingPlanData>> LoadLatestAsync()
    {
        try
        {
            var notifications = await _resourceService.GetNotificationsAsync();
            if (notifications is null || notifications.Count == 0)
            {
                return OperationResult<TeachingPlanData>.Failed(
                    new OperationError("TeachingPlan.NotificationsMissing", "Không lấy được thông báo"),
                    OperationFailureReason.Network);
            }
            
            var target = notifications.FirstOrDefault(item =>
                !string.IsNullOrWhiteSpace(item.Title) &&
                item.Title.IndexOf(TeachingPlanKeyword, StringComparison.CurrentCultureIgnoreCase) >= 0);

            if (target is null)
            {
                return OperationResult<TeachingPlanData>.Failed(
                    new OperationError("TeachingPlan.NotFound", "Không có kế hoạch giảng dạy"),
                    OperationFailureReason.NotFound);
            }

            if (string.IsNullOrWhiteSpace(target.PdfUrl))
            {
                return OperationResult<TeachingPlanData>.Failed(
                    new OperationError("TeachingPlan.PdfMissing", "Không tải được file PDF"),
                    OperationFailureReason.System);
            }

            var filePath = await _resourceService.DownloadPdfAsync(target.PdfUrl);
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return OperationResult<TeachingPlanData>.Failed(
                    new OperationError("TeachingPlan.DownloadFailed", "Không tải được file PDF"),
                    OperationFailureReason.Network);
            }

            var data = await _resourceService.ExtractTeachingPlanAsync(filePath);
            if (data is null)
            {
                return OperationResult<TeachingPlanData>.Failed(
                    new OperationError("TeachingPlan.ExtractFailed", "Không đọc được nội dung file"),
                    OperationFailureReason.System);
            }

            data.Title = string.IsNullOrWhiteSpace(data.Title) ? target.Title : data.Title;
            return OperationResult<TeachingPlanData>.Success(data);
        }
        catch (Exception ex)
        {
            return OperationResult<TeachingPlanData>.FromException(
                ex,
                "Không đọc được nội dung file",
                "TeachingPlan.Unexpected",
                OperationFailureReason.System);
        }
    }
}