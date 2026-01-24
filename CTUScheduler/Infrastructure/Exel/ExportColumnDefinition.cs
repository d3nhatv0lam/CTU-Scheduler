using System;

namespace CTUScheduler.Infrastructure.Exel
{
    public sealed class ExportColumnDefinition<T>       // Định nghĩa cột
    {
        public string Header { get; init; } = string.Empty;
        /*
         * Một hàm (Func) để lấy dữ liệu từ đối tượng T. Ví dụ: x => x.FullName
         * Đây là thành phần quan trọng nhất giúp code linh hoạt,
         * không phụ thuộc vào tên biến cố định.
         */
        public Func<T, object?> ValueSelector { get; init; } = _ => null;
        public double? Width { get; init; }
        public string? NumberFormat { get; init; }
    }
}