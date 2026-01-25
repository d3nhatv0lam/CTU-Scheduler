namespace CTUScheduler.Presentation.Services.UserInteractionService.Abstractions.Interfaces;

public interface IUserInteractionService
{
    IDialogService Dialog { get; }
    INotificationService Notification { get; }
    IToastService Toast { get; }
}