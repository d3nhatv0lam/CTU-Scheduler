using System;

namespace CTUScheduler.Core.Models.TeachingPlan;

public static class TimelineNodeExtensions
{
    public static TeachingPlanStep GetStepType(this TimelineNode node)
    {
        var title = node.Title;
        if (string.IsNullOrWhiteSpace(title)) return TeachingPlanStep.Unknown;

        if (title.Contains("Đợt 1", StringComparison.OrdinalIgnoreCase))
            return TeachingPlanStep.CourseRegistrationPhase1;
            
        if (title.Contains("Đợt 2", StringComparison.OrdinalIgnoreCase))
            return TeachingPlanStep.CourseRegistrationPhase2;
            
        if (title.Contains("Bổ sung", StringComparison.OrdinalIgnoreCase))
            return TeachingPlanStep.AdjustStudyPlanSupplementary;
            
        if (title.Contains("Điều chỉnh kế hoạch", StringComparison.OrdinalIgnoreCase) || 
            title.Contains("nhập kế hoạch", StringComparison.OrdinalIgnoreCase))
            return TeachingPlanStep.AdjustStudyPlan;
            
        if (title.Contains("Công bố thời khóa biểu", StringComparison.OrdinalIgnoreCase) || 
            title.Contains("Công bố TKB", StringComparison.OrdinalIgnoreCase))
            return TeachingPlanStep.PublishSchedule;
            
        if (title.Contains("Duyệt mở", StringComparison.OrdinalIgnoreCase))
            return TeachingPlanStep.ApproveExtraClasses;
            
        if (title.Contains("Đóng website", StringComparison.OrdinalIgnoreCase) || 
            title.Contains("Đóng KHHT", StringComparison.OrdinalIgnoreCase))
            return TeachingPlanStep.CloseRegistration;
            
        if (title.Contains("Bắt đầu giảng dạy", StringComparison.OrdinalIgnoreCase) || 
            title.Contains("Bắt đầu học kỳ", StringComparison.OrdinalIgnoreCase))
            return TeachingPlanStep.StartSemester;

        return TeachingPlanStep.Unknown;
    }
}
