using System;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using FluentAssertions;
using Xunit;

namespace CTUScheduler.Tests.Core.Models;

public class ClassDayTests
{
    [Theory]
    [InlineData(2, DayOfWeek.Monday)]
    [InlineData(3, DayOfWeek.Tuesday)]
    [InlineData(4, DayOfWeek.Wednesday)]
    [InlineData(5, DayOfWeek.Thursday)]
    [InlineData(6, DayOfWeek.Friday)]
    [InlineData(7, DayOfWeek.Saturday)]
    [InlineData(8, DayOfWeek.Sunday)]
    [InlineData(1, DayOfWeek.Sunday)]
    public void DayOfWeek_ValidAttendingDay_MapsToCorrectDayOfWeek(int attendingDay, DayOfWeek expectedDay)
    {
        var classDay = new ClassDay(attendingDay, "123-------", "DI-101");

        classDay.DayOfWeek.Should().Be(expectedDay);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(-1)]
    public void DayOfWeek_InvalidAttendingDay_ThrowsArgumentOutOfRangeException(int attendingDay)
    {
        var classDay = new ClassDay(attendingDay, "123-------", "DI-101");

        Action act = () => _ = classDay.DayOfWeek;

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("123-------", 1, 3, 3)]
    [InlineData("--345-----", 3, 5, 3)]
    [InlineData("-------89-", 8, 9, 2)]
    [InlineData("----------", 0, 0, 0)]
    [InlineData("", 0, 0, 0)]
    public void PeriodProperties_ShouldParseStartEndAndCount(string period, int expectedStart, int expectedEnd, int expectedCount)
    {
        var classDay = new ClassDay(2, period, "DI-101");

        classDay.StartPeriod.Should().Be(expectedStart);
        classDay.EndPeriod.Should().Be(expectedEnd);
        classDay.PeriodCount.Should().Be(expectedCount);
    }
}
