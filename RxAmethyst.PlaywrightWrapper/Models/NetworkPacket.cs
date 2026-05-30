namespace RxAmethyst.PlaywrightWrapper.Models;

public record  NetworkPacket()
{
    public required string Url { get; init; } 
    public required string Method { get; init; }
    public required string RawBody { get; init; }
}