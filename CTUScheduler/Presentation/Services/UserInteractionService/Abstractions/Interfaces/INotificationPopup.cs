using Avalonia.Controls.Notifications;
using CTUScheduler.Presentation.Services.UserInteractionService.Abstractions.Models;

namespace CTUScheduler.Presentation.Services.UserInteractionService.Abstractions.Interfaces;

public interface INotificationPopup : INotificationTypeAccessor
{
    /// <summary>
    /// Notification With a Light background 
    /// </summary>
    INotificationTypeAccessor Light { get; }

    void Show(
        object content,
        NotificationType type,
        in NotificationOptions options = default);
}