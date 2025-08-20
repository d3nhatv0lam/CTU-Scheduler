using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Core.Extensions;

public static class ScheduleCellExtension
{
    public static void SetCell(this ScheduleCell scheduleCell, SectionChoice choice)
    {
        // scheduleCell.CourseCode = choice.Course.Code;
        // scheduleCell.CourseName_VN = choice.Course.Name_VN;
        // scheduleCell.Group = choice.Section.Group;
        // scheduleCell.TotalStudents = choice.Section.TotalStudents;
        // scheduleCell.RemainingStudents = choice.Section.RemainingStudents;
        // scheduleCell.Lecturer = choice.Section.Lecturer;
        // scheduleCell.Credit = choice.Course.Credit;
        // public string CourseCode { get; set; }
        // public string CourseName_VN { get; set; }
        // public string Group { get; set; }
        // public int TotalStudents { get; set; }
        // public int RemainingStudents { get; set; }
        // public string Room { get; set; }
        // public int AttendingDay { get; set; } = DEFAULT_ATTENDING_DAY;
        // public int StartPeriod { get; set; } = DEFAULT_START_PERIOD;
        // public int NumberOfPeriods { get; set; } = DEFAULT_NUMBER_OF_PERIODS;
        // public string Lecturer { get; set; }
        // public int Credit { get; set; }
    }
}