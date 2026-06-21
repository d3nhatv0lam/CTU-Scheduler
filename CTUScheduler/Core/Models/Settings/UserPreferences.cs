namespace CTUScheduler.Core.Models.Settings;

public record UserPreferences
{
    public int Version { get; init; } = 1;
    public AppearanceSettings Appearance { get; init; } = new();
    public AuthSettings Auth { get; init; } = new();
    public ScheduleSettings Schedule { get; init; } = new();
    public BehaviorSettings Behavior { get; init; } = new();
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

public record ScheduleSettings
{
    public bool AutoSaveEnabled { get; init; } = true;
    public int MaxScheduleProfiles { get; init; } = AppConstants.DefaultMaxScheduleProfiles;
    public int ReminderAdvanceMinutes { get; init; } = 15;
}

public record BehaviorSettings
{
    public bool RunOnStartup { get; init; } = false;
    public bool MinimizeToTray { get; init; } = false;
    public bool IsTermsAccepted { get; init; } = false;
}


