namespace CTUScheduler.Core.Models.Academic.Curriculum.CourseData;

/// <summary>
/// Represents a course that is planned to be registered. Fetched from the CTU Kế hoạch học tập.
/// </summary>
/// <param name="Code"></param>
/// <param name="NameVn"></param>
/// <param name="Credits"></param>
/// <param name="Group"></param>
/// <param name="ScheduleText"></param>
/// <param name="LecturerName"></param>
/// <param name="LecturerEmail"></param>
/// <param name="IsRegistered"></param>
public record PlannedCourse(
    string Code,
    string NameVn,
    int Credits,
    string? Group,
    string? ScheduleText, // example: "1-12_40;41;42;43;58;59;60"
    string? LecturerName,
    string? LecturerEmail,
    bool IsRegistered); // != 1 is false