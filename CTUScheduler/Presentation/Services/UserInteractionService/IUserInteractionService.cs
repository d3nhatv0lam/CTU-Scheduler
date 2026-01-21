using Avalonia.Controls;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Presentation.Services.UserInteractionService.Components;

namespace CTUScheduler.Presentation.Services.UserInteractionService;

public interface IUserInteractionService: IInitializable<TopLevel?> 
{
    IDialogService Dialog { get; }
    INotificationService Notification { get; }
    IToastService Toast { get; }
}