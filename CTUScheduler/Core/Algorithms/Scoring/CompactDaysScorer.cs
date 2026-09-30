using System;
using System.Collections.Generic;
using System.Linq;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Core.Algorithms.Scoring;

/// <summary>
/// Chấm điểm dựa trên mật độ ngày học. Càng ít ngày học thì điểm càng cao.
/// Thang điểm: 0.0 -> 1.0
/// Công thức: (MaxDay - NDay / MaxDay - MinDay)
/// </summary>
public class CompactDaysScorer : IScheduleScorer
{
    public double Weight { get; }

    public CompactDaysScorer(double weight = ScoringConstants.DefaultWeightCompactDays)
    {
        Weight = weight;
    }

    public double CalculateScore(IReadOnlyList<SectionChoice> fullTimetable)
    {
        if (fullTimetable == null || !fullTimetable.Any()) return 0;

        // Lấy danh sách các ngày có lịch học
        var busyDays = fullTimetable
            .SelectMany(c => c.Section.ClassDays)
            .Select(d => d.AttendingDay)
            .Distinct()
            .Count();
        
        double score = (7.0 - busyDays) / 6.0;
        
        return Math.Clamp(score, ScoringConstants.MinScore, ScoringConstants.MaxScore);
    }
}
