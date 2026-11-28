namespace CTUScheduler.Core.Models.Academic.Grades;

public record SemesterGradeSummary(
    int SemesterAccumulatedCredits,
    int TotalAccumulatedCredits,
    double SemesterGpa,
    double CumulativeGpa,
    // điểm hạnh kiểm trong hk có thể bị trống?
    // dev note: tôi nghĩ là do học kỳ của tôi bị đặt biệt?
    int? ConductScore);
