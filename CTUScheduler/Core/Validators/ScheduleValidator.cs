using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;

namespace CTUScheduler.Core.Validators;

public static class ScheduleValidator
{
    /// <summary>
    /// Kiểm tra 2 học phần có trùng lịch nhau không
    /// </summary>
    public static bool IsOverlapTimeTable(CourseSection x, CourseSection y)
    {
        if (ReferenceEquals(x, y)) return true;
        
        if (x.ClassDays is null || y.ClassDays is null) return false;
        if (x.ClassDays.Count == 0 || y.ClassDays.Count == 0) return false;
        
        var xDays = x.ClassDays;
        var yDays = y.ClassDays;
        
        foreach (var xTime in xDays)
        {
            foreach (var yTime in yDays)
            {
                if (xTime.AttendingDay != yTime.AttendingDay)
                    continue;

                if (IsConflict(xTime, yTime))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Kiểm tra 2 khoảng thời gian trong cùng 1 ngày có chạm nhau không
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsConflict(ClassDay x, ClassDay y)
    {
        return !(x.EndPeriod < y.StartPeriod || y.EndPeriod < x.StartPeriod);
    }


    /// <summary>
    ///  Check existed timetable in a list by comparing saved course group keys
    /// </summary>
    /// <param name="table"></param>
    /// <param name="tables"></param>
    /// <returns></returns>
    public static bool IsTimetableExisted(IEnumerable<ScheduleProfile> tables, ScheduleProfile table)
        => tables.Any(x => table.SavedCourseGroupKeys.DictionaryEquals(x.SavedCourseGroupKeys));
}