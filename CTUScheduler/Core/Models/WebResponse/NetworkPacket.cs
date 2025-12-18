using System.Text.Json;

namespace CTUScheduler.Core.Models.WebResponse;

public record NetworkPacket()
{
    public required string Url { get; init; } 
    public required string Method { get; init; }
    public required string RawBody { get; init; }
}