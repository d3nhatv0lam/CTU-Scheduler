using CTUScheduler.Core.Models.Academic.Curriculum.Registration;
using CTUScheduler.Core.Models.Settings;

namespace CTUScheduler.Infrastructure.Sites.CTU.Extensions;

public static class RegistrationExtensions
{
    public static RegistrationContext ToContext(this RegistrationInformation info)
    {
        return new RegistrationContext
        {
            AcademicYear = info.AcademicYear,
            Semester = info.Semester,
            MaxCreditPerSemester = info.MaxCreditPerSemester
        };
    }
}