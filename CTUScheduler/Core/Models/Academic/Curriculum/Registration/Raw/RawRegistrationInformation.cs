using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Models.Academic.Curriculum.Registration.Raw
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
