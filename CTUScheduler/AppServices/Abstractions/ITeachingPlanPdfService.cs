using System;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Core.Models.TeachingPlan;

namespace CTUScheduler.AppServices.Abstractions;

public interface ITeachingPlanPdfService
{
    Task<OperationResult<string>> DownloadPdfAsync(
        string pdfUrl,
        CancellationToken cancellationToken = default,
        TimeSpan? timeout = null);
        
    Task<TeachingPlanData> ExtractTeachingPlanAsync(string filePath);
}
