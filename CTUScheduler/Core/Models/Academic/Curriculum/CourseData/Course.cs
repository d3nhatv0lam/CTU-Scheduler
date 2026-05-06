using System;
using System.Collections.Generic;

namespace CTUScheduler.Core.Models.Academic.Curriculum.CourseData;

public record Course(
    string Code,
    string Name_VN,
    int Credits,
    int TheorySessions,
    int PracticalSessions,
    IReadOnlyList<CourseSection> Sections
)
{
    /// <summary>
    /// Create a new course with a single filled section (Optimized overload)
    /// </summary>
    /// <param name="section">Section from this course</param>
    /// <returns>New course with sections</returns>
    /// <exception cref="ArgumentException">Section is null or Section not from this Course</exception>
    public Course WithSection(CourseSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        if (section.Code != this.Code)
        {
            throw new ArgumentException(
                $"Section must belong to course {this.Code}, " +
                $"but found section from course {section.Code}"
            );
        }

        return this with { Sections = [section] };
    }

    /// <summary>
    ///  Create a new course with filled sections
    /// </summary>
    /// <param name="sections">Section from this course</param>
    /// <returns>New course with sections</returns>
    /// <exception cref="ArgumentException">Section is null or Section not from this Course</exception>
    public Course WithSections(IReadOnlyList<CourseSection> sections)
    {
        ArgumentNullException.ThrowIfNull(sections);

        foreach (var section in sections)
        {
            if (section.Code != this.Code)
                throw new ArgumentException($"Mismatch code: Expected {this.Code}, found {section.Code}");
        }

        return this with { Sections = sections };
    }
}