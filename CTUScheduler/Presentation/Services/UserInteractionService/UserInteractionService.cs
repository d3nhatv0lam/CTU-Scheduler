using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Controls;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using Microsoft.Extensions.Logging;
using Semi.Avalonia.Locale;

namespace CTUScheduler.Presentation.Services.UserInteractionService;

/// <summary>
/// Facade class
/// </summary>
public class UserInteractionService : IUserInteractionService
{
    public IDialogService Dialog { get; }
    public INotificationService Notification { get; }
    public IToastService Toast { get; }

    public UserInteractionService(IDialogService dialogService,
        INotificationService notificationService,
        IToastService toastService)
    {
        Dialog = dialogService;
        Notification = notificationService;
        Toast = toastService;
    }
}