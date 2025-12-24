namespace CTUScheduler.Core.Models.Settings;

public record SystemConfig
{
    public string AppVersion { get; init; } = "0.1";
}