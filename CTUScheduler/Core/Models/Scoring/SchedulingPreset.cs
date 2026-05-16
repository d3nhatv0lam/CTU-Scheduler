using System.Collections.Generic;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Algorithms.Scoring;
using CTUScheduler.Core.Models.Timetable;

namespace CTUScheduler.Core.Models.Scoring;

/// <summary>
/// Đại diện cho một bộ cấu hình ưu tiên để xếp hạng thời khóa biểu
/// </summary>
public class SchedulingPreset
{
    public string Name { get; init; } = string.Empty;
    public string Icon { get; init; } = "";
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Danh sách các bộ chấm điểm đi kèm với trọng số tương ứng
    /// </summary>
    public IReadOnlyList<IScheduleScorer> Scorers { get; init; } = new List<IScheduleScorer>();

    // Preset

    public static readonly SchedulingPreset DeadlineWarrior = new()
    {
        Name = "Chiến thần chạy deadline",
        Icon = "🚀",
        Description = "Thích đi làm thêm",
        Scorers = new List<IScheduleScorer>
        {
            new CompactDaysScorer(3.0),
            new MinimizeGapsScorer(1.0)
        }
    };

    public static readonly SchedulingPreset ChillBalanced = new()
    {
        Name = "Chill & Cân bằng",
        Icon = "🧘",
        Description = "Tránh stress, tà tà mà học",
        Scorers = new List<IScheduleScorer>
        {
            new BalancedWorkloadScorer(3.0),
            new TimeOfDayScorer(TimeOfDay.Morning, 1.0)
        }
    };

    public static readonly SchedulingPreset NightOwl = new()
    {
        Name = "Cú đêm lười biếng",
        Icon = "🦉",
        Description = "Học xong là về liền",
        Scorers = new List<IScheduleScorer>
        {
            new TimeOfDayScorer(TimeOfDay.Afternoon, 3.0),
            new MinimizeGapsScorer(2.0)
        }
    };

    public static readonly IReadOnlyList<SchedulingPreset> DefaultPresets = new List<SchedulingPreset>
    {
        DeadlineWarrior, 
        ChillBalanced, 
        NightOwl
    };
}
