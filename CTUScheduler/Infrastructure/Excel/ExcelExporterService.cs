using System;
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

            var wb = await Task.Run(() => TimetableExcelBuilder.BuildWorkbook(blueprint, "Thời Khóa Biểu"));

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
}