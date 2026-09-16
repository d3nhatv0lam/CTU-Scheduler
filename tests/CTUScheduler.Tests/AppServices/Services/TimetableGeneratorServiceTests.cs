using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Services.TimetableGeneratorService;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Timetable;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CTUScheduler.Tests.AppServices.Services;

public class TimetableGeneratorServiceTests
{
    private readonly TimetableGeneratorService _service;

    public TimetableGeneratorServiceTests()
    {
        _service = new TimetableGeneratorService(NullLogger<TimetableGeneratorService>.Instance);
    }

    [Fact]
    public async Task Generate_WhenNoOverlaps_ShouldProduceAllCombinations()
    {
        // Course 1: 2 non-overlapping sections on Monday
        var set1 = new List<SectionChoice>
        {
            CreateChoice("CT101", 2, "12--------"),
            CreateChoice("CT101", 2, "--34------")
        };

        // Course 2: 1 section on Tuesday
        var set2 = new List<SectionChoice>
        {
            CreateChoice("CT102", 3, "12--------")
        };

        var sets = new List<IReadOnlyList<SectionChoice>> { set1, set2 };

        var results = await _service.Generate(sets).FirstAsync();

        results.Should().NotBeNull();
        results.Should().HaveCount(2); // 2 x 1 = 2
    }

    [Fact]
    public async Task Generate_WhenSectionsOverlap_ShouldPruneConflictedTimetables()
    {
        // Course 1 on Monday 1..2
        var choice1 = CreateChoice("CT101", 2, "12--------");
        var set1 = new List<SectionChoice> { choice1 };

        // Course 2: Section A overlaps Monday 1..2, Section B is Tuesday
        var choice2A = CreateChoice("CT102", 2, "12--------"); // Overlaps choice1!
        var choice2B = CreateChoice("CT102", 3, "12--------"); // Non-overlapping
        var set2 = new List<SectionChoice> { choice2A, choice2B };

        var sets = new List<IReadOnlyList<SectionChoice>> { set1, set2 };

        var results = await _service.Generate(sets).FirstAsync();

        // Only choice1 + choice2B is valid
        results.Should().HaveCount(1);
        results[0].Choices.Should().Contain(choice2B);
    }

    [Fact]
    public async Task Generate_WithMaxResults_ShouldRespectLimit()
    {
        var set1 = new List<SectionChoice>
        {
            CreateChoice("CT101", 2, "1---------"),
            CreateChoice("CT101", 2, "-2--------"),
            CreateChoice("CT101", 2, "--3-------")
        };
        var set2 = new List<SectionChoice>
        {
            CreateChoice("CT102", 3, "1---------"),
            CreateChoice("CT102", 3, "-2--------")
        };
        var sets = new List<IReadOnlyList<SectionChoice>> { set1, set2 }; // 3 x 2 = 6 total

        var options = new ScheduleGenerationOptions
        {
            MaxResults = 3
        };

        var results = await _service.Generate(sets, options).FirstAsync();

        results.Should().HaveCount(3);
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
