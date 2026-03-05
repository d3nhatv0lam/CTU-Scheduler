using System;

namespace CTUScheduler.Presentation.Services.UserInteractionService.Models.Dialogs;

public readonly record struct DialogOptions()
{
    // Pointers (8 bytes)
    public string? Title { get; init; } = null;
    public string? HostId { get; init; } = null;
    public string? StyleClass { get; init; } = null;
    public Action? OnClosed { get; init; } = null;

    // Nullable Doubles (8-16 bytes)
    public double? HorizontalOffset { get; init; } = null;
    public double? VerticalOffset { get; init; } = null;

    // Enums (4 bytes)
    public DialogHorizontalAlignment HorizontalAlignment { get; init; } = DialogHorizontalAlignment.Center;
    public DialogVerticalAlignment VerticalAlignment { get; init; } = DialogVerticalAlignment.Center;
    public DialogButtons Buttons { get; init; } = DialogButtons.OKCancel;

    // Bools (1 byte)
    public bool FullScreen { get; init; } = false;
    public bool IsCloseButtonVisible { get; init; } = true;
    public bool CanLightDismiss { get; init; } = false;
    public bool CanDragMove { get; init; } = false;
    public bool CanResize { get; init; } = false;
}