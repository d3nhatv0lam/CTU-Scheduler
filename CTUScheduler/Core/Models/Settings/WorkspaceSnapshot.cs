using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;

namespace CTUScheduler.Core.Models.Settings;

public record WorkspaceSnapshot(
    RegistrationContext? Context, 
    [property:JsonRequired] IReadOnlyList<Course> Courses, 
    [property:JsonRequired]IReadOnlyList<ScheduleProfile> Profiles, 
    DateTimeOffset LastModified
);