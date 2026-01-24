using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared.Results;

namespace CTUScheduler.Infrastructure.Exel;

public interface IExcelExporterService  // Giao diện dịch vụ — API duy nhất mà các ViewModel/Presentation gọi
{
    // ExportAsync: Ghi ra Stream (thích hợp cho test). Trả OperationResult<string> với đường dẫn hoặc thông tin.
    Task<OperationResult<string>> ExportAsync<T>(
        IEnumerable<T> data,
        Stream output,
        IEnumerable<ExportColumnDefinition<T>>? columns = null,
        ExcelExportOptions? options = null,
        CancellationToken cancellationToken = default);

    // ExportToFileAsync: ghi trực tiếp tới filePath (ổ cứng vậy lý)
    Task<OperationResult<string>> ExportToFileAsync<T>(
        IEnumerable<T> data,
        string filePath,
        IEnumerable<ExportColumnDefinition<T>>? columns = null,
        ExcelExportOptions? options = null,
        CancellationToken cancellationToken = default);
}