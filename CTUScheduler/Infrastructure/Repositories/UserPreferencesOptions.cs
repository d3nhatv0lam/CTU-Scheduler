using System.Text.Json;

namespace CTUScheduler.Infrastructure.Repositories;

public record UserPreferencesOptions
{
    public required string FilePath { get; set; }
}