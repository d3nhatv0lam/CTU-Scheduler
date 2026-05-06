using System.Collections.Generic;

namespace CTUScheduler.Core.Models.Academic.Curriculum.Registration;

public record RegistrationInformation(
    int? AcademicYear,
    string? Semester,
    int? MaxCreditPerSemester,
    string? Period,
    IReadOnlyList<GroupItem> Groups,
    UserPeriodItem? UserPeriod
);