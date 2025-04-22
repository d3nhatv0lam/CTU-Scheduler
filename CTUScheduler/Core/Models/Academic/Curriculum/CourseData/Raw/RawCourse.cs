using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Raw
{
    public class RawCourse
    {
        public int tuan_max { get; set; }
        [JsonPropertyName("hoc_phan_info")]
        public RawCourseInformation hoc_phan_info { get; set; }
        [JsonPropertyName("data")]
        public List<RawCourseData> data { get; set; }
    }
}
