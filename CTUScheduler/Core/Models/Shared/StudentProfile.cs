namespace CTUScheduler.Core.Models.Shared;

/// <summary>
/// Đại diện cho thông tin định danh của sinh viên
/// </summary>
/// <param name="Mssv">Mã số sinh viên (VD: B2303807)</param>
/// <param name="Name">Họ tên sinh viên (VD: Dương Minh Đức)</param>
public record StudentProfile(string Mssv, string Name)
{
    public string DisplayName => $"{Name} ({Mssv})";
}

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
// public record StudentProfile(
//     // 2 trường cũ
//     string Mssv,                  // sys_manguoidung
//     string Name,                  // sys_hoten
//     
//     // Các trường hiển thị mở rộng
//     string ClassCode,             // sys_malop (DI2396A1)
//     string MajorName,             // sys_tennganh (Kỹ thuật phần mềm)
//     string DepartmentName,        // sys_tendonvi (Trường CNTT&TT)
//     int Cohort,                   // sys_khoahoc (49)
//     int AccumulatedCredits,       // sys_sotinchidat (130)
//     
//     // Các trường logic hoạt động
//     int CurrentAcademicYear,      // sys_namhocht (2025)
//     int CurrentSemester,          // sys_hockyht (3)
//     int MaxCreditsMainSemester,   // sys_tcmaxhockychinh (20)
//     int MaxCreditsSummerSemester  // sys_tcmaxhockyhe (20)
// );

// Map to StudentProfile
// using var doc = JsonDocument.Parse(decompressedJson);
// var root = doc.RootElement;
// var profile = new StudentProfile(
//     Mssv: root.GetProperty("sys_manguoidung").GetString() ?? "",
//     Name: root.GetProperty("sys_hoten").GetString() ?? "",
//     ClassCode: root.GetProperty("sys_malop").GetString() ?? "",
//     MajorName: root.GetProperty("sys_tennganh").GetString() ?? "",
//     DepartmentName: root.GetProperty("sys_tendonvi").GetString() ?? "",
//     Cohort: root.GetProperty("sys_khoahoc").GetInt32(),
//     AccumulatedCredits: root.GetProperty("sys_sotinchidat").GetInt32(),
//     CurrentAcademicYear: root.GetProperty("sys_namhocht").GetInt32(),
//     CurrentSemester: root.GetProperty("sys_hockyht").GetInt32(),
//     MaxCreditsMainSemester: root.GetProperty("sys_tcmaxhockychinh").GetInt32(),
//     MaxCreditsSummerSemester: root.GetProperty("sys_tcmaxhockyhe").GetInt32()
// );
