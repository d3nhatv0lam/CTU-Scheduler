using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Raw
{
    public class RawCourseData
    {
        public int key { get; set; }
        public string dkmh_tu_dien_hoc_phan_ma { get; set; }
        public string dkmh_nhom_hoc_phan_ma { get; set; }
        public string dkmh_tu_dien_hoc_phan_ten_vn { get; set; }
        public int dkmh_tu_dien_hoc_phan_so_tin_chi { get; set; }
        public string? dkmh_tu_dien_phong_hoc_ten { get; set; }
        public int? dkmh_thu_trong_tuan_ma { get; set; }
        public string dkmh_tu_dien_giang_vien_ten_vn { get; set; }
        public string dkmh_tu_dien_giang_vien_email { get; set; }
        public int dkmh_tu_dien_lop_hoc_phan_si_so { get; set; }
        public int si_so_con_lai { get; set; }
        public string? tiet_hoc { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object>? ExtraData { get; set; }
    }
}
