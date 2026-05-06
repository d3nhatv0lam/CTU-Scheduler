using System;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Core.Interfaces;

public interface IPostFilterRule
{
    bool IsSatisfied(ReadOnlySpan<SectionChoice> fullSchedule);
}