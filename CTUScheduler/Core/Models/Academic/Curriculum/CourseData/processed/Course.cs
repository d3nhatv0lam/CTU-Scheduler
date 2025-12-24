using System.Collections.Generic;

namespace CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;

public class Course
{
    public string Code { get; set; }
    public string Name_VN { get; set; }
    public int Credits { get; set; }
    public int TheorySessions { get; set; }
    public int PracticalSessions { get; set; }
    public List<CourseSection> Sections { get; set; } = new();
    
    public Course CloneWithNewCourseSections(IEnumerable<CourseSection> sections)
    {
        var newCourse = (Course)this.MemberwiseClone();
        newCourse.Sections = new List<CourseSection>(sections);
        return newCourse;
    }
}