using System.Collections.Generic;
using System.Text;

namespace CTUScheduler.Core.Models.Academic.Curriculum.CourseData;

public record CourseSection(
    bool IsCancelled,
    int Key,
    string Code,
    string Group,
    string Lecturer,
    string LecturerEmail,
    int TotalStudents,
    int RemainingStudents,
    IReadOnlyList<ClassDay> ClassDays
)
{
    public override string ToString()
    {
        StringBuilder strBuilder = new StringBuilder($"Nhóm:{Group} - {Lecturer} - {RemainingStudents}/{TotalStudents}\n");
        strBuilder.Append("ClassDays:\n");

        foreach (var classDayData in ClassDays)
        {
            strBuilder.Append($"AttendingDay: {classDayData.AttendingDay} {classDayData.Period} {classDayData.Room}\n");
        }
        return strBuilder.ToString();
    }
}
