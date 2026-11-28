using System;

namespace CTUScheduler.Core.Models.Academic.Grades;

public record CourseGrade(
    int Index,
    string CourseCode,
    string CourseName,
    bool IsConditionCourse,
    string? GroupCode,
    int Credits,
    string? RawLetterGrade,
    GradeLetter LetterGrade,
    // Điểm số
    double? NumericScore,
    //Điểm số (ghi chú chữ), ví dụ *(BL), đa số còn laị là null
    string? RawScoreNote,
    bool IsAccumulated)
{
    public bool IsAdvisoryActivity =>
        string.Equals(CourseCode, "SHCVHT", StringComparison.OrdinalIgnoreCase) || Credits == 0;

    public bool IsExempted => LetterGrade == GradeLetter.M;

    public bool HasNumericScore => NumericScore.HasValue;

}
