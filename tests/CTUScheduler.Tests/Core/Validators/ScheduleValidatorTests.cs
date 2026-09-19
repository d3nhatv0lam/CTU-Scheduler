using System.Collections.Generic;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Validators;
using FluentAssertions;
using Xunit;

namespace CTUScheduler.Tests.Core.Validators;

public class ScheduleValidatorTests
{
    [Fact]
    public void IsConflict_WhenPeriodsOverlap_ShouldReturnTrue()
    {
        var day1 = new ClassDay(2, "123-------", "DI-101"); // periods 1..3
        var day2 = new ClassDay(2, "--345-----", "DI-102"); // periods 3..5

        ScheduleValidator.IsConflict(day1, day2).Should().BeTrue();
    }

    [Fact]
    public void IsConflict_WhenPeriodsDisjoint_ShouldReturnFalse()
    {
        var day1 = new ClassDay(2, "12--------", "DI-101"); // periods 1..2
        var day2 = new ClassDay(2, "--345-----", "DI-102"); // periods 3..5

        ScheduleValidator.IsConflict(day1, day2).Should().BeFalse();
    }

    [Fact]
    public void IsOverlapTimeTable_SameReference_ShouldReturnTrue()
    {
        var section = CreateSection("CT101", 2, "123-------");

        ScheduleValidator.IsOverlapTimeTable(section, section).Should().BeTrue();
    }

    [Fact]
    public void IsOverlapTimeTable_DifferentDays_ShouldNotOverlap()
    {
        var section1 = CreateSection("CT101", 2, "123-------"); // Monday
        var section2 = CreateSection("CT102", 3, "123-------"); // Tuesday

        ScheduleValidator.IsOverlapTimeTable(section1, section2).Should().BeFalse();
    }

    [Fact]
    public void IsOverlapTimeTable_SameDayAndOverlappingPeriods_ShouldOverlap()
    {
        var section1 = CreateSection("CT101", 4, "123-------"); // Wednesday 1..3
        var section2 = CreateSection("CT102", 4, "-234------"); // Wednesday 2..4

        ScheduleValidator.IsOverlapTimeTable(section1, section2).Should().BeTrue();
    }

    [Fact]
    public void IsOverlapTimeTable_EmptyClassDays_ShouldNotOverlap()
    {
        var section1 = new CourseSection(false, 1, "CT101", "01", "Teacher A", "", 40, 5, new List<ClassDay>());
        var section2 = CreateSection("CT102", 4, "123-------");

        ScheduleValidator.IsOverlapTimeTable(section1, section2).Should().BeFalse();
    }

    [Fact]
    public void IsConflict_WhenConsecutivePeriods_ShouldReturnFalse()
    {
        var day1 = new ClassDay(2, "123-------", "DI-101"); // periods 1..3
        var day2 = new ClassDay(2, "---45-----", "DI-102"); // periods 4..5

        ScheduleValidator.IsConflict(day1, day2).Should().BeFalse();
    }

    [Fact]
    public void IsOverlapTimeTable_MultipleDays_ShouldDetectOverlapOnMatchingDay()
    {
        // Section 1: Monday & Wednesday
        var section1 = new CourseSection(
            false, 1, "CT101", "01", "GV A", "", 40, 5,
            new List<ClassDay>
            {
                new(2, "123-------", "P1"), // Mon 1..3
                new(4, "123-------", "P1")  // Wed 1..3
            }
        );

        // Section 2: Tuesday & Wednesday (overlaps Wednesday!)
        var section2 = new CourseSection(
            false, 2, "CT102", "01", "GV B", "", 40, 5,
            new List<ClassDay>
            {
                new(3, "123-------", "P2"), // Tue 1..3
                new(4, "-234------", "P2")  // Wed 2..4 (overlap!)
            }
        );

        ScheduleValidator.IsOverlapTimeTable(section1, section2).Should().BeTrue();
    }

    [Fact]
    public void IsTimetableExisted_WhenMatchingSavedKeys_ShouldReturnTrue()
    {
        var profile1 = new CTUScheduler.Core.Models.Academic.Curriculum.Schedule.ScheduleProfile
        {
            SavedCourseGroupKeys = new Dictionary<string, string> { { "CT101", "01" }, { "CT102", "02" } }
        };

        var profile2 = new CTUScheduler.Core.Models.Academic.Curriculum.Schedule.ScheduleProfile
        {
            SavedCourseGroupKeys = new Dictionary<string, string> { { "CT101", "01" }, { "CT102", "02" } }
        };

        var list = new[] { profile1 };

        ScheduleValidator.IsTimetableExisted(list, profile2).Should().BeTrue();
    }

    [Fact]
    public void IsTimetableExisted_WhenDifferentSavedKeys_ShouldReturnFalse()
    {
        var profile1 = new CTUScheduler.Core.Models.Academic.Curriculum.Schedule.ScheduleProfile
        {
            SavedCourseGroupKeys = new Dictionary<string, string> { { "CT101", "01" } }
        };

        var profile2 = new CTUScheduler.Core.Models.Academic.Curriculum.Schedule.ScheduleProfile
        {
            SavedCourseGroupKeys = new Dictionary<string, string> { { "CT101", "02" } }
        };

        var list = new[] { profile1 };

        ScheduleValidator.IsTimetableExisted(list, profile2).Should().BeFalse();
    }

    private static CourseSection CreateSection(string code, int day, string period)
    {
        return new CourseSection(
            IsCancelled: false,
            Key: 1,
            Code: code,
            Group: "01",
            Lecturer: "Giảng viên",
            LecturerEmail: "gv@ctu.edu.vn",
            TotalStudents: 40,
            RemainingStudents: 10,
            ClassDays: new List<ClassDay> { new ClassDay(day, period, "Phòng 1") }
        );
    }
}
