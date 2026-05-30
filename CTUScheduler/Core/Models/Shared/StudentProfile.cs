namespace CTUScheduler.Core.Models.Shared;

/// <summary>
/// Http jwt parsed data
/// </summary>
/// <param name="Mssv"></param>
/// <param name="Name"></param>
/// <param name="ClassCode"></param>
/// <param name="MajorName"></param>
/// <param name="DepartmentName"></param>
/// <param name="Cohort"></param>
/// <param name="AccumulatedCredits"></param>
/// <param name="CurrentAcademicYear"></param>
/// <param name="CurrentSemester"></param>
/// <param name="MaxCreditsMainSemester"></param>
/// <param name="MaxCreditsSummerSemester"></param>
public record StudentProfile(
    // 2 trường cũ
    string Mssv, // sys_manguoidung
    string Name, // sys_hoten

    // Các trường hiển thị mở rộng
    string ClassCode, // sys_malop (DI2396A1)
    string MajorName, // sys_tennganh (Kỹ thuật phần mềm)
    string DepartmentName, // sys_tendonvi (Trường CNTT&TT)
    int Cohort, // sys_khoahoc (49)
    int AccumulatedCredits, // sys_sotinchidat (130)

    // Các trường logic hoạt động
    int CurrentAcademicYear, // sys_namhocht (2025)
    int CurrentSemester, // sys_hockyht (3)
    int MaxCreditsMainSemester, // sys_tcmaxhockychinh (20)
    int MaxCreditsSummerSemester // sys_tcmaxhockyhe (20)
)
{
    public string DisplayName => $"{Name} ({Mssv})";
}
