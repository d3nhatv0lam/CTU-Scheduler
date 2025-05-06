using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Raw;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed
{
    public class RegistrationInformation
    {
        public int AcademicYear { get; set; }
        public string Semester { get; set; }
        public int MaxCreditPerSemester { get; set; }
        public string Period { get; set; }
        public List<GroupItem> Groups { get; set; } = new List<GroupItem>();
        public List<PeriodItem> UserPeriod { get; set; } = new();
    }
}
