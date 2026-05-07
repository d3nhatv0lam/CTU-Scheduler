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
