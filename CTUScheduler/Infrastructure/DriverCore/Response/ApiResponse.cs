using System.Text.Json.Serialization;

namespace CTUScheduler.Infrastructure.DriverCore.Response;

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