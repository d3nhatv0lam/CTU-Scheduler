using System.Collections.Generic;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Core.Interfaces;

public interface IPostFilterRule
{
    bool IsSatisfied(IReadOnlyList<SectionChoice> fullSchedule);
}