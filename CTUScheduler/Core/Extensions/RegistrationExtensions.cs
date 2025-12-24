using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;
using CTUScheduler.Core.Models.UserSaves;

namespace CTUScheduler.Core.Extensions;

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