using System.Collections.Generic;
using System.Linq;
using CTUScheduler.Core.Models.Academic.Grades;

namespace CTUScheduler.Presentation.Features.Grades.Helpers;

public sealed record SemesterTrendPoint(
    int AcademicYearStart,
    int Term,
    string ShortLabel,
    string FullTitle,
    double SemesterGpa,
    double CumulativeGpa,
    int SemesterAccumulatedCredits,
    int TotalAccumulatedCredits,
    int? ConductScore);

public sealed record LetterDistributionPoint(
    GradeLetter Letter,
    string Label,
    string ColorHex,
    int CourseCount,
    int Credits,
    double Value,
    double Percentage);

public sealed record CourseScorePoint(
    string CourseCode,
    string CourseName,
    double NumericScore,
    GradeLetter LetterGrade,
    int Credits,
    string SemesterTitle);

public static class GradeStatisticsHelper
{
    private static readonly GradeLetter[] OrderedLetters =
    [
        GradeLetter.A,
        GradeLetter.BPlus,
        GradeLetter.B,
        GradeLetter.CPlus,
        GradeLetter.C,
        GradeLetter.DPlus,
        GradeLetter.D,
        GradeLetter.F,
        GradeLetter.M,
        GradeLetter.I
    ];

    public static IReadOnlyList<SemesterTrendPoint> BuildSemesterTrends(GradeTranscript? transcript)
    {
        if (transcript is null || transcript.Semesters.Count == 0)
        {
            return [];
        }

        return transcript.Semesters
            .Where(s => s.Summary is not null)
            .OrderBy(s => s.AcademicYearStart)
            .ThenBy(s => s.Term)
            .Select(s =>
            {
                var summary = s.Summary!;
                return new SemesterTrendPoint(
                    AcademicYearStart: s.AcademicYearStart,
                    Term: s.Term,
                    ShortLabel: GradeVisualHelper.FormatShortSemesterLabel(s.AcademicYearStart, s.Term),
                    FullTitle: GradeVisualHelper.FormatSemesterTitle(s.AcademicYearStart, s.Term),
                    SemesterGpa: summary.SemesterGpa,
                    CumulativeGpa: summary.CumulativeGpa,
                    SemesterAccumulatedCredits: summary.SemesterAccumulatedCredits,
                    TotalAccumulatedCredits: summary.TotalAccumulatedCredits,
                    ConductScore: summary.ConductScore);
            })
            .ToList();
    }

    public static IReadOnlyList<LetterDistributionPoint> BuildLetterDistribution(
        GradeTranscript? transcript,
        bool byCredits)
    {
        if (transcript is null || transcript.Semesters.Count == 0)
        {
            return [];
        }

        var courses = transcript.Semesters
            .SelectMany(s => s.Courses)
            .Where(c => !c.IsAdvisoryActivity && c.LetterGrade != GradeLetter.Unknown)
            .ToList();

        if (courses.Count == 0)
        {
            return [];
        }

        var grouped = courses
            .GroupBy(c => c.LetterGrade)
            .ToDictionary(
                g => g.Key,
                g => (Count: g.Count(), Credits: g.Sum(c => c.Credits)));

        double total = byCredits
            ? grouped.Values.Sum(v => v.Credits)
            : grouped.Values.Sum(v => v.Count);

        if (total <= 0)
        {
            return [];
        }

        var result = new List<LetterDistributionPoint>();
        foreach (var letter in OrderedLetters)
        {
            if (!grouped.TryGetValue(letter, out var stats))
            {
                continue;
            }

            double value = byCredits ? stats.Credits : stats.Count;
            if (value <= 0)
            {
                continue;
            }

            result.Add(new LetterDistributionPoint(
                Letter: letter,
                Label: letter.ToDisplayLabel(),
                ColorHex: letter.ToColorHex(),
                CourseCount: stats.Count,
                Credits: stats.Credits,
                Value: value,
                Percentage: value / total * 100.0));
        }

        return result;
    }

    public static IReadOnlyList<CourseScorePoint> BuildCourseScorePoints(
        GradeTranscript? transcript,
        int? academicYearStart = null,
        int? term = null)
    {
        if (transcript is null || transcript.Semesters.Count == 0)
        {
            return [];
        }

        var semesters = transcript.Semesters
            .Where(s => !academicYearStart.HasValue || s.AcademicYearStart == academicYearStart.Value)
            .Where(s => !term.HasValue || s.Term == term.Value)
            .OrderBy(s => s.AcademicYearStart)
            .ThenBy(s => s.Term);

        return semesters
            .SelectMany(s =>
            {
                string semTitle = GradeVisualHelper.FormatSemesterTitle(s.AcademicYearStart, s.Term);
                return s.Courses
                    .Where(c => !c.IsAdvisoryActivity && c.NumericScore.HasValue)
                    .OrderBy(c => c.Index)
                    .Select(c => new CourseScorePoint(
                        CourseCode: c.CourseCode,
                        CourseName: c.CourseName,
                        NumericScore: c.NumericScore!.Value,
                        LetterGrade: c.LetterGrade,
                        Credits: c.Credits,
                        SemesterTitle: semTitle));
            })
            .ToList();
    }
}
