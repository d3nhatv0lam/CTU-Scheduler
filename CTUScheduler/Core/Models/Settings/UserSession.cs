using System;
using System.Collections.Generic;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.UserSaves;

namespace CTUScheduler.Core.Models.Settings;

public record UserSession
{
    public RegistrationContext Context { get; set; }
    public List<Course> Courses { get; set; } = new ();
    public List<ScheduleProfile> ScheduleProfiles { get; set; } = new();
    public DateTimeOffset LastSaved { get; set; } = DateTimeOffset.Now;
}