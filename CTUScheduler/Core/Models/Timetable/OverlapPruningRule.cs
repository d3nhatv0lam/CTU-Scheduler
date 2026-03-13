using System.Collections.Generic;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Validators;

namespace CTUScheduler.Core.Models.Timetable;

public class OverlapPruningRule: IPruningRule
{
    public bool CanContinue(IReadOnlyList<SectionChoice> currentPath, SectionChoice nextCandidate)
    {
        return ScheduleValidator.ValidateStep(currentPath, nextCandidate);
    }
}