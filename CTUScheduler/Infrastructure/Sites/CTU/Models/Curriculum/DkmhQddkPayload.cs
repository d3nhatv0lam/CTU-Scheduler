using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;

public record RawQddkPayload(
    [property: JsonPropertyName("namhoc")] int NamHoc,
    [property: JsonPropertyName("hocky")] string HocKy,
    [property: JsonPropertyName("quyDinh")]
    IReadOnlyList<RawQddkQuyDinh> DanhSachQuyDinh,
    [property: JsonPropertyName("thoiGianDangKy")]
    IReadOnlyList<IReadOnlyList<RawQddkThoiGianDangKyItem>> DanhSachThoiGianDangKy);

public record RawQddkQuyDinh(
    [property: JsonPropertyName("leftData")]
    IReadOnlyList<RawQddkQuyDinhData> CotTrai,
    [property: JsonPropertyName("rightData")]
    IReadOnlyList<RawQddkQuyDinhData> CotPhai);

public record RawQddkQuyDinhData(
    [property: JsonPropertyName("value")] string NoiDung,
    [property: JsonPropertyName("important")]
    IReadOnlyList<string> CacDiemLuuY);

public record RawQddkThoiGianDangKyItem(
    [property: JsonPropertyName("title")] string TieuDe,
    [property: JsonPropertyName("rowspan")]
    string RowSpan,
    [property: JsonPropertyName("impo")] bool? IsQuanTrong
);