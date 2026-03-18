using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Shared.Results;
using System.Threading.Tasks;

namespace CTUScheduler.Infrastructure.Exel;

public interface IExcelExporterService
{
    Task<OperationResult<string>> ExportTimetableAsync(ScheduleBlueprint blueprint, string filePath);
}