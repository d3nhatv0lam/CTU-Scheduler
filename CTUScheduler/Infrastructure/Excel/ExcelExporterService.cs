using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Shared.Results;

namespace CTUScheduler.Infrastructure.Excel;

public class ExcelExporterService : IExcelExporterService
{
    public async Task<OperationResult<string>> ExportTimetableAsync(ScheduleBlueprint blueprint, string filePath)
    {
        try
        {
            if (!blueprint.IsConsistent)
                return OperationResult<string>.Failed("Dữ liệu Thời khóa biểu không nhất quán.");

            var wb = await Task.Run(() => TimetableExcelBuilder.BuildWorkbook(blueprint));

            var dir = System.IO.Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            await Task.Run(() => wb.SaveAs(filePath));

            // Mở thư mục chứa file
            new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo("explorer.exe", $"/select,\"{filePath}\"")
            }.Start();

            return OperationResult<string>.Success(filePath);
        }
        catch (Exception ex)
        {
            return OperationResult<string>.FromException(ex);
        }
    }

    public async Task<OperationResult<string>> ExportTimetablesAsync(IEnumerable<(ScheduleBlueprint Blueprint, string SheetName)> timetables, string filePath)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(timetables);

            var selectedTimetables = timetables.ToList();
            if (selectedTimetables.Count == 0)
                return OperationResult<string>.Failed("Không có thời khóa biểu nào để xuất.");

            if (selectedTimetables.Any(t => !t.Blueprint.IsConsistent))
                return OperationResult<string>.Failed("Có thời khóa biểu không nhất quán, không thể xuất file.");

            var wb = await Task.Run(() => TimetableExcelBuilder.BuildWorkbook(selectedTimetables));

            var dir = System.IO.Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            await Task.Run(() => wb.SaveAs(filePath));

            new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo("explorer.exe", $"/select,\"{filePath}\"")
            }.Start();

            return OperationResult<string>.Success(filePath);
        }
        catch (Exception ex)
        {
            return OperationResult<string>.FromException(ex);
        }
    }
}