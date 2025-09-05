using System.Collections.ObjectModel;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;

namespace CTUScheduler.Presentation.Shared.Models.Academic;

public class CourseSectionUi
{
    public int Key { get; set; }
    public string Code { get; set; }
    public  string Group { get; set; }
    public string Lecturer { get; set; }
    public string LecturerEmail { get; set; }
    public int TotalStudents { get; set; }
    public int RemainingStudents { get; set; }
    public ObservableCollection<ClassDay> ClassDays { get; set; } = new ();
}