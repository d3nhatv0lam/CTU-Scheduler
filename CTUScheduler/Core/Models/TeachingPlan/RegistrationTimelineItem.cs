using System;

namespace CTUScheduler.Core.Models.TeachingPlan;

public class RegistrationTimelineItem
{
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
}