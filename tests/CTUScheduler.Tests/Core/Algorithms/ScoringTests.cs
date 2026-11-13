using System.Collections.Generic;
using CTUScheduler.Core.Algorithms.Scoring;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Timetable;
using FluentAssertions;
using Xunit;

namespace CTUScheduler.Tests.Core.Algorithms;

public class ScoringTests
{
    [Fact]
    public void BalancedWorkloadScorer_WhenSingleDay_ShouldReturnPerfectScore()
    {
        var scorer = new BalancedWorkloadScorer();
        var schedule = new List<SectionChoice>
        {
            CreateChoice("CT101", 2, "123-------") // Monday 3 periods
        };

        var score = scorer.CalculateScore(schedule);

        score.Should().Be(1.0);
    }

    [Fact]
    public void BalancedWorkloadScorer_WhenEqualLoadsAcrossDays_ShouldReturnPerfectScore()
    {
        var scorer = new BalancedWorkloadScorer();
        var schedule = new List<SectionChoice>
        {
            CreateChoice("CT101", 2, "1234------"), // Monday 4 periods
            CreateChoice("CT102", 4, "1234------")  // Wednesday 4 periods
        };

        var score = scorer.CalculateScore(schedule);

        score.Should().Be(1.0);
    }

    [Fact]
    public void BalancedWorkloadScorer_WhenHeavilyImbalanced_ShouldPenalizeScore()
    {
        var scorer = new BalancedWorkloadScorer();
        var schedule = new List<SectionChoice>
        {
            CreateChoice("CT101", 2, "123456789-"), // Monday 9 periods
            CreateChoice("CT102", 3, "1---------")   // Tuesday 1 period
        };

        var score = scorer.CalculateScore(schedule);

        score.Should().BeLessThan(0.5);
    }

    [Fact]
    public void CompactDaysScorer_ShouldRewardFewerDays()
    {
        var scorer = new CompactDaysScorer();

        // Schedule A: 2 days
        var scheduleA = new List<SectionChoice>
        {
            CreateChoice("CT101", 2, "123-------"),
            CreateChoice("CT102", 3, "123-------")
        };

        // Schedule B: 4 days
        var scheduleB = new List<SectionChoice>
        {
            CreateChoice("CT101", 2, "1---------"),
            CreateChoice("CT102", 3, "1---------"),
            CreateChoice("CT103", 4, "1---------"),
            CreateChoice("CT104", 5, "1---------")
        };

        var scoreA = scorer.CalculateScore(scheduleA); // 1.0 - 2/7 ≈ 0.714
        var scoreB = scorer.CalculateScore(scheduleB); // 1.0 - 4/7 ≈ 0.428

        scoreA.Should().BeGreaterThan(scoreB);
        scoreA.Should().BeApproximately(1.0 - (2.0 / 7.0), 0.001);
    }

    [Fact]
    public void MinimizeGapsScorer_WhenConsecutiveClasses_ShouldReturnPerfectScore()
    {
        var scorer = new MinimizeGapsScorer();
        var schedule = new List<SectionChoice>
        {
            CreateChoice("CT101", 2, "12--------"), // Monday periods 1..2
            CreateChoice("CT102", 2, "--34------")  // Monday periods 3..4 (consecutive, no gap!)
        };

        var score = scorer.CalculateScore(schedule);

        score.Should().Be(1.0);
    }

    [Fact]
    public void MinimizeGapsScorer_WhenGapsExist_ShouldPenalizeScore()
    {
        var scorer = new MinimizeGapsScorer();
        var schedule = new List<SectionChoice>
        {
            CreateChoice("CT101", 2, "12--------"), // Monday periods 1..2 (end = 2)
            CreateChoice("CT102", 2, "-----67---")  // Monday periods 6..7 (start = 6, gap = 3 periods: 3,4,5)
        };

        var score = scorer.CalculateScore(schedule);

        // total periods = 2 + 2 = 4. total gap = 3. score = 1.0 - 3/4 = 0.25
        score.Should().BeApproximately(0.25, 0.01);
    }

    [Fact]
    public void TimeOfDayScorer_MorningPreference_ShouldRewardMorningClasses()
    {
        var morningScorer = new TimeOfDayScorer(TimeOfDay.Morning);
        var afternoonScorer = new TimeOfDayScorer(TimeOfDay.Afternoon);

        // Periods 1..3 is morning (<= 5)
        var morningSchedule = new List<SectionChoice>
        {
            CreateChoice("CT101", 2, "123-------")
        };

        morningScorer.CalculateScore(morningSchedule).Should().Be(1.0);
        afternoonScorer.CalculateScore(morningSchedule).Should().Be(0.0);
    }

    private static SectionChoice CreateChoice(string courseCode, int day, string period)
    {
        var section = new CourseSection(
            IsCancelled: false,
            Key: 1,
            Code: courseCode,
            Group: "01",
            Lecturer: "Giảng viên",
            LecturerEmail: "gv@ctu.edu.vn",
            TotalStudents: 40,
            RemainingStudents: 10,
            ClassDays: new List<ClassDay> { new ClassDay(day, period, "Phòng 1") }
        );

        var course = new Course(courseCode, "Môn học " + courseCode, 3, 30, 0, new[] { section });
        return new SectionChoice(course, section);
    }
}
