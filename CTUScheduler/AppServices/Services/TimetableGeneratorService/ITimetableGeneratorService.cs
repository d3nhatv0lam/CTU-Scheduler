using System;
using System.Collections.Generic;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Timetable;

namespace CTUScheduler.AppServices.Services.TimetableGeneratorService;

public interface ITimetableGeneratorService
{
    IObservable<IReadOnlyList<SectionChoice>> Generate(IEnumerable<IReadOnlyList<SectionChoice>> sets,
        ScheduleGenerationOptions? options = null);
}