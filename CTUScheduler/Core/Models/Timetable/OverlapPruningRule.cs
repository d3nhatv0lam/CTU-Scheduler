using System;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Validators;

namespace CTUScheduler.Core.Models.Timetable;

public class OverlapPruningRule: IPruningRule
{
    public bool CanContinue(ReadOnlySpan<SectionChoice> currentPath, SectionChoice nextCandidate)
    {
        for (int i = 0; i < currentPath.Length; i++)
        {
            if (ScheduleValidator.IsOverlapTimeTable(currentPath[i].Section, nextCandidate.Section))
                return false;
        }
        return true;
    }
}