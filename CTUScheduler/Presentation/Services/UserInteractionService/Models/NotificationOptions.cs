using System;

namespace CTUScheduler.Presentation.Services.UserInteractionService.Models;

public readonly record struct NotificationOptions()
{
    
    //sắp xếp tối ưu memory alignment
    public Action? OnClick { get; init; } = null;
    public Action? OnClose { get; init; } = null;
    public string[]? Classes { get; init; } = null;
    public TimeSpan? Expiration { get; init; } = TimeSpan.FromSeconds(5);
    
    public NotificationTheme Theme { get; init; } = NotificationTheme.Normal;
    public bool ShowIcon { get; init; } = true;
    public bool ShowClose { get; init; } = true;
}

internal static class NotificationCache
{
    public static readonly string[] LightThemeClass = [nameof(NotificationTheme.Light)];
}