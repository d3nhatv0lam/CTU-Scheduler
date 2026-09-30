using System;
using System.Collections.Generic;
using CTUScheduler.Core.Models.Timetable;
using CTUScheduler.Presentation.Features.Scheduling.Models;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels.Components;

/// <summary>
/// Quản lý trạng thái tránh lịch của một ngày trong tuần.
/// </summary>
public partial class DayAvoidanceItemViewModel : ReactiveObject
{
    public DayOfWeek Day { get; }
    public string ShortLabel { get; }
    public string FullLabel { get; }

    public IReadOnlyList<DayAvoidanceOption> Options => DayAvoidanceOption.AllOptions;

    [Reactive] private bool _isAvoided;
    [Reactive] private DayAvoidanceOption _selectedOption;

    public DayAvoidanceItemViewModel(DayOfWeek day, string shortLabel, string fullLabel)
    {
        Day = day;
        ShortLabel = shortLabel;
        FullLabel = fullLabel;
        _selectedOption = DayAvoidanceOption.AllOptions[0]; // Mặc định là Cả ngày
    }

    /// <summary>
    /// Sinh ra AvoidanceSlot dựa trên trạng thái cấu hình.
    /// Nếu không kích hoạt -> trả về null.
    /// </summary>
    public AvoidanceSlot? GetAvoidanceSlot()
    {
        if (!IsAvoided) return null;

        return SelectedOption.Mode switch
        {
            DayAvoidanceMode.MorningOnly => new AvoidanceSlot(Day, TimeOfDay.Morning),
            DayAvoidanceMode.AfternoonOnly => new AvoidanceSlot(Day, TimeOfDay.Afternoon),
            _ => new AvoidanceSlot(Day, null) // Cả ngày (hoặc không chọn buổi cụ thể)
        };
    }

    /// <summary>
    /// Đặt lại trạng thái ngày về chưa kích hoạt.
    /// </summary>
    public void Reset()
    {
        IsAvoided = false;
        SelectedOption = DayAvoidanceOption.AllOptions[0];
    }
}
