using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using DynamicData;

namespace CTUScheduler.AppServices.Models;

public class EditableCourse
{
    public string Code { get; set; }
    public string Name_VN { get; set; }
    public int Credit { get; set; }
    public int TheorySessions { get; set; }
    public int PracticalSessions { get; set; }
    public SourceCache<CourseSection,string> Sections  { get; set; } = new(section => section.Group);
}