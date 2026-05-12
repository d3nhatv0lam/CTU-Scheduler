using System.Collections.Generic;
using System.Linq;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Core.Algorithms.Scoring;

/// <summary>
/// Chấm điểm dựa trên mật độ ngày học. Càng ít ngày học thì điểm càng cao
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

        if (busyDays == 0) return 0;

        // Công thức: 1.0 - (Số ngày học / Tổng số ngày học tối đa trong tuần)
        // Lưu ý: Càng ít ngày học thì giá trị càng lớn 
        double score = 1.0 - ((double)busyDays / ScoringConstants.MaxStudyDaysPerWeek);
        
        return Math.Clamp(score, ScoringConstants.MinScore, ScoringConstants.MaxScore);
    }
}
