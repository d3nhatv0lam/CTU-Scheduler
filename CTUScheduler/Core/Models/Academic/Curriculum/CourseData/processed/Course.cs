using System;
using System.Collections.Generic;
using System.Linq;

namespace CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;

public class Course
{
    public string Code { get; set; }
    public string Name_VN { get; set; }
    public int Credits { get; set; }
    public int TheorySessions { get; set; }
    public int PracticalSessions { get; set; }
    public List<CourseSection> Sections { get; set; } = new();
    
    /// <summary>
    ///  Create a new course with filled sections
    /// </summary>
    /// <param name="sections">Section from this course</param>
    /// <returns>New course with sections</returns>
    /// <exception cref="ArgumentException">Section is null or Section not from this Course</exception>
    public Course WithSections(IReadOnlyList<CourseSection> sections)
    {
        if (sections is null) 
            throw new ArgumentNullException(nameof(sections));
        if (sections.Any(s => s.Code != this.Code))
            throw new ArgumentException(
                $"All sections must belong to course {this.Code}, " +
                $"but found section from course {sections.First(s => s.Code != this.Code).Code}"
            );
        var newCourse = (Course)this.MemberwiseClone();
        newCourse.Sections = new List<CourseSection>(sections);
        return newCourse;
    }
}