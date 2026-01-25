using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using CTUScheduler.Presentation.Services.UserInteractionService.Abstractions.Interfaces;
using CTUScheduler.Presentation.Services.UserInteractionService.Abstractions.Models;
using CTUScheduler.Presentation.Services.UserInteractionService.Infrastructure.Ursa.Notifications.Base;
using CTUScheduler.Presentation.Services.ViewContext;
using Microsoft.Extensions.Logging;
using WindowNotificationManager = Ursa.Controls.WindowNotificationManager;

namespace CTUScheduler.Presentation.Services.UserInteractionService.Infrastructure.Ursa.Notifications;

public class UrsaInteractionNotification(IViewContextService viewContextService, ILogger<UrsaInteractionNotification> logger)
    : UrsaInteractionManagerBase<WindowNotificationManager>(viewContextService, logger), INotificationService
{
    protected override WindowNotificationManager CreateManager(TopLevel context) =>
        new (context) { MaxItems = 5, Position = NotificationPosition.BottomRight};

    protected override void UninstallManager(WindowNotificationManager manager) => manager.Uninstall();

    protected override void InvokeShow(WindowNotificationManager manager, NotificationType type, object content,
        in NotificationOptions opt)
    {
        manager.Show(content,
            type,
            expiration: opt.Expiration,
            showIcon: opt.ShowIcon,
            showClose: opt.ShowClose,
            onClick: opt.OnClick,
            onClose: opt.OnClose,
            classes: opt.Classes
        );
    }
}