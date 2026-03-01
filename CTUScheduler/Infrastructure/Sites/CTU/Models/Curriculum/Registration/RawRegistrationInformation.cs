using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum.Registration
{
    public class RawRegistrationInformation
    {
        [JsonRequired]
        public int namhoc { get; set; }
        [JsonRequired]
        public string hocky { get; set; }
        [JsonRequired]
        public List<RawQuyDinh> quyDinh { get; set; }
        [JsonRequired]
        public List<List<RawThoiGianDangKyItem>> thoiGianDangKy { get; set; }
    }
}
