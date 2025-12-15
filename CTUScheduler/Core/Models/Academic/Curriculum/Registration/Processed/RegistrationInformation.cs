using System.Collections.Generic;

namespace CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed
{
    public class RegistrationInformation
    {
        public int AcademicYear { get; set; }
        public string Semester { get; set; } = string.Empty;
        public int MaxCreditPerSemester { get; set; }
        public string Period { get; set; } = string.Empty;
        public List<GroupItem> Groups { get; set; } = new ();
        public List<PeriodItem> UserPeriod { get; set; } = new();
    }
}
