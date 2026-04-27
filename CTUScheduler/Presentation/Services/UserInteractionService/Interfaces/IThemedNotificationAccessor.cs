namespace CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;

public interface IThemedNotificationAccessor: INotificationTypeAccessor
{
    /// <summary>
    /// Notification With a Light background 
    /// </summary>
    INotificationTypeAccessor Light { get; }
}