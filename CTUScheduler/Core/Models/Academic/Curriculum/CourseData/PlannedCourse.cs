namespace CTUScheduler.Core.Models.Academic.Curriculum.CourseData;

public record PlannedCourse(
    string Code,
    string NameVn,
    int Credits,
    string? Group,
    string? ScheduleText, // example: "1-12_40;41;42;43;58;59;60"
    string? LecturerName,
    string? LecturerEmail,
    bool IsRegistered); // != 1 is false