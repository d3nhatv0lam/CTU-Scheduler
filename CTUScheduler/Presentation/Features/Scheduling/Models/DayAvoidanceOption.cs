using System.Collections.Generic;

namespace CTUScheduler.Presentation.Features.Scheduling.Models;

/// <summary>
/// Chế độ tránh lịch của một ngày trong tuần.
/// </summary>
public enum DayAvoidanceMode
{
    /// <summary>
    /// Tránh toàn bộ ngày (cả sáng và chiều, hoặc khi không chọn cụ thể buổi nào)
    /// </summary>
    AllDay = 0,

    /// <summary>
    /// Chỉ tránh buổi sáng
    /// </summary>
    MorningOnly = 1,

    /// <summary>
    /// Chỉ tránh buổi chiều
    /// </summary>
    AfternoonOnly = 2
}

/// <summary>
/// Lựa chọn chế độ tránh hiển thị trên Dropdown / ComboBox của từng ngày.
/// </summary>
public record DayAvoidanceOption(DayAvoidanceMode Mode, string DisplayName, string ShortName)
{
    public static readonly IReadOnlyList<DayAvoidanceOption> AllOptions =
    [
        new(DayAvoidanceMode.AllDay, "Cả ngày (Sáng & Chiều)", "Cả ngày"),
        new(DayAvoidanceMode.MorningOnly, "Chỉ buổi Sáng", "Sáng"),
        new(DayAvoidanceMode.AfternoonOnly, "Chỉ buổi Chiều", "Chiều")
    ];
}
