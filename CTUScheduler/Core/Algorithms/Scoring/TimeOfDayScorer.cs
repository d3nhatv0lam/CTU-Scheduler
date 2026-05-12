using System;
using System.Collections.Generic;
using System.Linq;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Core.Algorithms.Scoring;

public enum PreferredTime
{
    Morning,
    Afternoon
}

/// <summary>
/// Chấm điểm dựa trên mức độ phù hợp với buổi học yêu thích
/// </summary>
public class TimeOfDayScorer : IScheduleScorer
{
    public double Weight { get; }
    public PreferredTime Preference { get; }

    public TimeOfDayScorer(PreferredTime preference, double weight = ScoringConstants.DefaultWeightTimeOfDay)
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
            
            // 1 - 5: sáng, 6 - 9: chiều
            bool isMorning = period.StartPeriod <= 5;
            
            if ((Preference == PreferredTime.Morning && isMorning) ||
                (Preference == PreferredTime.Afternoon && !isMorning))
            {
                matchingPeriods += period.PeriodCount;
            }
        }

        if (totalPeriods == 0) return 0;

        return (double)matchingPeriods / totalPeriods;
    }
}
