using CTUScheduler.Core.Models.Academic.Curriculum.Registration;
using CTUScheduler.Core.Models.Settings;

namespace CTUScheduler.AppServices.Extensions;

public static class RegistrationExtensions
{
    public static RegistrationContext? ToContext(this RegistrationInformation info)
    {
        if (info.AcademicYear is null || string.IsNullOrEmpty(info.Semester) || info.MaxCreditPerSemester is null)
            return null;

        return new RegistrationContext
        {
            AcademicYear = info.AcademicYear.Value,
            Semester = info.Semester,
            MaxCreditPerSemester = info.MaxCreditPerSemester.Value
        };
    }
}