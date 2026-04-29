using Avalonia.Controls.Notifications;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using CTUScheduler.Presentation.Services.UserInteractionService.Models;

namespace CTUScheduler.Presentation.Services.UserInteractionService.Implementations.Ursa.Notifications.Proxies;

internal class NotificationThemeProxy(
    INotificationTypeAccessor parent,
    NotificationTheme theme) : INotificationTypeAccessor
{
    public void Show(object content, NotificationType type, string? title, in NotificationOptions options = default) =>
        parent.Show(content, type, title, options with { Theme = theme });

    public void Show(object content, NotificationType type, in NotificationOptions options = default) =>
        parent.Show(content, type, options with { Theme = theme });

    public void Info(string title, object content, in NotificationOptions options = default) =>
        Show(content, NotificationType.Information, title, options with { Theme = theme });

    public void Info(object content, in NotificationOptions options = default) =>
        Show(content, NotificationType.Information, options with { Theme = theme });

    public void Success(string title, object content, in NotificationOptions options = default) =>
        Show(content, NotificationType.Success, title, options with { Theme = theme });

    public void Success(object content, in NotificationOptions options = default) =>
        Show(content, NotificationType.Success, options with { Theme = theme });

    public void Warning(string title, object content, in NotificationOptions options = default) =>
        Show(content, NotificationType.Warning, title, options with { Theme = theme });
    
    public void Warning(object content, in NotificationOptions options = default) =>
        Show(content, NotificationType.Warning, options with { Theme = theme });

    public void Error(string title, object content, in NotificationOptions options = default) =>
        Show(content, NotificationType.Error, title, options with { Theme = theme });

    public void Error(object content, in NotificationOptions options = default) =>
        Show(content, NotificationType.Error, options with { Theme = theme });
}