using System.Text.Json.Serialization;

namespace CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum.CourseData;

public class QuickSelectCourse
{
    [JsonPropertyName("value")] public string CourseCode { get; set; }
    [JsonPropertyName("label")] public string CourseName_VN { get; set; }

    [JsonIgnore] public string Information => $"{CourseCode} - {CourseName_VN}";
}