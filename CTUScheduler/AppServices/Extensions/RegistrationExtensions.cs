using CTUScheduler.Core.Models.Academic.Curriculum.Registration;
using CTUScheduler.Core.Models.Settings;

namespace CTUScheduler.AppServices.Extensions;

public static class RegistrationExtensions
{
    public static RegistrationContext? ToContext(this RegistrationInformation info)
    {
        if (info.AcademicYear is null || string.IsNullOrEmpty(info.Semester) || info.MaxCreditPerSemester is null)
            return null;

        if (!int.TryParse(info.Semester, out var semesterInt))
            return null;

        return new RegistrationContext(
            AcademicYear: info.AcademicYear.Value,
            Semester: semesterInt,
            MaxCreditPerSemester: info.MaxCreditPerSemester.Value);
    }
}