using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CTUScheduler.Models
{
    public class RawCourseData
    {
        public int key { get; set; }
        public string dkmh_tu_dien_hoc_phan_ma { get; set; }
        public string dkmh_nhom_hoc_phan_ma { get; set; }
        public string dkmh_tu_dien_hoc_phan_ten_vn { get; set; }
        public int dkmh_tu_dien_hoc_phan_so_tin_chi { get; set; }
        public string dkmh_tu_dien_phong_hoc_ten { get; set; }
        public int dkmh_thu_trong_tuan_ma { get; set; }
        public string dkmh_tu_dien_giang_vien_ten_vn { get; set; }
        public string dkmh_tu_dien_giang_vien_email { get; set; }
        public int dkmh_tu_dien_lop_hoc_phan_si_so { get; set; }
        public int si_so_con_lai { get; set; }
        public string tiet_hoc { get; set; }

        [JsonPropertyName("tuanhoc-1")]
        public string tuanhoc_1 { get; set; }
        [JsonPropertyName("tuanhoc-2")]
        public string tuanhoc_2 { get; set; }
        [JsonPropertyName("tuanhoc-3")]
        public string tuanhoc_3 { get; set; }
        [JsonPropertyName("tuanhoc-4")]
        public string tuanhoc_4 { get; set; }
        [JsonPropertyName("tuanhoc-5")]
        public string tuanhoc_5 { get; set; }
        [JsonPropertyName("tuanhoc-6")]
        public string tuanhoc_6 { get; set; }
        [JsonPropertyName("tuanhoc-7")]
        public string tuanhoc_7 { get; set; }
        [JsonPropertyName("tuanhoc-8")]
        public string tuanhoc_8 { get; set; }
        [JsonPropertyName("tuanhoc-9")]
        public string tuanhoc_9 { get; set; }
        [JsonPropertyName("tuanhoc-10")]
        public string tuanhoc_10 { get; set; }
        [JsonPropertyName("tuanhoc-11")]
        public string tuanhoc_11 { get; set; }
        [JsonPropertyName("tuanhoc-12")]
        public string tuanhoc_12 { get; set; }
        [JsonPropertyName("tuanhoc-13")]
        public string tuanhoc_13 { get; set; }
        [JsonPropertyName("tuanhoc-14")]
        public string tuanhoc_14 { get; set; }
        [JsonPropertyName("tuanhoc-15")]
        public string tuanhoc_15 { get; set; }
        [JsonPropertyName("tuanhoc-16")]
        public string tuanhoc_16 { get; set; }
        [JsonPropertyName("tuanhoc-17")]
        public string tuanhoc_17 { get; set; }
    }
}
