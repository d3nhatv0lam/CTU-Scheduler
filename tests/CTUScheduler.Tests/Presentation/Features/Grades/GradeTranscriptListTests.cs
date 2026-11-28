using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Academic.Grades;
using CTUScheduler.Presentation.Features.Grades.Helpers;
using CTUScheduler.Presentation.Features.Grades.ViewModels.Components;
using FluentAssertions;
using ReactiveUI.Builder;
using Xunit;

namespace CTUScheduler.Tests.Presentation.Features.Grades;

public class GradeTranscriptListTests
{
    static GradeTranscriptListTests()
    {
        RxAppBuilder.CreateReactiveUIBuilder()
            .WithCoreServices()
            .BuildApp();
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
            new(1, "ML016", "Kinh tế chính trị Mác - Lênin", false, "12", 2, "A", GradeLetter.A, 9.0, null, true)
        };
        var sem3 = new SemesterGrade(2023, 3, sem3Courses, new SemesterGradeSummary(2, 12, 4.00, 3.94, null));

        var currentCourses = new List<CourseGrade>
        {
            new(1, "CT287", "Kiểm chứng mô hình", false, "01", 3, "A", GradeLetter.A, 9.2, null, false)
        };
        var currentSem = new SemesterGrade(2026, 1, currentCourses, null);

        return new GradeTranscript(
            [sem1, sem3, currentSem],
            new DateTimeOffset(2026, 11, 28, 14, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void GradeVisualHelper_FormatsSemesterWithoutSummerWordAndComputesKpis()
    {
        GradeVisualHelper.FormatSemesterTitle(2023, 3).Should().Be("Học kỳ 3 · 2023 - 2024");
        GradeVisualHelper.FormatShortSemesterLabel(2023, 3).Should().Be("HK3 (23-24)");

        var kpis = GradeVisualHelper.ComputeKpis(CreateSampleTranscript());
        kpis.LatestCumulativeGpa.Should().Be(3.94);
        kpis.TotalAccumulatedCredits.Should().Be(12);
        kpis.LatestConductScore.Should().Be(83);
        kpis.GradedCourseCount.Should().Be(4);
    }

    [Fact]
    public void GradeTranscriptListViewModel_FiltersSearchSemesterAndAdvisoryCourses()
    {
        using var vm = new GradeTranscriptListViewModel();
        var transcript = CreateSampleTranscript();

        vm.UpdateTranscript(transcript);

        // Mặc định ẩn SHCVHT và sắp xếp mới nhất lên đầu
        vm.SemesterGroups.Should().HaveCount(3);
        vm.SemesterGroups[0].Title.Should().Be("Học kỳ 1 · 2026 - 2027");
        vm.SemesterGroups[1].Title.Should().Be("Học kỳ 3 · 2023 - 2024");
        vm.SemesterGroups[2].Courses.Should().NotContain(c => c.CourseCode == "SHCVHT");
        vm.SemesterGroups[2].Courses.Should().ContainSingle(c => c.IsFailed && c.CourseCode == "CT199");

        // Hiện lại SHCVHT
        vm.HideAdvisoryCourses = false;
        vm.SemesterGroups[2].Courses.Should().Contain(c => c.CourseCode == "SHCVHT");

        // Lọc cả năm học 2023 - 2024 (2 học kỳ: HK3 và HK1)
        var year2023Node = vm.SemesterOptions[2];
        year2023Node.DisplayName.Should().Be("Năm học 2023 - 2024");
        year2023Node.Children.Should().HaveCount(2);

        vm.SelectedSemester = year2023Node;
        vm.SemesterGroups.Should().HaveCount(2);

        // Lọc riêng Học kỳ 3 trong nhánh con của năm học 2023 - 2024
        vm.SelectedSemester = year2023Node.Children[0];
        vm.SemesterGroups.Should().ContainSingle();
        vm.SemesterGroups[0].Title.Should().Be("Học kỳ 3 · 2023 - 2024");

        // Mô phỏng TreeComboBox tạm gán SelectedSemester = null khi reset selection vẫn lọc tiếp bình thường
        vm.SelectedSemester = null;
        vm.SemesterGroups.Should().HaveCount(3);
    }

    [Fact]
    public async Task GradeTranscriptListViewModel_SearchDebouncesRapidTypingAndSupportsClear()
    {
        using var vm = new GradeTranscriptListViewModel();
        vm.UpdateTranscript(CreateSampleTranscript());

        vm.SearchText = "CT";
        vm.SearchText = "CT2";
        vm.SearchText = "CT287";

        // Chưa hết thời gian debounce 300ms thì chưa lọc lại ngay
        vm.SemesterGroups.Should().HaveCount(3);

        await Task.Delay(500);

        vm.SemesterGroups.Should().ContainSingle();
        vm.SemesterGroups[0].Courses.Should().ContainSingle(c => c.CourseCode == "CT287");

        // Xóa SearchText về null (hành vi TextBox trên Avalonia) sẽ reset ngay không cần đợi debounce
        vm.SearchText = null;
        vm.SemesterGroups.Should().HaveCount(3);
    }
}
