namespace CTUScheduler.Core.Models.Update;

/// <summary>
/// Kết quả kiểm tra bản cập nhật mới từ IUpdateService.
/// Không phụ thuộc vào thư viện Velopack.
/// </summary>
public readonly record struct UpdateCheckResult(
    bool IsUpdateAvailable,
    string? CurrentVersion = null,
    string? TargetVersion = null,
    long? PackageSizeBytes = null,
    bool IsDowngrade = false
)
{
    /// <summary>
    /// Không có bản cập nhật mới nào.
    /// </summary>
    public static UpdateCheckResult NoUpdate(string? currentVersion = null) 
        => new(false, CurrentVersion: currentVersion);
    
    /// <summary>
    /// Chuỗi định dạng dung lượng thân thiện (ví dụ: "15.4 MB", "420 KB").
    /// </summary>
    public string FormattedSize => PackageSizeBytes switch
    {
        null or <= 0 => string.Empty,
        < 1024 * 1024 => $"{PackageSizeBytes.Value / 1024.0:F1} KB",
        _ => $"{PackageSizeBytes.Value / (1024.0 * 1024.0):F1} MB"
    };
}