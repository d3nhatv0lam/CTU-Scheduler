namespace CTUScheduler.Core.Models.TeachingPlan;

public enum TeachingPlanStep
{
    Unknown,
    PublishSchedule,               // Công bố thời khóa biểu
    CourseRegistrationPhase1,      // Sinh viên đăng ký học phần (Đợt 1)
    AdjustStudyPlan,               // Điều chỉnh kế hoạch học tập
    ApproveExtraClasses,           // Duyệt mở thêm lớp học phần
    CloseRegistration,             // Đóng đăng ký KHHT & công bố xóa lớp
    StartSemester,                 // Bắt đầu học kỳ
    CourseRegistrationPhase2,      // Sinh viên thay đổi, đăng ký HP (Đợt 2)
    AdjustStudyPlanSupplementary   // Điều chỉnh kế hoạch học tập (Bổ sung)
}
