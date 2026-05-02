using System.Collections.Generic;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Models;
using CTUScheduler.Core.Models.TeachingPlan;

namespace CTUScheduler.AppServices.Abstractions;

public interface ITeachingPlanResourceService
{
    Task<IReadOnlyList<NotificationItem>> GetNotificationsAsync();
    Task<string> DownloadPdfAsync(string pdfUrl);
    Task<TeachingPlanData> ExtractTeachingPlanAsync(string filePath);
}

