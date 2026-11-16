using System;
using System.Collections.Generic;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Timetable;
using FluentAssertions;
using Xunit;

namespace CTUScheduler.Tests.Core.Models;

public class MaxCreditsPruningRuleTests
{
    [Fact]
    public void Constructor_WhenMaxCreditsNegative_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new MaxCreditsPruningRule(-1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CanContinue_WhenCurrentPathIsEmpty_AndCandidateCreditsWithinLimit_ReturnsTrue()
    {
        var rule = new MaxCreditsPruningRule(20);
        var candidate = CreateChoice("CT101", credits: 4);

        var result = rule.CanContinue(ReadOnlySpan<SectionChoice>.Empty, candidate);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanContinue_WhenCurrentPathIsEmpty_AndCandidateCreditsExceedLimit_ReturnsFalse()
    {
        var rule = new MaxCreditsPruningRule(3);
        var candidate = CreateChoice("CT101", credits: 4);

        var result = rule.CanContinue(ReadOnlySpan<SectionChoice>.Empty, candidate);

        result.Should().BeFalse();
    }

    [Fact]
    public void CanContinue_WhenTotalCreditsBelowLimit_ReturnsTrue()
    {
        var rule = new MaxCreditsPruningRule(20);
        var choice1 = CreateChoice("CT101", credits: 4);
        var choice2 = CreateChoice("CT102", credits: 3);
        var candidate = CreateChoice("CT103", credits: 3);

        ReadOnlySpan<SectionChoice> currentPath = new[] { choice1, choice2 }; // 4 + 3 = 7

        var result = rule.CanContinue(currentPath, candidate); // 7 + 3 = 10 <= 20

        result.Should().BeTrue();
    }

    [Fact]
    public void CanContinue_WhenTotalCreditsExactlyEqualsLimit_ReturnsTrue()
    {
        var rule = new MaxCreditsPruningRule(10);
        var choice1 = CreateChoice("CT101", credits: 4);
        var choice2 = CreateChoice("CT102", credits: 3);
        var candidate = CreateChoice("CT103", credits: 3);

        ReadOnlySpan<SectionChoice> currentPath = new[] { choice1, choice2 }; // 4 + 3 = 7

        var result = rule.CanContinue(currentPath, candidate); // 7 + 3 = 10 == 10

        result.Should().BeTrue();
    }

    [Fact]
    public void CanContinue_WhenTotalCreditsExceedsLimit_ReturnsFalse()
    {
        var rule = new MaxCreditsPruningRule(10);
        var choice1 = CreateChoice("CT101", credits: 4);
        var choice2 = CreateChoice("CT102", credits: 4);
        var candidate = CreateChoice("CT103", credits: 3);

        ReadOnlySpan<SectionChoice> currentPath = new[] { choice1, choice2 }; // 4 + 4 = 8

        var result = rule.CanContinue(currentPath, candidate); // 8 + 3 = 11 > 10

        result.Should().BeFalse();
    }

    private static SectionChoice CreateChoice(string courseCode, int credits)
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
            ClassDays: new List<ClassDay> { new ClassDay(2, "123-------", "Phòng 1") }
        );

        var course = new Course(courseCode, "Môn học " + courseCode, credits, 30, 0, new[] { section });
        return new SectionChoice(course, section);
    }
}
