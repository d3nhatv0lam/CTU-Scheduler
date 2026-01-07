namespace CTUScheduler.Core.Models.Settings;

public record UserSettings
{
    public bool IsDarkMode { get; init; } = false;
    public bool AutoSaveEnabled { get; init; } = true;
    // public string Language { get; init; } = "vi-VN";
    
    public bool IsSaveUsername { get; init; } = false;
    public string? SavedUserName { get; init; } = null;
    
    public required int MaxScheduleProfiles { get; init; }
}