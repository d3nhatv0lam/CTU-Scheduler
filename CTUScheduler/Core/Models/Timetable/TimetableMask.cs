using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTUScheduler.Core.Models.Timetable;

public readonly record struct TimetableMask(UInt128 Value)
{
    /// <summary>
    /// Thứ đi học. [2,3,...8: sunday]
    /// </summary>
    private static readonly (int Min, int Max) AttendingDay = (2, 8);

    /// <summary>
    /// Tiết học. [1,13: 1st period]
    /// </summary>
    private static readonly (int Min, int Max) Period = (1, 13);

    private static int PeriodsPerDay => Period.Max - Period.Min + 1;

    #region Private Helpers (Core Logic)

    /// <summary>
    /// Kiểm tra tính hợp lệ của Ngày và Tiết. Fail-Fast nếu sai logic.
    /// </summary>
    private static void EnsureValidSlot(int day, int period)
    {
        if (day < AttendingDay.Min || day > AttendingDay.Max)
            throw new ArgumentOutOfRangeException(nameof(day), day,
                $"Day must be between {AttendingDay.Min} and {AttendingDay.Max}.");

        if (period < Period.Min || period > Period.Max)
            throw new ArgumentOutOfRangeException(nameof(period), period,
                $"Period must be between {Period.Min} and {Period.Max}.");
    }

    /// <summary>
    /// Dịch Xuôi: Chuyển Ngày & Tiết thành Zero-based Index (Bit offset).
    /// </summary>
    private static int GetBitIndex(int day, int period)
    {
        EnsureValidSlot(day, period);
        return (day - AttendingDay.Min) * PeriodsPerDay + (period - Period.Min);
    }

    /// <summary>
    /// Dịch Ngược: Chuyển Zero-based Index trở lại Ngày & Tiết.
    /// </summary>
    private static (int Day, int Period) GetDayAndPeriod(int bitIndex)
    {
        int day = (bitIndex / PeriodsPerDay) + AttendingDay.Min;
        int period = (bitIndex % PeriodsPerDay) + Period.Min;
        return (day, period);
    }

    #endregion

    #region Factory Methods
    
    public static TimetableMask Create(int day, int period)
    {
        int index = GetBitIndex(day, period);
        return new TimetableMask((UInt128)1 << index);
    }

    public static TimetableMask Create(IEnumerable<(int Day, int Period)> slots)
    {
        UInt128 maskValue = 0;
        foreach (var (day, period) in slots)
        {
            // Sẽ quăng exception ngay lập tức nếu Mapper đẩy vào bất kỳ slot nào bị lỗi
            int index = GetBitIndex(day, period);
            maskValue |= ((UInt128)1 << index);
        }

        return new TimetableMask(maskValue);
    }

    #endregion
    
    #region Operators & Operations

    public static TimetableMask operator |(TimetableMask left, TimetableMask right)
        => new(left.Value | right.Value);

    public static TimetableMask operator &(TimetableMask left, TimetableMask right)
        => new(left.Value & right.Value);

    public bool Overlaps(TimetableMask other) => (Value & other.Value) != 0;

    public bool IsOccupied(int day, int period)
    {
        int index = GetBitIndex(day, period); // Hàm này đã bao gồm EnsureValidSlot
        return (Value & ((UInt128)1 << index)) != 0;
    }

    public static TimetableMask Combine(params TimetableMask[] masks)
        => masks.Aggregate(new TimetableMask(0), (acc, m) => acc | m);

    /// <summary>
    /// Lấy danh sách tất cả các ô thời khóa biểu đang bị chiếm dụng.
    /// </summary>
    public IEnumerable<(int Day, int Period)> GetOccupiedSlots()
    {
        int maxBits = (AttendingDay.Max - AttendingDay.Min + 1) * PeriodsPerDay;
        
        for (int i = 0; i < maxBits; i++)
        {
            if ((Value & ((UInt128)1 << i)) != 0)
            {
                yield return GetDayAndPeriod(i);
            }
        }
    }

    #endregion
    

    public string ToDebugGrid()
    {
        var sb = new StringBuilder();
        sb.AppendLine("   | 1 2 3 4 5 | 6 7 8 9 10 | 11 12 13");
        sb.AppendLine("---+------------------------------------");

        for (int day = AttendingDay.Min; day <= AttendingDay.Max; day++)
        {
            sb.Append($"T{day} | ");
            for (int period = Period.Min; period <= Period.Max; period++)
            {
                bool isSet = IsOccupied(day, period);
                sb.Append(isSet ? "█ " : ". ");

                if (period == 5 || period == 10) sb.Append("| ");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}

public static class TimetableMaskExtensions
{
    public static TimetableMask Combine(this IEnumerable<TimetableMask> masks)
        => masks.Aggregate(new TimetableMask(0), (acc, m) => acc | m);


    public static bool AnyOverlap(this IEnumerable<TimetableMask> masks)
    {
        UInt128 combined = 0;
        foreach (var mask in masks)
        {
            if ((combined & mask.Value) != 0)
            {
                return true;
            }

            combined |= mask.Value;
        }

        return false;
    }

    public static int CountOccupied(this TimetableMask mask)
        => (int)UInt128.PopCount(mask.Value);
}