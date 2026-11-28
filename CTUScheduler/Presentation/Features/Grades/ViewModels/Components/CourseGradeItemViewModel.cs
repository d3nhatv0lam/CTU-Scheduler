using System.Globalization;
using CTUScheduler.Core.Models.Academic.Grades;
using CTUScheduler.Presentation.Features.Grades.Helpers;

namespace CTUScheduler.Presentation.Features.Grades.ViewModels.Components;

public sealed class CourseGradeItemViewModel
{
    public CourseGradeItemViewModel(CourseGrade source)
    {
        Index = source.Index;
        CourseCode = source.CourseCode;
        CourseName = source.CourseName;
        Credits = source.Credits;
        GroupDisplay = string.IsNullOrWhiteSpace(source.GroupCode) ? "-" : source.GroupCode;
        LetterGrade = source.LetterGrade;
        GradeLabel = source.LetterGrade != GradeLetter.Unknown
            ? source.LetterGrade.ToDisplayLabel()
            : source.RawLetterGrade ?? "-";
        ScoreDisplay = source.NumericScore.HasValue
            ? source.NumericScore.Value.ToString("0.0", CultureInfo.InvariantCulture)
            : source.RawScoreNote ?? "-";
        IsFailed = source.LetterGrade == GradeLetter.F;
        IsAccumulated = source.IsAccumulated;
        AccumulatedDisplay = source.IsAccumulated ? "*" : string.Empty;
        IsConditionCourse = source.IsConditionCourse;
        IsAdvisoryActivity = source.IsAdvisoryActivity;
    }

    public int Index { get; }
    public string CourseCode { get; }
    public string CourseName { get; }
    public int Credits { get; }
    public string GroupDisplay { get; }
    public GradeLetter LetterGrade { get; }
    public string GradeLabel { get; }
    public string ScoreDisplay { get; }
    public bool IsFailed { get; }
    public bool IsAccumulated { get; }
    public string AccumulatedDisplay { get; }
    public bool IsConditionCourse { get; }
    public bool IsAdvisoryActivity { get; }
}
