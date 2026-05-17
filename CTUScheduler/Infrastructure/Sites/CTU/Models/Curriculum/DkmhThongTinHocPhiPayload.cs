using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;

// Đã comment/bỏ bớt các field không dùng thực tế trong app
// Không cài đặt map JsonPropertyName [bankData] và [phuongThucThanhToan] vì không dùng
public record RawThongTinHocPhiPayload(
    [property: JsonPropertyName("tt_tinh_hocphi")]
    string ThongTinTinhHocPhi,

    // [property: JsonPropertyName("danhsachmh")] 
    // IReadOnlyList<RawHocPhiMonHoc> DanhSachMonHoc,
    [property: JsonPropertyName("chitiethocphi")]
    IReadOnlyList<IReadOnlyList<RawHocPhiTextValue>> ChiTietHocPhi,
    [property: JsonPropertyName("ghi_chu")]
    RawHocPhiHaiCot GhiChu,

    // [property: JsonPropertyName("thongtin1")] 
    // IReadOnlyList<RawHocPhiTextValue> ThongTin1,
    //
    // [property: JsonPropertyName("thongtin2")] 
    // RawHocPhiHaiCot ThongTin2,
    [property: JsonPropertyName("thongtin3")]
    IReadOnlyList<RawHocPhiTextValue> ThongTin3
);

// Dùng chung cho "ghi_chu" và "thongtin2" (cấu trúc chia 2 cột)
public record RawHocPhiHaiCot(
    [property: JsonPropertyName("colLeft")]
    IReadOnlyList<RawHocPhiTextValue> CotTrai,
    [property: JsonPropertyName("colRight")]
    IReadOnlyList<RawHocPhiTextValue> CotPhai
);

// Dùng chung cho tất cả các object chứa text (có "value" và thi thoảng có "align")
public record RawHocPhiTextValue(
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("align")] string? Align = null
);

public record RawHocPhiMonHoc(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("so_thu_tu")]
    string SoThuTu,
    [property: JsonPropertyName("loai_hp")]
    string? LoaiHocPhan,
    [property: JsonPropertyName("dkmh_tu_dien_hoc_phan_ma")]
    string MaHocPhan,
    [property: JsonPropertyName("dkmh_tu_dien_hoc_phan_ten_vn")]
    string TenHocPhan,
    [property: JsonPropertyName("tin_chi_hoc_phi")]
    string TinChiHocPhi,
    [property: JsonPropertyName("thanh_tien")]
    string ThanhTien,
    [property: JsonPropertyName("donmuc_tc")]
    string? DonMucTinChi = null,
    [property: JsonPropertyName("donmuc_mg")]
    string? DonMucMienGiam = null,
    [property: JsonPropertyName("tyletinhhocphi")]
    int? TyLeTinhHocPhi = null,
    [property: JsonPropertyName("istongcong")]
    bool? IsTongCong = null
);