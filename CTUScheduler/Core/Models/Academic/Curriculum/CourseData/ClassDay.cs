using System;

namespace CTUScheduler.Core.Models.Academic.Curriculum.CourseData;

/// <summary>
/// Represent a single class day in a course section
/// </summary>
/// <param name="AttendingDay">từ thứ 2 -> 7, Value = [2, 3, .., 7]</param>
/// <param name="Period">Tiết học (chuỗi dấu gạch và số)</param>
/// <param name="Room">Phòng học</param>
public record ClassDay(int AttendingDay, string Period, string Room)
{
    /// <summary>
    /// Chuyển đổi số Thứ (int) sang DayOfWeek 
    /// </summary>
    public DayOfWeek DayOfWeek => AttendingDay switch
    {
        2 => System.DayOfWeek.Monday,
        3 => System.DayOfWeek.Tuesday,
        4 => System.DayOfWeek.Wednesday,
        5 => System.DayOfWeek.Thursday,
        6 => System.DayOfWeek.Friday,
        7 => System.DayOfWeek.Saturday,
        8 or 1 => System.DayOfWeek.Sunday, // Hỗ trợ cả 1 hoặc 8 cho Chủ nhật
        _ => throw new ArgumentOutOfRangeException(nameof(AttendingDay),
            $"Giá trị {AttendingDay} không hợp lệ cho một ngày trong tuần.")
    };


    /// <summary>
    /// Tiết bắt đầu
    /// </summary>
    public int StartPeriod
    {
        get
        {
            if (string.IsNullOrEmpty(Period)) return 0;
            var span = Period.AsSpan().TrimStart('-');
            return span.Length > 0 ? span[0] - '0' : 0;
        }
    }


    /// <summary>
    /// Tiết kết thúc
    /// </summary>
    public int EndPeriod
    {
        get
        {
            if (string.IsNullOrEmpty(Period)) return 0;
            var span = Period.AsSpan().TrimEnd('-');
            return span.Length > 0 ? span[^1] - '0' : 0;
        }
    }

    /// <summary>
    /// Số tiết học
    /// </summary>
    public int PeriodCount
    {
        get
        {
            if (string.IsNullOrEmpty(Period)) return 0;
            var span = Period.AsSpan().Trim('-');
            return span.Length;
        }
    }
}