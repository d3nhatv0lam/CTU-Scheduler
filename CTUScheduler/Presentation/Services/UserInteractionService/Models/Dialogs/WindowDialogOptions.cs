using System;
using Avalonia;
using Avalonia.Controls;

namespace CTUScheduler.Presentation.Services.UserInteractionService.Models.Dialogs;

public readonly record struct WindowDialogOptions()
{
    // Thông tin cơ bản
    public string? Title { get; init; } = null;
    public string? StyleClass { get; init; } = null;
    public Action? OnClosed { get; init; } = null;
    public DialogSizeMode SizeMode { get; init; } = DialogSizeMode.Content;
    // Vị trí mở cửa sổ
    public WindowStartupLocation StartupLocation { get; init; } = WindowStartupLocation.CenterOwner;
    public PixelPoint? Position { get; init; } = null;
    // Kích thước
    public double? Width { get; init; } = null;
    public double? Height { get; init; } = null;
    // Nút bấm & Hành vi của Window
    public DialogButtons Buttons { get; init; } = DialogButtons.None;
    public bool ShowInTaskBar { get; init; } = false; // Dialog con thường không cần hiện Taskbar
    public bool IsCloseButtonVisible { get; init; } = true;
    public bool CanDragMove { get; init; } = true;
    public bool CanResize { get; init; } = false;
}