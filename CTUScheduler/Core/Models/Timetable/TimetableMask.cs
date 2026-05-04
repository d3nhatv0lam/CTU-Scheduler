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
    
    public bool Overlaps(TimetableMask other) => (Value & other.Value) != 0;

    public static TimetableMask operator |(TimetableMask left, TimetableMask right)
        => new(left.Value | right.Value);
    
    public TimetableMask CombineWith(TimetableMask other) 
        => new(Value | other.Value);

    public bool IsOccupied(int day, int period)
    {
        if (day < AttendingDay.Min || day > AttendingDay.Max || period < Period.Min || period > Period.Max) 
            return false;
        int index = (day - AttendingDay.Min) * Period.Max + (period - 1);
        return (Value & ((UInt128)1 << index)) != 0;
    }
    
    public static TimetableMask Combine(params TimetableMask[] masks)
        => masks.Aggregate(new TimetableMask(0), (acc, m) => acc | m);
    
    public string ToDebugGrid()
    {
        var sb = new StringBuilder();
        sb.AppendLine("   | 1 2 3 4 5 | 6 7 8 9 10 | 11 12 13");
        sb.AppendLine("---+------------------------------------");
    
        for (int day = 2; day <= 8; day++)
        {
            sb.Append($"T{day} | ");
            for (int period = 1; period <= 13; period++)
            {
                int index = (day - 2) * 13 + (period - 1);
                bool isSet = (Value & ((UInt128)1 << index)) != 0;
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
        => masks.Combine().Value != 0; 

    public static int CountOccupied(this TimetableMask mask)
        => (int)UInt128.PopCount(mask.Value);
}