using System;
using System.Collections.Generic;
using System.Linq;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Core.Algorithms.Scoring;

/// <summary>
/// Chấm điểm dựa trên thời gian trống giữa các tiết học trong cùng một ngày.
/// Thang điểm: 0.0 -> 1.0
/// Công thức: 1.0 - (Tổng tiết trống / Tổng tiết học)
/// </summary>
public class MinimizeGapsScorer : IScheduleScorer
{
    public double Weight { get; }

    public MinimizeGapsScorer(double weight = ScoringConstants.DefaultWeightMinimizeGaps)
    {
        Weight = weight;
    }

    public double CalculateScore(IReadOnlyList<SectionChoice> fullTimetable)
    {
        if (fullTimetable == null || !fullTimetable.Any()) return 0;

        var classesByDay = fullTimetable
            .SelectMany(c => c.Section.ClassDays)
            .GroupBy(d => d.AttendingDay);

        int totalGaps = 0;
        int totalPeriods = 0;

        foreach (var dayGroup in classesByDay)
        {
            var orderedClasses = dayGroup.OrderBy(d => d.StartPeriod).ToList();
            totalPeriods += orderedClasses.Sum(c => c.PeriodCount);
            
            for (int i = 0; i < orderedClasses.Count - 1; i++)
            {
                var current = orderedClasses[i];
                var next = orderedClasses[i + 1];
                
                int currentEnd = current.StartPeriod + current.PeriodCount - 1;
                int gap = next.StartPeriod - currentEnd - 1;
                if (gap > 0)
                {
                    totalGaps += gap;
                }
            }
        }

        if (totalPeriods == 0) return 1.0;

        // Công thức: 1.0 - (Tổng tiết trống / Tổng tiết học)
        double score = 1.0 - ((double)totalGaps / totalPeriods);

        return Math.Clamp(score, ScoringConstants.MinScore, ScoringConstants.MaxScore);
    }
}
