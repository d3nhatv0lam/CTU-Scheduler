using System.Text.Json.Serialization;

namespace CTUScheduler.Core.Models.Academic;

public record SchoolAnnouncement(
    [property: JsonPropertyName("noidung")]
    string Title,
    [property: JsonPropertyName("link")] string Link);