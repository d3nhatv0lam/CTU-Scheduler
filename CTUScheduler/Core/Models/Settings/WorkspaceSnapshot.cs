using System;
using System.Collections.Generic;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;

namespace CTUScheduler.Core.Models.Settings;

public record WorkspaceSnapshot
{
    public RegistrationContext Context { get; init; } = RegistrationContext.Unknown;
    public List<Course> Courses { get; set; } = new();
    public List<ScheduleProfile> Profiles { get; set; } = new();
    public DateTimeOffset LastModified { get; init; } = DateTimeOffset.Now;
}