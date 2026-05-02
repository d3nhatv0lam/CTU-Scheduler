using System.Collections.Generic;

namespace CTUScheduler.Core.Models.TeachingPlan;

public class TeachingPlanData
{
    public string Title { get; set; } = string.Empty;
    public int Semester { get; set; }
    public string SchoolYear { get; set; } = string.Empty;
    public List<RegistrationTimelineItem> RegistrationTimeline { get; set; } = new();
}