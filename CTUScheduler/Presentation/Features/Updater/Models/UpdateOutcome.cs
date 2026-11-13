namespace CTUScheduler.Presentation.Features.Updater.Models;

public enum UpdateOutcome
{
    Closed,      // Đóng hộp thoại
    Updated,    // Đã tải xong và gọi lệnh khởi động lại
    UpToDate,   // Đang là bản mới nhất
    Ignored,    // Người dùng bấm bỏ qua phiên bản này
    Cancelled,  // Người dùng hủy tải
    Failed,     // Quá trình cập nhật thất bại
}