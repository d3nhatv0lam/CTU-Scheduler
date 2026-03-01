using System.Collections.ObjectModel;

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
    public ObservableCollection<ClassDayUi> ClassDays { get; set; } = new ();
}