using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Models.Timetable;

namespace CTUScheduler.Core.Algorithms.Scoring;

/// <summary>
/// Chấm điểm dựa trên mức độ phù hợp với buổi học yêu thích
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
            
            // Sáng 1-5, Chiều 6-9, Tối 10-13
            var session = period.TimeOfDay;

            bool isMatch;
            if (Preference == TimeOfDay.Morning)
            {
                isMatch = session == TimeOfDay.Morning;
            }
            else
            {
                // Chiều và Tối được tính điểm giống
                isMatch = session == TimeOfDay.Afternoon || session == TimeOfDay.Evening;
            }
            
            if (isMatch)
            {
                matchingPeriods += period.PeriodCount;
            }
        }

        if (totalPeriods == 0) return 0;

        return (double)matchingPeriods / totalPeriods;
    }
}
