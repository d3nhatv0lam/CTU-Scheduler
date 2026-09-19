using System;
using System.Collections.Generic;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Timetable;
using FluentAssertions;
using Xunit;

namespace CTUScheduler.Tests.Core.Models;

public class OverlapPruningRuleTests
{
    [Fact]
    public void CanContinue_WhenNoConflictWithCurrentPath_ShouldReturnTrue()
    {
        var rule = new OverlapPruningRule();
        var choice1 = CreateChoice("CT101", 2, "123-------"); // Monday 1..3
        var choice2 = CreateChoice("CT102", 3, "123-------"); // Tuesday 1..3
        var nextCandidate = CreateChoice("CT103", 4, "123-------"); // Wednesday 1..3

        ReadOnlySpan<SectionChoice> currentPath = new[] { choice1, choice2 };

        var result = rule.CanContinue(currentPath, nextCandidate);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanContinue_WhenCandidateConflictsWithCurrentPath_ShouldReturnFalse()
    {
        var rule = new OverlapPruningRule();
        var choice1 = CreateChoice("CT101", 2, "123-------"); // Monday 1..3
        var nextCandidate = CreateChoice("CT102", 2, "-234------"); // Monday 2..4 (overlap!)

        ReadOnlySpan<SectionChoice> currentPath = new[] { choice1 };

        var result = rule.CanContinue(currentPath, nextCandidate);

        result.Should().BeFalse();
    }

    [Fact]
    public void CanContinue_WhenCurrentPathIsEmpty_ShouldReturnTrue()
    {
        var rule = new OverlapPruningRule();
        var nextCandidate = CreateChoice("CT101", 2, "123-------");

        ReadOnlySpan<SectionChoice> currentPath = ReadOnlySpan<SectionChoice>.Empty;

        var result = rule.CanContinue(currentPath, nextCandidate);

        result.Should().BeTrue();
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
