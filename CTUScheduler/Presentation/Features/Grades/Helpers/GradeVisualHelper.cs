using System.Linq;
using CTUScheduler.Core.Models.Academic.Grades;

namespace CTUScheduler.Presentation.Features.Grades.Helpers;

public sealed record GradeKpiSummary(
    double? LatestCumulativeGpa,
    int TotalAccumulatedCredits,
    int? LatestConductScore,
    int GradedCourseCount);

public static class GradeVisualHelper
{
    public static string FormatSemesterTitle(int academicYearStart, int term) =>
        $"Học kỳ {term} · {academicYearStart} - {academicYearStart + 1}";

    public static string FormatShortSemesterLabel(int academicYearStart, int term) =>
        $"HK{term} ({academicYearStart % 100:D2}-{(academicYearStart + 1) % 100:D2})";

    public static string ToDisplayLabel(this GradeLetter letter) => letter switch
    {
        GradeLetter.A => "A",
        GradeLetter.BPlus => "B+",
        GradeLetter.B => "B",
        GradeLetter.CPlus => "C+",
        GradeLetter.C => "C",
        GradeLetter.DPlus => "D+",
        GradeLetter.D => "D",
        GradeLetter.F => "F",
        GradeLetter.M => "M",
        GradeLetter.I => "I",
        _ => "-"
    };

    public static string ToColorHex(this GradeLetter letter) => letter switch
    {
        GradeLetter.A => "#2CAFFE",
        GradeLetter.BPlus => "#544FC5",
        GradeLetter.B => "#00E272",
        GradeLetter.CPlus => "#FE6A35",
        GradeLetter.C => "#6B8ABC",
        GradeLetter.DPlus => "#D568FB",
        GradeLetter.D => "#2EE0CA",
        GradeLetter.F => "#FA4B42",
        GradeLetter.M => "#8C8C8C",
        GradeLetter.I => "#FAAD14",
        _ => "#9CA3AF"
    };

    public static GradeKpiSummary ComputeKpis(GradeTranscript? transcript)
    {
        if (transcript is null || transcript.Semesters.Count == 0)
        {
            return new GradeKpiSummary(null, 0, null, 0);
        }

        var orderedSemesters = transcript.Semesters
            .OrderBy(s => s.AcademicYearStart)
            .ThenBy(s => s.Term)
            .ToList();

        var latestFinalized = orderedSemesters
            .Select(s => s.Summary)
            .LastOrDefault(s => s is not null);

        int? latestConductScore = orderedSemesters
            .Select(s => s.Summary?.ConductScore)
            .LastOrDefault(v => v.HasValue);

        int gradedCourseCount = orderedSemesters
            .SelectMany(s => s.Courses)
            .Count(c => !c.IsAdvisoryActivity && c.HasNumericScore);

        return new GradeKpiSummary(
            LatestCumulativeGpa: latestFinalized?.CumulativeGpa,
            TotalAccumulatedCredits: latestFinalized?.TotalAccumulatedCredits ?? 0,
            LatestConductScore: latestConductScore,
            GradedCourseCount: gradedCourseCount);
    }
}
