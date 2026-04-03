using System;
using System.Collections.Generic;

namespace CTUScheduler.Core.Models.Academic.Curriculum.Schedule;

public class ScheduleProfile
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = "Unnamed";
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string> SavedCourseGroupKeys { get; init; } = new();
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.Now;
}