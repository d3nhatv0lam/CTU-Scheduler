using System;

namespace CTUScheduler.Core.Exceptions;

/// <summary>
/// Ngoại lệ dùng để chủ động dừng/hủy bỏ toàn bộ tiến trình cào hàng loạt (Batch Fetch)
/// khi phát hiện lỗi hệ thống, mất phiên, hoặc sai cấu hình đầu vào (Validation).
/// </summary>
public class BatchFetchAbortedException : Exception
{
    public BatchFetchAbortedException(string message, Exception? innerException = null) 
        : base(message, innerException)
    {
    }
}
