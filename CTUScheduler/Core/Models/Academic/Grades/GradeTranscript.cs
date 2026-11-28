using System;
using System.Collections.Generic;

namespace CTUScheduler.Core.Models.Academic.Grades;

/// <summary>
/// Semesters sắp xếp tăng dần theo (AcademicYear, Term)
/// </summary>
/// <param name="Semesters"></param>
/// <param name="FetchedAt"></param>
public record GradeTranscript(
    IReadOnlyList<SemesterGrade> Semesters,
    DateTimeOffset FetchedAt);
