using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Raw
{
    public class QuickSelectCourse
    {
        [JsonPropertyName("value")]
        public string CourseCode { get; set; }
        [JsonPropertyName("label")]
        public string CourseName_VN { get; set; }
    }
}
