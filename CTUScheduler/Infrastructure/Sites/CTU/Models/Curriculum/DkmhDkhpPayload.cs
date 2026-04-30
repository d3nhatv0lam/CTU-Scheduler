using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;

public record RawDkhpPayload(
    // Phần sinh viên chọn
    [property: JsonPropertyName("key")] int Key,
    [property: JsonPropertyName("dkmh_tu_dien_hoc_phan_ma")]
    string HocPhanMa,
    [property: JsonPropertyName("dkmh_tu_dien_hoc_phan_ten_vn")]
    string HocPhanTen,
    [property: JsonPropertyName("dkmh_nhom_hoc_phan_ma")]
    string? NhomHocPhanMa,
    [property: JsonPropertyName("dkmh_tu_dien_hoc_phan_so_tin_chi")]
    int SoTinChi,
    [property: JsonPropertyName("dkmh_tu_dien_lop_hoc_phan_tkb")]
    string? ThoiKhoaBieu,
    [property: JsonPropertyName("dkmh_tu_dien_giang_vien_ten_vn")]
    string? GiangVienTen,
    [property: JsonPropertyName("dkmh_tu_dien_giang_vien_email")]
    string? GiangVienEmail,
    [property: JsonPropertyName("dkmh_tu_dien_hoc_phan_ma_bac_dao_tao")]
    string MaBacDaoTao,
    [property: JsonPropertyName("trang_thai_dang_ky")]
    int? TrangThaiDangKy,
    [property: JsonPropertyName("dang_ky_du_phong")]
    int? DangKyDuPhong,
    [property: JsonPropertyName("cho_phep_trung_lich")]
    int? ChoPhepTrungLich,
    [property: JsonPropertyName("trang_thai_dang_ky_du_phong")]
    int? TrangThaiDangKyDuPhong,
    [property: JsonPropertyName("cho_phep_dang_ky_du_phong")]
    int? ChoPhepDangKyDuPhong,
    [property: JsonPropertyName("thuoc_khht")]
    int? ThuocKhht,
    [property: JsonPropertyName("dkmh_rut_hoc_phan")]
    string? RutHocPhan,
    [property: JsonPropertyName("khong_dang_ky_hoc_phan")]
    int? KhongDangKyHocPhan,

    // thông tin tổng hợp tất cả
    // Dictionary vì key của nó là string động (vd: "01", "02", "100")
    [property: JsonPropertyName("thong_tin_giang_vien")]
    IReadOnlyDictionary<string, IReadOnlyList<RawDkhpGiangVien>> ThongTinGiangVienDict,
    [property: JsonPropertyName("nhom_hp")]
    IReadOnlyList<RawDkhpNhomHocPhan> DanhSachNhomHp,
    [property: JsonPropertyName("data_nhom_hp")]
    IReadOnlyList<RawDkhpDataNhomHocPhan> ChiTietNhomHp
);

public record RawDkhpGiangVien(
    [property: JsonPropertyName("dkmh_tu_dien_giang_vien_ten_vn")]
    string GiangVienTen,
    [property: JsonPropertyName("dkmh_tu_dien_giang_vien_email")]
    string GiangVienEmail
);

public record RawDkhpNhomHocPhan(
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("tkb")] string? ThoiKhoaBieu
);

public record RawDkhpDataNhomHocPhan(
    [property: JsonPropertyName("key")] string? Key,
    [property: JsonPropertyName("dkmh_tu_dien_lop_hoc_phan_si_so")]
    int? SiSo,
    [property: JsonPropertyName("dkmh_tu_dien_lop_hoc_phan_si_so_con_lai")]
    int? SiSoConLai,
    [property: JsonPropertyName("dkmh_tu_dien_hoc_phan_ten_vn")]
    string? HocPhanTen,
    [property: JsonPropertyName("dkmh_nhom_hoc_phan_ma")]
    string? NhomHocPhanMa,
    [property: JsonPropertyName("dkmh_tu_dien_hoc_phan_so_tin_chi")]
    int? SoTinChi,
    [property: JsonPropertyName("tuan_hoc")]
    IReadOnlyList<int> TuanHoc,
    [property: JsonPropertyName("dkmh_tu_dien_hoc_phan_ma")]
    string? HocPhanMa,
    [property: JsonPropertyName("dkmh_tu_dien_lop_hoc_phan_tkb")]
    string? ThoiKhoaBieu,
    [property: JsonPropertyName("dkmh_tu_dien_lop_hoc_phan_lop_ma")]
    string? LopMa, // Mã học phần + Nhóm hoặc tên lớp ngành nếu là môn chuyên ngành
    [property: JsonPropertyName("trang_thai_thao_tac")]
    RawDkhpTrangThaiThaoTac? TrangThaiThaoTac,
    [property: JsonPropertyName("data")] IReadOnlyList<RawDkhpLichHocChiTiet> LichHocChiTiet
);

public record RawDkhpTrangThaiThaoTac(
    [property: JsonPropertyName("disabled_chon_nhom")]
    int? DisabledChonNhom,
    [property: JsonPropertyName("cho_phep_dang_ky")]
    int? ChoPhepDangKy,
    [property: JsonPropertyName("trang_thai")]
    string? TrangThai
);

public record RawDkhpLichHocChiTiet(
    [property: JsonPropertyName("key")] string? Key,
    [property: JsonPropertyName("dkmh_tu_dien_hoc_phan_ma")]
    string? HocPhanMa,
    [property: JsonPropertyName("dkmh_thu_trong_tuan_ma")]
    int? ThuTrongTuan,
    [property: JsonPropertyName("tuan_hoc")]
    IReadOnlyList<int> TuanHoc,
    [property: JsonPropertyName("dkmh_tu_dien_tiet_hoc_stt")]
    int? TietHocBatDau,
    [property: JsonPropertyName("tiet_hoc")]
    string? ChuoiTietHoc, // vd: "-----6789----"
    [property: JsonPropertyName("gv")] IReadOnlyList<RawDkhpGiangVien> GiangVien,
    [property: JsonPropertyName("dkmh_tu_dien_phong_hoc_ten")]
    string? PhongHoc
);