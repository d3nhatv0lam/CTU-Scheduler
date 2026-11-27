using Xunit;

namespace CTUScheduler.Tests.Scheduling;

public sealed record ClassSessionSlot(
    string CourseCode,
    int DayOfWeek,
    int StartPeriod,
    int EndPeriod);

public static class ScheduleConflictChecker
{
    public static bool HasConflict(ClassSessionSlot a, ClassSessionSlot b)
    {
        if (a.DayOfWeek != b.DayOfWeek)
            return false;

        if (a.StartPeriod > a.EndPeriod || b.StartPeriod > b.EndPeriod)
            return false;

        return a.StartPeriod <= b.EndPeriod && b.StartPeriod <= a.EndPeriod;
    }
}

public sealed class ScheduleConflictTests
{
    [Fact]
    public void HasConflict_DifferentDays_ReturnsFalse()
    {
        var mon = new ClassSessionSlot("CT101", 2, 1, 3);
        var tue = new ClassSessionSlot("CT102", 3, 1, 3);

        Assert.False(ScheduleConflictChecker.HasConflict(mon, tue));
    }

    [Fact]
    public void HasConflict_AdjacentPeriodsOnSameDay_ReturnsFalse()
    {
        var first = new ClassSessionSlot("CT101", 2, 1, 3);
        var second = new ClassSessionSlot("CT102", 2, 4, 6);

        Assert.False(ScheduleConflictChecker.HasConflict(first, second));
    }

    [Fact]
    public void HasConflict_OverlappingPeriodsOnSameDay_ReturnsTrue()
    {
        var first = new ClassSessionSlot("CT101", 4, 1, 4);
        var second = new ClassSessionSlot("CT102", 4, 3, 5);

        Assert.True(ScheduleConflictChecker.HasConflict(first, second));
    }
}
