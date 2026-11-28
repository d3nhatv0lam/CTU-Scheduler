using System.Collections.Generic;

namespace CTUScheduler.Core.Models.Academic.Grades;

/// <summary>
/// Represents grade information for a semester, including the academic year, term, course grades, and aggregate performance summary.
/// </summary>
/// <param name="AcademicYearStart">The starting year of the academic year to which the semester belongs.</param>
/// <param name="Term">The term number within the academic year. include 1,2,3</param>
/// <param name="Courses">The collection of course grades recorded for the semester.</param>
/// <param name="Summary">The summarized credit, GPA, and conduct information for the semester.</param>
public record SemesterGrade(
    int AcademicYearStart,
    int Term,
    IReadOnlyList<CourseGrade> Courses,
    SemesterGradeSummary? Summary);
