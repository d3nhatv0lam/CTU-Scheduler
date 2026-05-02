using System;
using System.Linq;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Models;

namespace CTUScheduler.AppServices.Services.TeachingPlanService;

public class TeachingPlanLoaderService : ITeachingPlanLoaderService
{
    private const string TeachingPlanKeyword = "kế hoạch giảng dạy";
    private readonly ITeachingPlanResourceService _resourceService;

    public TeachingPlanLoaderService(ITeachingPlanResourceService resourceService)
    {
        _resourceService = resourceService ?? throw new ArgumentNullException(nameof(resourceService));
    }

    public async Task<TeachingPlanLoadResult> LoadLatestAsync()
    {
        try
        {
            var notifications = await _resourceService.GetNotificationsAsync();
            if (notifications is null || notifications.Count == 0)
            {
                return TeachingPlanLoadResult.Failed("Không lấy được thông báo");
            }
            
            var target = notifications.FirstOrDefault(item =>
                !string.IsNullOrWhiteSpace(item.Title) &&
                item.Title.IndexOf(TeachingPlanKeyword, StringComparison.CurrentCultureIgnoreCase) >= 0);

            if (target is null)
            {
                return TeachingPlanLoadResult.Failed("Không có kế hoạch giảng dạy");
            }

            if (string.IsNullOrWhiteSpace(target.PdfUrl))
            {
                return TeachingPlanLoadResult.Failed("Không tải được file PDF");
            }

            var filePath = await _resourceService.DownloadPdfAsync(target.PdfUrl);
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return TeachingPlanLoadResult.Failed("Không tải được file PDF");
            }

            var data = await _resourceService.ExtractTeachingPlanAsync(filePath);
            if (data is null)
            {
                return TeachingPlanLoadResult.Failed("Không đọc được nội dung file");
            }

            data.Title = string.IsNullOrWhiteSpace(data.Title) ? target.Title : data.Title;
            return TeachingPlanLoadResult.Success(data);
        }
        catch
        {
            return TeachingPlanLoadResult.Failed("Không đọc được nội dung file");
        }
    }
}