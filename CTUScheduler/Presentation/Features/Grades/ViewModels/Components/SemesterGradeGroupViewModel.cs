using System.Collections.Generic;
using System.Globalization;
using CTUScheduler.Core.Models.Academic.Grades;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Grades.Helpers;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Features.Grades.ViewModels.Components;

public sealed partial class SemesterGradeGroupViewModel : ViewModelBase
{
    [Reactive] private bool _isExpanded;

    public SemesterGradeGroupViewModel(
        SemesterGrade semester,
        IReadOnlyList<CourseGradeItemViewModel> courses,
        bool isExpanded)
    {
        AcademicYearStart = semester.AcademicYearStart;
        Term = semester.Term;
        Title = GradeVisualHelper.FormatSemesterTitle(semester.AcademicYearStart, semester.Term);
        Courses = courses;
        IsExpanded = isExpanded;
        IsFinalized = semester.Summary is not null;

        if (semester.Summary is { } summary)
        {
            string drl = summary.ConductScore.HasValue
                ? $" · ĐRL: {summary.ConductScore.Value}"
                : string.Empty;

            SummaryText = string.Create(
                CultureInfo.InvariantCulture,
                $"ĐTB: {summary.SemesterGpa:0.00} · Tích lũy: {summary.CumulativeGpa:0.00} · {summary.SemesterAccumulatedCredits}/{summary.TotalAccumulatedCredits} TC{drl}");
        }
        else
        {
            SummaryText = "Đang học (chưa tổng kết)";
        }
    }

    public int AcademicYearStart { get; }
    public int Term { get; }
    public string Title { get; }
    public string SummaryText { get; }
    public bool IsFinalized { get; }
    public IReadOnlyList<CourseGradeItemViewModel> Courses { get; }
}
