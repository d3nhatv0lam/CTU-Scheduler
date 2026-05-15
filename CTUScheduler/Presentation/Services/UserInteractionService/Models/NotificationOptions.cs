using System;

namespace CTUScheduler.Presentation.Services.UserInteractionService.Models;

public readonly record struct NotificationOptions()
{
    
    //sắp xếp tối ưu memory alignment
    public Action? OnClick { get; init; } = null;
    public Action<MessageCloseReason>? OnClose { get; init; } = null;
    public string[]? Classes { get; init; } = null;
    public TimeSpan? Expiration { get; init; } = TimeSpan.FromSeconds(5);
    
    public NotificationTheme Theme { get; init; } = NotificationTheme.Normal;
    public bool ShowIcon { get; init; } = true;
    public bool ShowClose { get; init; } = true;
}

public enum MessageCloseReason
{
    /// <summary>
    /// The message closed because its display duration expired.
    /// </summary>
    Timeout,

    /// <summary>
    /// The message was closed by an explicit user action (e.g., clicking the close button).
    /// </summary>
    UserAction,

    /// <summary>
    /// The message was closed because a newer message arrived, displacing it due to the MaxItems limit.
    /// </summary>
    Displaced
}

internal static class NotificationCache
{
    public static readonly string[] LightThemeClass = [nameof(NotificationTheme.Light)];
}