using System.Collections.Generic;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Infrastructure.Excel;
using CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;
using CTUScheduler.Presentation.Services.ControlRenderer;
using CTUScheduler.Presentation.Services.Theme;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CTUScheduler.Tests.Presentation.Features.TimetableRefactor;

public class TimetablePreviewViewModelTests
{
    private readonly IExcelExporterService _excelExporter = Substitute.For<IExcelExporterService>();
    private readonly IControlRendererService _controlRendererService = Substitute.For<IControlRendererService>();
    private readonly ITimetablePreviewRenderer _timetablePreviewRenderer = Substitute.For<ITimetablePreviewRenderer>();
    private readonly IUserInteractionService _userInteractionService = Substitute.For<IUserInteractionService>();
    private readonly IThemeService _themeService = Substitute.For<IThemeService>();

    private static (Course Course, CourseSection Section) CreateCourseAndSection(
        string code,
        string name,
        string group,
        int total,
        int remaining)
    {
        var section = new CourseSection(
            IsCancelled: false,
            Key: 1,
            Code: code,
            Group: group,
            Lecturer: "Giảng viên A",
            LecturerEmail: "a@ctu.edu.vn",
            TotalStudents: total,
            RemainingStudents: remaining,
            ClassDays: new List<ClassDay>()
        );

        var course = new Course(
            Code: code,
            Name_VN: name,
            Credits: 3,
            TheorySessions: 30,
            PracticalSessions: 0,
            Sections: new[] { section }
        );

        return (course, section);
    }

    [Fact]
    public void WhenAllSectionsHaveRemainingSlots_HasFullSectionsShouldBeFalse()
    {
        var (c1, s1) = CreateCourseAndSection("CT178", "Mạng máy tính", "01", 60, 10);
        var (c2, s2) = CreateCourseAndSection("CT101", "Lập trình căn bản", "02", 45, 5);

        var choices = new[]
        {
            new SectionChoice(c1, s1),
            new SectionChoice(c2, s2)
        };

        var vm = new TimetablePreviewViewModel(
            choices,
            _excelExporter,
            _controlRendererService,
            _timetablePreviewRenderer,
            _userInteractionService,
            _themeService);

        vm.HasFullSections.Should().BeFalse();
        vm.FullSectionsWarningText.Should().BeNull();
    }

    [Fact]
    public void WhenASectionHasZeroRemainingSlots_HasFullSectionsShouldBeTrue()
    {
        var (c1, s1) = CreateCourseAndSection("CT178", "Mạng máy tính", "01", 60, 0);
        var (c2, s2) = CreateCourseAndSection("CT101", "Lập trình căn bản", "02", 45, 5);

        var choices = new[]
        {
            new SectionChoice(c1, s1),
            new SectionChoice(c2, s2)
        };

        var vm = new TimetablePreviewViewModel(
            choices,
            _excelExporter,
            _controlRendererService,
            _timetablePreviewRenderer,
            _userInteractionService,
            _themeService);

        vm.HasFullSections.Should().BeTrue();
        vm.FullSectionsWarningText.Should().NotBeNullOrEmpty();
        vm.FullSectionsWarningText.Should().Contain("CT178");
        vm.FullSectionsWarningText.Should().Contain("Mạng máy tính");
        vm.FullSectionsWarningText.Should().Contain("0/60");
        vm.FullSectionsWarningText.Should().NotContain("CT101");
    }

    [Fact]
    public void WhenMultipleSectionsHaveZeroRemainingSlots_FullSectionsWarningTextShouldContainAll()
    {
        var (c1, s1) = CreateCourseAndSection("CT178", "Mạng máy tính", "01", 60, 0);
        var (c2, s2) = CreateCourseAndSection("CT101", "Lập trình căn bản", "02", 45, 0);

        var choices = new[]
        {
            new SectionChoice(c1, s1),
            new SectionChoice(c2, s2)
        };

        var vm = new TimetablePreviewViewModel(
            choices,
            _excelExporter,
            _controlRendererService,
            _timetablePreviewRenderer,
            _userInteractionService,
            _themeService);

        vm.HasFullSections.Should().BeTrue();
        vm.FullSectionsWarningText.Should().Contain("Có 2 lớp học phần đã hết slot:");
        vm.FullSectionsWarningText.Should().Contain("CT178");
        vm.FullSectionsWarningText.Should().Contain("CT101");
    }
}
