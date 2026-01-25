using CTUScheduler.Presentation.Services.UserInteractionService.Abstractions.Models;

namespace CTUScheduler.Presentation.Services.UserInteractionService.Abstractions.Interfaces;

public interface INotificationTypeAccessor
{
    void Info(object content, in NotificationOptions optionses = default);
    void Success(object content, in NotificationOptions optionses = default);
    void Warning(object content, in NotificationOptions optionses = default);
    void Error(object content, in NotificationOptions optionses = default);
}