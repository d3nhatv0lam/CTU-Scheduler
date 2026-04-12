namespace CTUScheduler.Infrastructure.DriverCore.Response;

public record  NetworkPacket()
{
    public required string Url { get; init; } 
    public required string Method { get; init; }
    public required string RawBody { get; init; }
}