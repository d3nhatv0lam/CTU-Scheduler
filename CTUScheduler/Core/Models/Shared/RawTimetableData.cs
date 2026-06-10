using System.Collections.Generic;
using CTUScheduler.Core.Interfaces;

namespace CTUScheduler.Core.Models.Shared;

public record RawTimetableData(IReadOnlyList<SectionChoice> Choices) : IScorable
{
    public double Score { get; set; }
}