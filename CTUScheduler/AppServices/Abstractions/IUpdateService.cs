using System;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Update;

namespace CTUScheduler.AppServices.Abstractions;

public interface IUpdateService
{
    /// <summary>
    /// Kiểm tra ứng dụng có đang chạy từ bản cài đặt Velopack hay không.
    /// Nếu chạy ở môi trường Debug/Dev trong IDE, thuộc tính này sẽ là false.
    /// </summary>
    bool IsInstalled { get; }
    
    /// <summary>
    /// Version hiện tại của ứng dụng.
    /// </summary>
    string CurrentVersion { get; }

    /// <summary>
    /// Kiểm tra trên GitHub xem có phiên bản mới hơn không.
    /// </summary>
    Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tải gói cập nhật về máy. Callback onProgress trả về tiến độ từ 0 đến 100.
    /// </summary>
    Task DownloadUpdatesAsync(Action<int>? onProgress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Áp dụng gói cập nhật vừa tải và khởi động lại ứng dụng.
    /// </summary>
    void ApplyUpdatesAndRestart();
}