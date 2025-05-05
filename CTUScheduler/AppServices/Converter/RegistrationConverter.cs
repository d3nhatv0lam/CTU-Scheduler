using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Raw;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Converter
{
    public static class RegistrationConverter
    {
        public static RegistrationInformation ToRegistrationInformation(this RawRegistrationInformation rawRegistrationInformation)
        {
            RegistrationInformation info = new RegistrationInformation();

            info.AcademicYear = rawRegistrationInformation.namhoc;
            info.Semester = rawRegistrationInformation.hocky;
            info.Period = string.Concat('(', info.AcademicYear,'-',info.AcademicYear+1,')');

            return info;
        }
    }
}
