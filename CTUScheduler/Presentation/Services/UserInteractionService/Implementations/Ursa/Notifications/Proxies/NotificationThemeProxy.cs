using Avalonia.Controls.Notifications;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using CTUScheduler.Presentation.Services.UserInteractionService.Models;

namespace CTUScheduler.Presentation.Services.UserInteractionService.Implementations.Ursa.Notifications.Proxies;

internal class NotificationThemeProxy(
    INotificationPopup parent,
    NotificationTheme theme) : INotificationTypeAccessor
{
    public void Info(object content, in NotificationOptions options = default) =>
        parent.Show(content, NotificationType.Information, options with { Theme = theme });

    public void Success(object content, in NotificationOptions options = default) =>
        parent.Show(content, NotificationType.Success, options with { Theme = theme });

    public void Warning(object content, in NotificationOptions options = default) =>
        parent.Show(content, NotificationType.Warning, options with { Theme = theme });

    public void Error(object content, in NotificationOptions options = default) =>
        parent.Show(content, NotificationType.Error, options with { Theme = theme });
}