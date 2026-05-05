using System.Collections.Generic;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Core.Interfaces;

public interface IScheduleScorer
{
    double Weight { get; }
    double CalculateScore(IReadOnlyList<SectionChoice> fullTimetable);
}