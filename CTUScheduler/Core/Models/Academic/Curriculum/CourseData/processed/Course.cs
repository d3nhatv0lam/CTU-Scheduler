using System.Collections.Generic;

namespace CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;

public class Course
{
    public string Code { get; set; }
    public string Name_VN { get; set; }
    public int Credit { get; set; }
    public int TheorySessions { get; set; }
    public int PracticalSessions { get; set; }
    public List<CourseSection> Sections { get; set; } = new();
}