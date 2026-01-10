namespace CTUScheduler.Core.Models.Settings;

public record UserSettings
{
    public AppearanceSettings Appearance { get; init; } = new();
    public AuthSettings Auth { get; init; } = new();
    public GeneralSettings General { get; init; } = new();
}

public record AppearanceSettings
{
    public bool IsDarkMode { get; init; } = false;
    // public string LanguageCode { get; init; } = "vi-VN";
}

public record AuthSettings
{
    public bool IsSaveUsername { get; init; } = false;
    public string? SavedUserName { get; init; } = null;
}

public record GeneralSettings
{
    public bool AutoSaveEnabled { get; init; } = true;
    public int MaxScheduleProfiles { get; init; } = AppConstants.DefaultMaxScheduleProfiles;
}


