using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;

public record QuickSelectDmhpCourse(
    [property: JsonPropertyName("value")] string CourseCode,
    [property: JsonPropertyName("label")] string CourseNameVn)
{
    public string Information => $"{CourseCode} - {CourseNameVn}";
}

public record RawDmhpPayload(
    [property: JsonPropertyName("tuan_max")]
    int TuanMax,
    [property: JsonPropertyName("hoc_phan_info")]
    RawDmhpCourseInfo HocPhanInfo,
    [property: JsonPropertyName("data")] IReadOnlyList<RawDmhpCourseData> Data);

public record RawDmhpCourseInfo(
    [property: JsonPropertyName("dkmh_tu_dien_hoc_phan_ma")]
    string HocPhanMa,
    [property: JsonPropertyName("dkmh_tu_dien_hoc_phan_ten")]
    string HocPhanTen,
    [property: JsonPropertyName("dkmh_tu_dien_hoc_phan_ten_vn")]
    string HocPhanTenVn,
    [property: JsonPropertyName("dkmh_tu_dien_hoc_phan_so_tin_chi")]
    int SoTinChi,
    [property: JsonPropertyName("dkmh_tu_dien_hoc_phan_so_tiet_ly_thuyet")]
    int SoTietLyThuyet,
    [property: JsonPropertyName("dkmh_tu_dien_hoc_phan_so_tiet_thuc_hanh")]
    int SoTietThucHanh,
    [property: JsonPropertyName("dkmh_tu_dien_don_vi_ma")]
    string DonViMa);

public record RawDmhpCourseData(
    [property: JsonPropertyName("key")] int Key,
    [property: JsonPropertyName("dkmh_tu_dien_hoc_phan_ma")]
    string HocPhanMa,
    [property: JsonPropertyName("dkmh_nhom_hoc_phan_ma")]
    string NhomHocPhanMa,
    [property: JsonPropertyName("dkmh_tu_dien_hoc_phan_ten_vn")]
    string HocPhanTenVn,
    [property: JsonPropertyName("dkmh_tu_dien_hoc_phan_so_tin_chi")]
    int SoTinChi,
    [property: JsonPropertyName("dkmh_tu_dien_phong_hoc_ten")]
    string? PhongHocTen,
    [property: JsonPropertyName("dkmh_thu_trong_tuan_ma")]
    int? ThuTrongTuanMa,
    [property: JsonPropertyName("dkmh_tu_dien_giang_vien_ten_vn")]
    string GiangVienTenVn,
    [property: JsonPropertyName("dkmh_tu_dien_giang_vien_email")]
    string GiangVienEmail,
    [property: JsonPropertyName("dkmh_tu_dien_lop_hoc_phan_si_so")]
    int SiSo,
    [property: JsonPropertyName("si_so_con_lai")]
    int SiSoConLai,
    [property: JsonPropertyName("tiet_hoc")]
    string? TietHoc,
    [property: JsonExtensionData] IDictionary<string, JsonElement>? ExtraData
);