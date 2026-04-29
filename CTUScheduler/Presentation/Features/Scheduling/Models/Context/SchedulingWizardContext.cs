using DynamicData;

namespace CTUScheduler.Presentation.Features.Scheduling.Models.Context;

public record SchedulingWizardContext
{
    public SourceList<SelectedCourse> SelectedCourses { get; init; } = new();
};