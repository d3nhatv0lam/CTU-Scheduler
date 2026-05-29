using System.Collections.Generic;
using CTUScheduler.Core.Algorithms.Scoring;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Timetable;

namespace CTUScheduler.Core.Models.Scoring;

/// <summary>
/// Chứa cấu hình các bộ chấm điểm
/// </summary>
public class ScoringProfile
{
    public string ProfileId { get; init; } = string.Empty;
    public IReadOnlyList<IScheduleScorer> Scorers { get; init; } = new List<IScheduleScorer>();
    
    /// <summary>
    /// Dồn lịch vào ít ngày nhất có thể để dành thời gian làm việc khác
    /// </summary>
    public static readonly ScoringProfile DeadlineWarrior = new()
    {
        ProfileId = "DeadlineWarrior",
        Scorers = new List<IScheduleScorer>
        {
            new CompactDaysScorer(1.0),
            new MinimizeGapsScorer(0.5)
        }
    };

    /// <summary>
    /// Phân bổ lịch học đều các ngày, tránh dồn cục gây stress
    /// </summary>
    public static readonly ScoringProfile ChillBalanced = new()
    {
        ProfileId = "ChillBalanced",
        Scorers = new List<IScheduleScorer>
        {
            new BalancedWorkloadScorer(1.0),
            new TimeOfDayScorer(TimeOfDay.Morning, 0.5)
        }
    };

    /// <summary>
    /// Ưu tiên các buổi chiều và tối, tránh dậy sớm
    /// </summary>
    public static readonly ScoringProfile NightOwl = new()
    {
        ProfileId = "NightOwl",
        Scorers = new List<IScheduleScorer>
        {
            new TimeOfDayScorer(TimeOfDay.Afternoon, 1.0),
            new MinimizeGapsScorer(0.5)
        }
    };
}
