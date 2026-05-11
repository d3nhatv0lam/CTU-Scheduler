using System.Collections.Generic;
using CTUScheduler.Presentation.Shared.Controls.Timeline;

namespace CTUScheduler.Core.Models.TeachingPlan;

public class TeachingPlanData
{
    public List<TimelineNode> RegistrationTimeline { get; set; } = new();
}