using System.Text.Json.Serialization;

namespace CTUScheduler.Core.Models.WebResponse;

public record ApiResponse<T>
{
    [JsonPropertyName("code")]
    public int Code { get; init; }
    [JsonPropertyName("msg")]
    public string? Message { get; init; }
    [JsonPropertyName("data")]
    public T? Data { get; init; }
    public bool IsSuccess => Code == 200;
}