using System;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared.Results;

namespace CTUScheduler.AppServices.Abstractions;

public interface ITeachingPlanPdfService
{
    Task<OperationResult<string>> DownloadPdfAsync(
        string pdfUrl,
        CancellationToken cancellationToken = default,
        TimeSpan? timeout = null);

    /// <summary>
    /// Lấy đường dẫn file cục bộ đã được cache tương ứng với link từ xa.
    /// </summary>
    string GetCachedPdfPath(string pdfUrl);
}
