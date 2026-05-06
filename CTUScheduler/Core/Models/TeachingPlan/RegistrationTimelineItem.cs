using System;

namespace CTUScheduler.Core.Models.TeachingPlan;

public class RegistrationTimelineItem
{
    public string Description { get; set; } = string.Empty;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}