using System.Collections.Generic;
using CTUScheduler.Core.Interfaces;

namespace CTUScheduler.Core.Models.Scoring;

/// <summary>
/// Chứa cấu hình các bộ chấm điểm
/// </summary>
public class ScoringProfile
{
    public IReadOnlyList<IScheduleScorer> Scorers { get; init; } = new List<IScheduleScorer>();
}
