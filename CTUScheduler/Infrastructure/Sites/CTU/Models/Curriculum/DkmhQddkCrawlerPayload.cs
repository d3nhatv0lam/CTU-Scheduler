using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;

public record DkmhQddkCrawlerPayload(
    [property: JsonPropertyName("namhoc")] int NamHoc,
    [property: JsonPropertyName("hocky")] string HocKy,
    [property: JsonPropertyName("quyDinh")]
    IReadOnlyList<RawQuyDinh> DanhSachQuyDinh,
    [property: JsonPropertyName("thoiGianDangKy")]
    IReadOnlyList<IReadOnlyList<RawThoiGianDangKyItem>> DanhSachThoiGianDangKy);

public record RawQuyDinh(
    [property: JsonPropertyName("leftData")]
    IReadOnlyList<RawQuyDinhData> CotTrai,
    [property: JsonPropertyName("rightData")]
    IReadOnlyList<RawQuyDinhData> CotPhai);

public record RawQuyDinhData(
    [property: JsonPropertyName("value")] string NoiDung,
    [property: JsonPropertyName("important")]
    IReadOnlyList<string> CacDiemLuuY);

public record RawThoiGianDangKyItem(
    [property: JsonPropertyName("title")] string TieuDe,
    [property: JsonPropertyName("rowspan")]
    string RowSpan,
    [property: JsonPropertyName("impo")] bool? IsQuanTrong
);