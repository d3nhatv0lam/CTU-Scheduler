using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using CTUScheduler.Core.Models.Shared.Results;

namespace CTUScheduler.Infrastructure.Exel;

public class ExcelExporterService : IExcelExporterService
{
    public async Task<OperationResult<string>> ExportAsync<T>(
        IEnumerable<T> data,
        Stream output,  // stream là nơi xuất file excel vào
        IEnumerable<ExportColumnDefinition<T>>? columns = null,
        ExcelExportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (data is null) throw new ArgumentNullException(nameof(data));
        if (output is null) throw new ArgumentNullException(nameof(output));
        if (!output.CanWrite) return OperationResult<string>.Failed("Output stream is not writable.");

        options ??= new ExcelExportOptions();

        // Ưu tiên CancellationToken từ tham số hàm nếu có, nếu không thì dùng từ options
        var ct = cancellationToken != default ? cancellationToken : options.CancellationToken;

        try
        {
            ct.ThrowIfCancellationRequested();

            // ResolveColumns: nếu không có định nghĩa cột thì dùng phản chiếu để lấy tất cả thuộc tính công khai
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
                    var cell = sheet.Cell(row, col);    // Lấy ô hiện tại để ghi header
                    cell.Value = c.Header ?? string.Empty;  // Gán giá trị header, nếu null thì gán chuỗi rỗng
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromArgb(0xF2, 0xF2, 0xF2);
                    col++;
                }

                if (options.FreezeTopRow)
                    sheet.SheetView.FreezeRows(1);      // Đóng băng hàng đầu tiên nếu được yêu cầu

                row++;
            }

            // Rows
            foreach (var item in data)              // Duyệt qua từng mục dữ liệu
            {
                ct.ThrowIfCancellationRequested();

                col = 1;
                foreach (var c in columnList)       // Duyệt qua từng cột đã định nghĩa
                {
                    var cell = sheet.Cell(row, col);    // Lấy ô hiện tại để ghi dữ liệu
                    object? value = null;               // Biến để giữ giá trị lấy được từ selector
                    try
                    {
                        value = c.ValueSelector?.Invoke(item);  // Lấy giá trị bằng cách gọi selector
                    }
                    catch
                    {
                        value = null;
                    }

                    // Null handling
                    if (value is null)
                    {
                        cell.Value = string.Empty;
                    }
                    else
                    {
                        switch (value)  // Hổ trợ một số kiểu dữ liệu phổ biến
                        {
                            case DateTime dt:   // Khi value là DateTime thì gán trực tiếp
                                cell.Value = dt;
                                cell.Style.DateFormat.Format = c.NumberFormat ?? options.DateFormat ?? "yyyy-MM-dd";
                                break;
                            case DateTimeOffset dto:    // Khi value là DateTimeOffset thì gán giá trị DateTime của nó
                                cell.Value = dto.DateTime;
                                cell.Style.DateFormat.Format = c.NumberFormat ?? options.DateFormat ?? "yyyy-MM-dd";
                                break;
                            case IFormattable formattable:  // Hỗ trợ các kiểu dữ liệu có thể định dạng (số, ngày tháng, v.v.)
                                // If specific number format provided use it, otherwise assign raw value
                                cell.Value = formattable;
                                if (!string.IsNullOrWhiteSpace(c.NumberFormat))
                                    cell.Style.NumberFormat.Format = c.NumberFormat;
                                break;
                            default:
                                cell.Value = value;
                                break;
                        }
                    }

                    col++;
                }

                row++;
                // yield control to keep UI responsive in long operations
                await Task.Yield();
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
            // ClosedXML SaveAs(Stream) is synchronous; keep as Task.Run to avoid blocking caller thread if desired.
            await Task.Run(() =>    // này là để tránh blocking thread chính
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

        // Ensure directory exists
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // Handle overwrite policy
        if (File.Exists(filePath) && !options.OverwriteIfExists)
            return OperationResult<string>.Failed($"File already exists: {filePath}");

        try
        {
            // Open FileStream and delegate to ExportAsync
            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            var result = await ExportAsync(data, fs, columns, options, cancellationToken);
            if (result.IsSuccess)
                return OperationResult<string>.Success(filePath);

            // propagate failure (wrap message)
            return OperationResult<string>.Failed(result.FirstErrorMessage ?? "Lỗi không xác định trong quá trình xuất file");
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

    // Helpers

    private static List<ExportColumnDefinition<T>> ResolveColumns<T>(IEnumerable<ExportColumnDefinition<T>>? columns)
    {
        if (columns != null && columns.Any())
            return columns.ToList();        // nếu đã có định nghĩa cột thì dùng luôn

        // Fallback to reflection: là lấy tất cả các thuộc tính công khai của T
        var props = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToArray();

        var list = new List<ExportColumnDefinition<T>>(props.Length);
        foreach (var p in props)
        {
            // Capture property in local for closure
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
}