using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using CTUScheduler.Presentation.Services.UserInteractionService.Implementations.Ursa.Notifications.Base;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using CTUScheduler.Presentation.Services.UserInteractionService.Models;
using CTUScheduler.Presentation.Services.ViewContext.Interfaces;
using CTUScheduler.Presentation.Shared.Models;
using Microsoft.Extensions.Logging;
using Notification = Ursa.Controls.Notification;
using WindowNotificationManager = Ursa.Controls.WindowNotificationManager;

namespace CTUScheduler.Presentation.Services.UserInteractionService.Implementations.Ursa.Notifications;

public class UrsaNotification(
    IViewContextService viewContextService,
    ILogger<UrsaNotification> logger)
    : UrsaInteractionManagerBase<WindowNotificationManager>(viewContextService, logger), INotificationService
{
    protected override WindowNotificationManager CreateManager(TopLevel context) =>
        new(context) { MaxItems = 5, Position = NotificationPosition.BottomRight };

    protected override void UninstallManager(WindowNotificationManager manager) => manager.Uninstall();

    protected override object CreateFinalContent(object content, string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return content;

        // Nếu có title -> noti có tiêu đề
        return content is string msg
            ? new Notification(title, msg) // content là string
            : new ManagedNotification(title, content); // content là ViewModel phức tạp
    }

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