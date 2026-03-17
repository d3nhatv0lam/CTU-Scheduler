using ClosedXML.Excel;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Shared.Results;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CTUScheduler.Infrastructure.Exel;

public class ExcelExporterService : IExcelExporterService
{
    public async Task<OperationResult<string>> ExportAsync<T>(
        IEnumerable<T> data,
        Stream output,
        IEnumerable<ExportColumnDefinition<T>>? columns = null,
        ExcelExportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (data is null) throw new ArgumentNullException(nameof(data));
        if (output is null) throw new ArgumentNullException(nameof(output));
        if (!output.CanWrite) return OperationResult<string>.Failed("Output stream is not writable.");

        options ??= new ExcelExportOptions();

        // Determine cancellation token precedence: explicit param wins, otherwise options token.
        var ct = cancellationToken != default ? cancellationToken : options.CancellationToken;

        try
        {
            ct.ThrowIfCancellationRequested();

            // Prepare column definitions (fallback to reflection when columns not provided)
            var columnList = ResolveColumns(columns);

            using var wb = new XLWorkbook();
            var sheet = wb.Worksheets.Add(options.SheetName ?? "Sheet1");

            int row = 1;
            int col = 1;

            // Header
            if (options.IncludeHeader)
            {
                foreach (var c in columnList)
                {
                    var cell = sheet.Cell(row, col);
                    cell.Value = c.Header ?? string.Empty;
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromArgb(0xF2, 0xF2, 0xF2);
                    col++;
                }

                if (options.FreezeTopRow)
                    sheet.SheetView.FreezeRows(1);

                row++;
            }

            // Rows
            foreach (var item in data)
            {
                ct.ThrowIfCancellationRequested();

                col = 1;
                foreach (var c in columnList)
                {
                    var cell = sheet.Cell(row, col);
                    object? value = null;
                    try
                    {
                        value = c.ValueSelector?.Invoke(item);
                    }
                    catch
                    {
                        value = null;
                    }

                    // --- FIX FOR ClosedXML: Xử lý gán giá trị thủ công ---
                    if (value == null)
                    {
                        cell.Value = string.Empty; // Sửa lỗi .From() tại đây
                    }
                    else
                    {
                        switch (value)
                        {
                            case DateTime dt:
                                cell.Value = dt;
                                cell.Style.DateFormat.Format = c.NumberFormat ?? options.DateFormat ?? "yyyy-MM-dd";
                                break;
                            case DateTimeOffset dto:
                                cell.Value = dto.DateTime;
                                cell.Style.DateFormat.Format = c.NumberFormat ?? options.DateFormat ?? "yyyy-MM-dd";
                                break;
                            case double dbl:
                                cell.Value = dbl;
                                if (!string.IsNullOrWhiteSpace(c.NumberFormat))
                                    cell.Style.NumberFormat.Format = c.NumberFormat;
                                break;
                            case float flt:
                                cell.Value = flt; // Implicit conversion
                                if (!string.IsNullOrWhiteSpace(c.NumberFormat))
                                    cell.Style.NumberFormat.Format = c.NumberFormat;
                                break;
                            case decimal dec:
                                cell.Value = dec;
                                if (!string.IsNullOrWhiteSpace(c.NumberFormat))
                                    cell.Style.NumberFormat.Format = c.NumberFormat;
                                break;
                            case int intVal:
                                cell.Value = intVal;
                                if (!string.IsNullOrWhiteSpace(c.NumberFormat))
                                    cell.Style.NumberFormat.Format = c.NumberFormat;
                                break;
                            case long longVal:
                                cell.Value = longVal; // Implicit conversion
                                if (!string.IsNullOrWhiteSpace(c.NumberFormat))
                                    cell.Style.NumberFormat.Format = c.NumberFormat;
                                break;
                            case bool b:
                                cell.Value = b;
                                break;
                            case string s:
                                cell.Value = s;
                                break;
                            default:
                                // Fallback: Ép về string cho các kiểu dữ liệu lạ
                                cell.Value = value.ToString();
                                break;
                        }
                    }
                    // --- END FIX ---

                    col++;
                }

                row++;
                // Nhường thread định kỳ để UI không bị đơ
                if (row % 100 == 0) await Task.Yield();
            }

            // Column widths / autofit
            int totalColumns = columnList.Count;
            for (int i = 1; i <= totalColumns; i++)
            {
                var def = columnList[i - 1];
                if (def.Width.HasValue)
                    sheet.Column(i).Width = def.Width.Value;
            }

            if (options.AutoFitColumns)
                sheet.Columns(1, totalColumns).AdjustToContents();

            // Save workbook to provided stream
            await Task.Run(() =>
            {
                wb.SaveAs(output);
                output.Flush();
            }, ct);

            return OperationResult<string>.Success("ExportedToStream");
        }
        catch (OperationCanceledException ex)
        {
            return OperationResult<string>.FromException(ex);
        }
        catch (Exception ex)
        {
            return OperationResult<string>.FromException(ex);
        }
    }

    public async Task<OperationResult<string>> ExportToFileAsync<T>(
        IEnumerable<T> data,
        string filePath,
        IEnumerable<ExportColumnDefinition<T>>? columns = null,
        ExcelExportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));
        options ??= new ExcelExportOptions();

        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        if (File.Exists(filePath) && !options.OverwriteIfExists)
            return OperationResult<string>.Failed($"File already exists: {filePath}");

        try
        {
            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            var result = await ExportAsync(data, fs, columns, options, cancellationToken);

            if (result.IsSuccess)
                return OperationResult<string>.Success(filePath);

            return OperationResult<string>.Failed(result.FirstErrorMessage ?? "Unknown error while exporting");
        }
        catch (OperationCanceledException ex)
        {
            return OperationResult<string>.FromException(ex);
        }
        catch (Exception ex)
        {
            return OperationResult<string>.FromException(ex);
        }
    }

    private static List<ExportColumnDefinition<T>> ResolveColumns<T>(IEnumerable<ExportColumnDefinition<T>>? columns)
    {
        if (columns != null && columns.Any())
            return columns.ToList();

        var props = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToArray();

        var list = new List<ExportColumnDefinition<T>>(props.Length);
        foreach (var p in props)
        {
            var prop = p;
            var def = new ExportColumnDefinition<T>
            {
                Header = prop.Name,
                ValueSelector = (T instance) => prop.GetValue(instance),
                NumberFormat = null,
                Width = null
            };
            list.Add(def);
        }

        return list;
    }

    public async Task<OperationResult<string>> ExportTimetableAsync(ScheduleBlueprint blueprint, string filePath)
    {
        try
        {
            if (!blueprint.IsConsistent)
                return OperationResult<string>.Failed("Dữ liệu Thời khóa biểu không nhất quán.");

            var wb = await Task.Run(() => CTUScheduler.Infrastructure.Exel.TimetableExcelBuilder.BuildWorkbook(blueprint, "Thời Khóa Biểu"));

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