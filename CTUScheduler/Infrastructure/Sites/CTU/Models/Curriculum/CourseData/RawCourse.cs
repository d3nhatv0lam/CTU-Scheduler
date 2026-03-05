using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum.CourseData
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
