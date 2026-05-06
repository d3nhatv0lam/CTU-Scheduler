using System;
using DynamicData;

namespace CTUScheduler.Presentation.Features.Scheduling.Models.Context;

public class SchedulingWizardContext : IDisposable
{
    private readonly IDisposable _cleanUpTrigger;
    public SourceList<CourseBlueprint> CourseBlueprints { get; } = new();

    public SchedulingWizardContext()
    {
        _cleanUpTrigger = CourseBlueprints.Connect()
            .DisposeMany()
            .Subscribe();
    }

    public void Dispose()
    {
        _cleanUpTrigger.Dispose();
        CourseBlueprints.Dispose();
    }
}