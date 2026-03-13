using System.Collections.Generic;
using System.Linq;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Core.Models.Timetable;

public class AvoidTimePruningRule : IPruningRule
{
    private readonly List<AvoidanceSlot> _avoidanceSlots;

    // - Chỉ truyền day: Tránh nguyên ngày
    // - Chỉ truyền time: Tránh buổi đó ở tất cả các ngày
    // - Truyền cả hai: Tránh chính xác buổi của ngày đó
    public AvoidTimePruningRule(IEnumerable<AvoidanceSlot> slotsToAvoid)
    {
        _avoidanceSlots = slotsToAvoid?.ToList() ?? new List<AvoidanceSlot>();
    }

    public bool CanContinue(IReadOnlyList<SectionChoice> currentPath, SectionChoice nextCandidate)
    {
        if (_avoidanceSlots.Count == 0) return true;

        return !nextCandidate.Section.ClassDays.Any(day => _avoidanceSlots.Any(slot => IsMatch(day, slot)));
    }

    private bool IsMatch(ClassDay day, AvoidanceSlot slot)
    {
        var dayMatch = !slot.Day.HasValue || day.DayOfWeek == slot.Day.Value;
        var timeMatch = !slot.Time.HasValue || day.TimeOfDay == slot.Time.Value;

        return dayMatch && timeMatch;
    }
    
}