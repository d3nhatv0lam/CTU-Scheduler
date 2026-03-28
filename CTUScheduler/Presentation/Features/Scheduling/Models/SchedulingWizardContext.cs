using DynamicData;

namespace CTUScheduler.Presentation.Features.Scheduling.Models;

public record SchedulingWizardContext
{
    public SourceList<SelectedCourseNode> SelectedCourses { get; init; } = new();
};