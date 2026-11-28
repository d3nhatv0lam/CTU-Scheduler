using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using CTUScheduler.Core.Models.Academic.Grades;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Presentation.Features.Grades.Helpers;
using CTUScheduler.Presentation.Features.Grades.ViewModels.Components;
using CTUScheduler.Presentation.Services.Theme;
using FluentAssertions;
using NSubstitute;
using ReactiveUI.Builder;
using Xunit;

namespace CTUScheduler.Tests.Presentation.Features.Grades;

public class GradeChartsTests
{
    static GradeChartsTests()
    {
        try
        {
            RxAppBuilder.CreateReactiveUIBuilder()
                .WithCoreServices()
                .BuildApp();
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static GradeTranscript CreateSampleTranscript()
    {
        var sem1Courses = new List<CourseGrade>
        {
            new(1, "XH023", "Anh văn căn bản 1", true, null, 4, "M", GradeLetter.M, null, "*(BL)", true),
            new(2, "CT100", "Kỹ năng học đại học", false, "D01", 2, "A", GradeLetter.A, 9.4, null, true),
            new(3, "SHCVHT", "Cố vấn học tập sinh hoạt lớp", false, "D23", 0, null, GradeLetter.Unknown, null, null, false),
            new(4, "CT199", "Môn ví dụ rớt", false, "01", 3, "F", GradeLetter.F, 2.5, null, false)
        };
        var sem1 = new SemesterGrade(2023, 1, sem1Courses, new SemesterGradeSummary(6, 10, 3.79, 4.00, 83));

        var sem3Courses = new List<CourseGrade>
        {
            new(1, "ML016", "Kinh tế chính trị Mác - Lênin", false, "12", 2, "B+", GradeLetter.BPlus, 8.2, null, true)
        };
        var sem3 = new SemesterGrade(2023, 3, sem3Courses, new SemesterGradeSummary(2, 12, 3.50, 3.94, null));

        var currentCourses = new List<CourseGrade>
        {
            new(1, "CT287", "Kiểm chứng mô hình", false, "01", 3, "A", GradeLetter.A, 9.2, null, false)
        };
        var currentSem = new SemesterGrade(2026, 1, currentCourses, null);

        return new GradeTranscript(
            [currentSem, sem3, sem1],
            new DateTimeOffset(2026, 11, 28, 16, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void GradeStatisticsHelper_BuildSemesterTrends_SortsChronologicallyAndSkipsUnfinalized()
    {
        var trends = GradeStatisticsHelper.BuildSemesterTrends(CreateSampleTranscript());

        trends.Should().HaveCount(2);
        trends[0].ShortLabel.Should().Be("HK1 (23-24)");
        trends[0].ConductScore.Should().Be(83);
        trends[1].ShortLabel.Should().Be("HK3 (23-24)");
        trends[1].ConductScore.Should().BeNull();
        trends[1].TotalAccumulatedCredits.Should().Be(12);
    }

    [Fact]
    public void GradeStatisticsHelper_BuildLetterDistribution_ComputesCreditsAndCountsAccurately()
    {
        var transcript = CreateSampleTranscript();

        var byCredits = GradeStatisticsHelper.BuildLetterDistribution(transcript, byCredits: true);
        byCredits.Should().Contain(p => p.Letter == GradeLetter.A && p.Credits == 5 && p.CourseCount == 2);
        byCredits.Should().Contain(p => p.Letter == GradeLetter.M && p.Credits == 4 && p.CourseCount == 1);
        byCredits.Should().NotContain(p => p.Letter == GradeLetter.Unknown);

        var byCourses = GradeStatisticsHelper.BuildLetterDistribution(transcript, byCredits: false);
        byCourses.Should().Contain(p => p.Letter == GradeLetter.A && p.Value == 2);
        byCourses.Should().Contain(p => p.Letter == GradeLetter.F && p.Value == 1);
    }

    [Fact]
    public void GradeStatisticsHelper_BuildCourseScorePoints_FiltersAndPreservesChronologicalOrder()
    {
        var transcript = CreateSampleTranscript();

        var allScored = GradeStatisticsHelper.BuildCourseScorePoints(transcript);
        allScored.Should().HaveCount(4);
        allScored[0].CourseCode.Should().Be("CT100");
        allScored[1].CourseCode.Should().Be("CT199");
        allScored[2].CourseCode.Should().Be("ML016");
        allScored[3].CourseCode.Should().Be("CT287");

        var filteredYear = GradeStatisticsHelper.BuildCourseScorePoints(transcript, 2023, 3);
        filteredYear.Should().ContainSingle(p => p.CourseCode == "ML016");
    }

    [Fact]
    public void GradeChartsViewModel_UpdatesSeriesWhenTranscriptModeOrThemeChanges()
    {
        using var themeSubject = new Subject<AppTheme>();
        var themeService = Substitute.For<IThemeService>();
        themeService.CurrentTheme.Returns(AppTheme.Light);
        themeService.ThemeChanged.Returns(themeSubject);

        using var vm = new GradeChartsViewModel(themeService);
        vm.UpdateTranscript(CreateSampleTranscript());

        vm.HasGpaData.Should().BeTrue();
        vm.GpaSeries.Should().HaveCount(2);
        vm.HasCreditData.Should().BeTrue();
        vm.CreditSeries.Should().HaveCount(2);
        vm.HasConductData.Should().BeTrue();
        vm.ConductSeries.Should().ContainSingle();
        vm.HasDistributionData.Should().BeTrue();
        vm.DistributionSeries.Should().NotBeEmpty();
        vm.HasCourseScoreData.Should().BeTrue();
        vm.CourseFilterOptions.Should().HaveCount(3);

        vm.IsDistributionByCourses = true;
        vm.IsDistributionByCredits.Should().BeFalse();

        themeSubject.OnNext(AppTheme.Dark);
        vm.HasGpaData.Should().BeTrue();
    }
}
