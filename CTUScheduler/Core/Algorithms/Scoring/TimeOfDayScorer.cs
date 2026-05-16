using System.Collections.Generic;
using System.Linq;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Models.Timetable;

namespace CTUScheduler.Core.Algorithms.Scoring;

/// <summary>
/// Chấm điểm dựa trên mức độ phù hợp với buổi học yêu thích.
/// Thang điểm: 0.0 -> 1.0
/// Công thức: Số tiết đúng buổi / Tổng số tiết
/// </summary>
public class TimeOfDayScorer : IScheduleScorer
{
    public double Weight { get; }
    public TimeOfDay Preference { get; }

    public TimeOfDayScorer(TimeOfDay preference, double weight = ScoringConstants.DefaultWeightTimeOfDay)
    {
        Preference = preference;
        Weight = weight;
    }

    public double CalculateScore(IReadOnlyList<SectionChoice> fullTimetable)
    {
        if (fullTimetable == null || !fullTimetable.Any()) return 0;

        var allPeriods = fullTimetable.SelectMany(c => c.Section.ClassDays);
        int matchingPeriods = 0;
        int totalPeriods = 0;

        foreach (var period in allPeriods)
        {
            totalPeriods += period.PeriodCount;
            var session = period.TimeOfDay;

            bool isMatch;
            if (Preference == TimeOfDay.Morning)
            {
                isMatch = session == TimeOfDay.Morning;
            }
            else
            {
                // Chiều và Tối được tính điểm giống nhau
                isMatch = session == TimeOfDay.Afternoon || session == TimeOfDay.Evening;
            }
            
            if (isMatch)
            {
                matchingPeriods += period.PeriodCount;
            }
        }

        if (totalPeriods == 0) return 0;

        // Công thức theo yêu cầu: Số tiết đúng buổi / Tổng số tiết
        return (double)matchingPeriods / totalPeriods;
    }
}
