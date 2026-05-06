using System;
using System.Collections.Generic;

namespace CTUScheduler.Core.Models.Academic.Curriculum.Registration;

public record UserPeriodItem(
    string Key, 
    DateTime? StartDate, 
    DateTime? EndDate, 
    IReadOnlyList<int> AllowedGroups, 
    string GroupDescription
)
{
    public string AllowedGroupsDisplay => string.Join(", ", AllowedGroups);
}
