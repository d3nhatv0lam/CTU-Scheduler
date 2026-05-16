using System.Collections.Generic;
using CTUScheduler.Core.Algorithms.Scoring;
using CTUScheduler.Core.Models.Scoring;
using CTUScheduler.Core.Models.Timetable;
using CTUScheduler.Presentation.Features.Scheduling.Models;
using CTUScheduler.Presentation.Shared.Models;
using CTUScheduler.Core.Interfaces;

namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels.Components;

public class SchedulingPresetViewModel : SelectableItem<SchedulingPreset>
{
    public SchedulingPreset Metadata => Item;

    public SchedulingPresetViewModel(SchedulingPreset preset) : base(preset)
    {
    }

    /// <summary>
    /// Danh sách các Preset mặc định
    /// </summary>
    public static readonly List<SchedulingPreset> DefaultPresets = new()
    {
        new SchedulingPreset
        {
            Name = "Chiến thần deadline",
            Icon = "🚀",
            Description = "Dồn lịch vào ít ngày nhất có thể để dành thời gian chạy deadline",
            Profile = new ScoringProfile 
            { 
                Scorers = new List<IScheduleScorer> 
                { 
                    new CompactDaysScorer(1.0), 
                    new MinimizeGapsScorer(0.5) 
                } 
            }
        },
        new SchedulingPreset
        {
            Name = "Chill & Cân bằng",
            Icon = "🧘",
            Description = "Phân bổ lịch học đều các ngày, tránh dồn cục gây stress",
            Profile = new ScoringProfile 
            { 
                Scorers = new List<IScheduleScorer> 
                { 
                    new BalancedWorkloadScorer(1.0), 
                    new TimeOfDayScorer(TimeOfDay.Morning, 0.5) 
                } 
            }
        },
        new SchedulingPreset
        {
            Name = "Cú đêm lười biếng",
            Icon = "🦉",
            Description = "Ưu tiên các buổi chiều và tối, tránh dậy sớm",
            Profile = new ScoringProfile 
            { 
                Scorers = new List<IScheduleScorer> 
                { 
                    new TimeOfDayScorer(TimeOfDay.Afternoon, 1.0), 
                    new MinimizeGapsScorer(0.5) 
                } 
            }
        }
    };
}
