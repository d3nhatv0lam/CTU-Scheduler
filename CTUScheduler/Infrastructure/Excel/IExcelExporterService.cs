using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Shared.Results;

namespace CTUScheduler.Infrastructure.Excel;

public interface IExcelExporterService
{
    Task<OperationResult<string>> ExportTimetableAsync(ScheduleBlueprint blueprint, string filePath);
}