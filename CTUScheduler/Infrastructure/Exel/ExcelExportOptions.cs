using System.Threading;

namespace CTUScheduler.Infrastructure.Exel;

public sealed class ExcelExportOptions      // Tùy chọn xuất file
{
    public string SheetName { get; init; } = "Sheet1";
    public bool IncludeHeader { get; init; } = true;        // Có bao gồm hàng tiêu đề không
    public bool AutoFitColumns { get; init; } = true;
    public bool FreezeTopRow { get; init; } = true;         // Có cố định hàng đầu tiên khi cuộn xuống không
    public bool OverwriteIfExists { get; init; } = true;    // Có ghi đè file nếu đã tồn tại không
    public string DateFormat { get; init; } = "yyyy-MM-dd";
    public CancellationToken CancellationToken { get; init; } = CancellationToken.None; // Mặc định không hủy
}