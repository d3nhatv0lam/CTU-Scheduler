using Avalonia.Controls.Notifications;
using CTUScheduler.Presentation.Services.UserInteractionService.Abstractions.Interfaces;
using CTUScheduler.Presentation.Services.UserInteractionService.Abstractions.Models;

namespace CTUScheduler.Presentation.Services.UserInteractionService.Infrastructure.Ursa.Notifications.Proxies;

internal class NotificationThemeProxy(
    INotificationPopup parent,
    NotificationTheme theme) : INotificationTypeAccessor
{
    public void Info(object content, in NotificationOptions optionses = default) =>
        parent.Show(content, NotificationType.Information, optionses with { Theme = theme });

    public void Success(object content, in NotificationOptions optionses = default) =>
        parent.Show(content, NotificationType.Success, optionses with { Theme = theme });

    public void Warning(object content, in NotificationOptions optionses = default) =>
        parent.Show(content, NotificationType.Warning, optionses with { Theme = theme });

    public void Error(object content, in NotificationOptions optionses = default) =>
        parent.Show(content, NotificationType.Error, optionses with { Theme = theme });
}