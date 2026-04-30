using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Shared.Results;
using System.Collections.Generic;

namespace CTUScheduler.Infrastructure.Excel;

public interface IExcelExporterService
{
    Task<OperationResult<string>> ExportTimetableAsync(ScheduleBlueprint blueprint, string filePath);
    Task<OperationResult<string>> ExportTimetablesAsync(IEnumerable<(ScheduleBlueprint Blueprint, string SheetName)> timetables, string filePath);
}