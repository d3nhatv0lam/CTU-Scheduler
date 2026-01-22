using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Controls;
using CTUScheduler.Presentation.Services.UserInteractionService.Components;
using Microsoft.Extensions.Logging;
using Semi.Avalonia.Locale;

namespace CTUScheduler.Presentation.Services.UserInteractionService;

public class UserInteractionService : IUserInteractionService, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly ILogger<UserInteractionService> _logger;
    private readonly BehaviorSubject<TopLevel?> _toplevelSubject = new(null);
    public IDialogService Dialog { get; }
    public INotificationService Notification { get; }
    public IToastService Toast { get; }

    public UserInteractionService(ILogger<UserInteractionService> logger)
    {
        Dialog = new DialogService();
        Notification = new NotificationService()
            .DisposeWith(_disposables);
        Toast = new ToastService()
            .DisposeWith(_disposables);
        _logger = logger;

        _toplevelSubject.DisposeWith(_disposables);
        
        _toplevelSubject
            .Subscribe(toplevel =>
            {
                Toast.Initialize(toplevel);
                Notification.Initialize(toplevel);
            })
            .DisposeWith(_disposables);
    }

    public void Initialize(TopLevel? topLevel)
    {
        _logger.LogDebug("Initializing UserInteractionService");
        _toplevelSubject.OnNext(topLevel);
    }

    public void Dispose()
    {
        _disposables.Dispose();
        _logger.LogInformation("UserInteractionService disposed");
    }
}