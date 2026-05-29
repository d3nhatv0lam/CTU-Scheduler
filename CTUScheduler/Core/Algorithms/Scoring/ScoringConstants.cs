namespace CTUScheduler.Core.Algorithms.Scoring;

/// <summary>
/// Chứa các hằng số dùng cho việc tính toán điểm số thời khóa biểu
/// </summary>
public static class ScoringConstants
{
    // Trọng số mặc định cho các tiêu chí
    public const double DefaultWeightCompactDays = 1.0;
    public const double DefaultWeightMinimizeGaps = 1.0;
    public const double DefaultWeightTimeOfDay = 1.0;

    // Các ngưỡng cấu hình
    public const int MaxStudyDaysPerWeek = 6; // Số ngày học tối đa trong tuần
    public const int TotalStudyPeriodsPerDay = 13; // Tổng số tiết học tối đa trong một ngày
    public const double MaxGapsThreshold = 4.0; // tiết trống trung bình mỗi ngày được coi là tệ nhất 
    
    // Điểm số tối đa và tối thiểu
    public const double MaxScore = 1.0;
    public const double MinScore = 0.0;
}
