using System;
using System.Collections.Generic;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;

namespace CTUScheduler.Core.Models.UserSaves
{
    public class ScheduleSave
    {
        public RegistrationContext Context { get; set; }
        public string Semester { get; set; } = string.Empty;
        public int AcademicYear { get; set; }
        public List<Course> Courses { get; set; } = new ();
        public List<ScheduleProfile> ScheduleTables { get; set; } = new();
        public DateTime LastSaved { get; set; } = DateTime.Now;
    }
}
