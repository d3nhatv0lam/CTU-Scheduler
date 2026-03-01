using CTUScheduler.Presentation.Services.UserInteractionService.Models;

namespace CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;

public interface INotificationTypeAccessor
{
    void Info(object content, in NotificationOptions options = default);
    void Success(object content, in NotificationOptions options = default);
    void Warning(object content, in NotificationOptions options = default);
    void Error(object content, in NotificationOptions options = default);
}