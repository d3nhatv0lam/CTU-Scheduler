using System;
using System.Collections.Generic;

namespace CTUScheduler.Core.Models.Academic.Curriculum.CourseData;

public class Course
{
    public string Code { get; set; }
    public string Name_VN { get; set; }
    public int Credits { get; set; }
    public int TheorySessions { get; set; }
    public int PracticalSessions { get; set; }
    public List<CourseSection> Sections { get; set; } = new();
    
    /// <summary>
    /// Create a new course with a single filled section (Optimized overload)
    /// </summary>
    public Course WithSection(CourseSection section)
    {
        if (section is null) 
            throw new ArgumentNullException(nameof(section));

        if (section.Code != this.Code)
        {
            throw new ArgumentException(
                $"Section must belong to course {this.Code}, " +
                $"but found section from course {section.Code}"
            );
        }
        var newCourse = (Course)this.MemberwiseClone();
        newCourse.Sections = new List<CourseSection>(1) { section };
        return newCourse;
    }
    
    /// <summary>
    ///  Create a new course with filled sections
    /// </summary>
    /// <param name="sections">Section from this course</param>
    /// <returns>New course with sections</returns>
    /// <exception cref="ArgumentException">Section is null or Section not from this Course</exception>
    public Course WithSections(IReadOnlyList<CourseSection> sections)
    {
        if (sections is null) throw new ArgumentNullException(nameof(sections));
        foreach (var section in sections)
        {
            if (section.Code != this.Code)
                throw new ArgumentException($"Mismatch code: Expected {this.Code}, found {section.Code}");
        }

        var newCourse = (Course)this.MemberwiseClone();
        newCourse.Sections = new List<CourseSection>(sections);
        return newCourse;
    }
}