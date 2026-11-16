using System;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Core.Models.Timetable;

public sealed class MaxCreditsPruningRule(int maxCredits) : IPruningRule
{
    public int MaxCredits { get; } = maxCredits >= 0
        ? maxCredits
        : throw new ArgumentOutOfRangeException(nameof(maxCredits));

    public bool CanContinue(ReadOnlySpan<SectionChoice> currentPath, SectionChoice nextCandidate)
    {
        int totalCredits = nextCandidate.Course.Credits;
        for (int i = 0; i < currentPath.Length; i++)
        {
            totalCredits += currentPath[i].Course.Credits;
        }

        return totalCredits <= MaxCredits;
    }
}
