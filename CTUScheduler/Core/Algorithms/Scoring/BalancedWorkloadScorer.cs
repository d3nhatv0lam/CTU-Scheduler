using System;
using System.Collections.Generic;
using System.Linq;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Core.Algorithms.Scoring;

/// <summary>
/// Chấm điểm dựa trên sự cân bằng tải trọng học tập giữa các ngày trong tuần
/// Càng phân bổ đều (độ lệch chuẩn thấp) thì điểm càng cao
/// </summary>
public class BalancedWorkloadScorer : IScheduleScorer
{
    public double Weight { get; }

    public BalancedWorkloadScorer(double weight = ScoringConstants.DefaultWeightTimeOfDay)
    {
        Weight = weight;
    }

    public double CalculateScore(IReadOnlyList<SectionChoice> fullTimetable)
    {
        if (fullTimetable == null || !fullTimetable.Any()) return 0;

        // Tính tổng số tiết của từng ngày trong tuần (Thứ 2 - Chủ Nhật: 2-8)
        var dailyPeriods = new Dictionary<int, int>();
        for (int i = 2; i <= 8; i++) dailyPeriods[i] = 0;

        foreach (var choice in fullTimetable)
        {
            foreach (var day in choice.Section.ClassDays)
            {
                if (dailyPeriods.ContainsKey(day.AttendingDay))
                {
                    dailyPeriods[day.AttendingDay] += day.PeriodCount;
                }
            }
        }

        // Chỉ tính những ngày có lịch học 
        var busyDayLoads = dailyPeriods.Values.Where(v => v > 0).ToList();
        
        // Nếu chỉ học 1 ngày hoặc không học ngày nào -> cân bằng tuyệt đối
        if (busyDayLoads.Count <= 1) return 1.0;

        // Tính độ lệch chuẩn 
        double average = busyDayLoads.Average();
        double sumOfSquares = busyDayLoads.Sum(v => Math.Pow(v - average, 2));
        double standardDeviation = Math.Sqrt(sumOfSquares / busyDayLoads.Count);

        // Chuẩn hóa điểm số:
        // Giả sử độ lệch chuẩn = 0 (học đều) là 1 điểm
        // Giả sử độ lệch chuẩn >= 4.0 (lệch rất nặng, ví dụ ngày 10 tiết, ngày 2 tiết) là 0 điểm
        const double maxDeviationThreshold = 4.0;
        double score = 1.0 - (standardDeviation / maxDeviationThreshold);

        return Math.Clamp(score, ScoringConstants.MinScore, ScoringConstants.MaxScore);
    }
}
