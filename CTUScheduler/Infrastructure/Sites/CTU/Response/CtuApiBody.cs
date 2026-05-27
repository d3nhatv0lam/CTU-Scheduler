using System.Text.Json.Serialization;
using CTUScheduler.Infrastructure.DriverCore.Interfaces;

namespace CTUScheduler.Infrastructure.Sites.CTU.Response;

public record CtuApiBody<T> : IApiBody<T>
{
    [JsonPropertyName("code")]
    public int Code { get; init; }
    
    [JsonPropertyName("msg")]
    public string? Message { get; init; } 
    
    [JsonPropertyName("data")]
    public T? Data { get; init; } 
    
    public bool IsSuccess => Code == 200;
    public T? Content => Data;
}