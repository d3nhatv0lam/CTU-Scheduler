using System.Collections.Generic;
using System.Linq;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Presentation.Shared.Models;

namespace CTUScheduler.Presentation.Features.Scheduling.Comparers;

/// <summary>
/// Bộ so sánh để sắp xếp các thời khóa biểu dựa trên tổng điểm từ các Scorer.
/// </summary>
public class TimetableScoreComparer : IComparer<SelectableTimetableLayout>
{
    private readonly IReadOnlyList<IScheduleScorer> _scorers;

    public TimetableScoreComparer(IReadOnlyList<IScheduleScorer> scorers)
    {
        _scorers = scorers ?? new List<IScheduleScorer>();
    }

    public int Compare(SelectableTimetableLayout? x, SelectableTimetableLayout? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x == null) return 1; 
        if (y == null) return -1;

        double scoreX = CalculateTotalScore(x);
        double scoreY = CalculateTotalScore(y);

        
        int result = scoreY.CompareTo(scoreX);
        
        // nếu điểm bằng nhau, so sánh theo tiêu chí phụ như tên hoặc thời gian cập nhật
        if (result == 0)
        {
            return x.Item.Name.CompareTo(y.Item.Name);
        }
        
        return result;
    }

    private double CalculateTotalScore(SelectableTimetableLayout layout)
    {
        if (_scorers.Count == 0) return 0;

        double totalWeightedScore = 0;
        double totalWeight = 0;

        // Lấy danh sách các môn học từ VM
        var choices = layout.Item.Choices;

        foreach (var scorer in _scorers)
        {
            double score = scorer.CalculateScore(choices);
            totalWeightedScore += score * scorer.Weight;
            totalWeight += scorer.Weight;
        }

        return totalWeight > 0 ? totalWeightedScore / totalWeight : 0;
    }
}
