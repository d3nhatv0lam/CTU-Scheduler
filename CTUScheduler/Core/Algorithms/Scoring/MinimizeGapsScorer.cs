using System;
using System.Collections.Generic;
using System.Linq;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Core.Algorithms.Scoring;

/// <summary>
/// Chấm điểm dựa trên thời gian trống giữa các tiết học trong cùng một ngày
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

        // Nhóm các tiết học theo ngày
        var classesByDay = fullTimetable
            .SelectMany(c => c.Section.ClassDays)
            .GroupBy(d => d.AttendingDay);

        int totalGaps = 0;
        int totalDays = 0;

        foreach (var dayGroup in classesByDay)
        {
            totalDays++;
            // Sắp xếp các tiết học trong ngày theo tiết bắt đầu
            var orderedClasses = dayGroup.OrderBy(d => d.StartPeriod).ToList();
            
            for (int i = 0; i < orderedClasses.Count - 1; i++)
            {
                var current = orderedClasses[i];
                var next = orderedClasses[i + 1];
                
                // Tiết kết thúc của môn hiện tại
                int currentEnd = current.StartPeriod + current.PeriodCount - 1;
                
                // Khoảng cách đến môn tiếp theo
                int gap = next.StartPeriod - currentEnd - 1;
                if (gap > 0)
                {
                    totalGaps += gap;
                }
            }
        }

        if (totalDays == 0) return 0;

        // Điểm số giảm dần khi số tiết trống tăng lên
        // Penalty = 1.0 khi số tiết trống trung bình mỗi ngày đạt ngưỡng MaxGapsThreshold
        double penalty = (double)totalGaps / (totalDays * ScoringConstants.MaxGapsThreshold); 
        double score = 1.0 - penalty;

        return Math.Clamp(score, ScoringConstants.MinScore, ScoringConstants.MaxScore);
    }
}
