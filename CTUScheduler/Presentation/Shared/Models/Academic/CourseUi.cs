using System.Collections.ObjectModel;

namespace CTUScheduler.Presentation.Shared.Models.Academic;

public class CourseUi
{
    public string Code { get; set; }
    public string Name_VN { get; set; }
    public int Credits { get; set; }
    public int TheorySessions { get; set; }
    public int PracticalSessions { get; set; }
    public ObservableCollection<CourseSectionUi> Sections  { get; set; } = new();
}