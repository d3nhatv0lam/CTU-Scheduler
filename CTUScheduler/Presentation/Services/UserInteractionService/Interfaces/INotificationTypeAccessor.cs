using Avalonia.Controls.Notifications;
using CTUScheduler.Presentation.Services.UserInteractionService.Models;

namespace CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;

public interface INotificationTypeAccessor
{
    void Show(object content, NotificationType type, string? title, in NotificationOptions options = default);
    void Show(object content, NotificationType type, in NotificationOptions options = default);

    // info
    void Info(string title, object content, in NotificationOptions options = default);
    void Info(object content, in NotificationOptions options = default);

    // success
    void Success(string title, object content, in NotificationOptions options = default);
    void Success(object content, in NotificationOptions options = default);

    // warning
    void Warning(string title, object content, in NotificationOptions options = default);
    void Warning(object content, in NotificationOptions options = default);

    // error
    void Error(string title, object content, in NotificationOptions options = default);
    void Error(object content, in NotificationOptions options = default);
}