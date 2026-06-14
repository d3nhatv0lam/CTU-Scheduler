namespace CTUScheduler.Core.Models.TeachingPlan;

public static class TeachingPlanStepExtensions
{
    public static string ToFriendlyString(this TeachingPlanStep step) => step switch
    {
        TeachingPlanStep.PublishSchedule => "Công bố thời khóa biểu",
        TeachingPlanStep.CourseRegistrationPhase1 => "Đăng ký học phần (Đợt 1)",
        TeachingPlanStep.AdjustStudyPlan => "Điều chỉnh kế hoạch học tập",
        TeachingPlanStep.ApproveExtraClasses => "Duyệt mở thêm lớp học phần",
        TeachingPlanStep.CloseRegistration => "Đóng KHHT & Công bố xóa lớp",
        TeachingPlanStep.StartSemester => "Bắt đầu học kỳ mới",
        TeachingPlanStep.CourseRegistrationPhase2 => "Thay đổi, đăng ký HP (Đợt 2)",
        TeachingPlanStep.AdjustStudyPlanSupplementary => "Điều chỉnh kế hoạch học tập (Bổ sung)",
        _ => "Không xác định"
    };
}
