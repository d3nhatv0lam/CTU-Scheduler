using System;
using DynamicData;

namespace CTUScheduler.Presentation.Features.Scheduling.Models.Context;

public record SchedulingWizardContext : IDisposable
{
    public SourceList<CourseBlueprint> CourseBlueprints { get; init; } = new();

    public void Dispose()
    {
        CourseBlueprints.Dispose();
    }
}