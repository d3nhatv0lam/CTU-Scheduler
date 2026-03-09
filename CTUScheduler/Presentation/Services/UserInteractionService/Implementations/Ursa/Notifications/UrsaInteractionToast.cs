using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using CTUScheduler.Presentation.Services.UserInteractionService.Implementations.Ursa.Notifications.Base;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using CTUScheduler.Presentation.Services.UserInteractionService.Models;
using CTUScheduler.Presentation.Services.ViewContext.Interfaces;
using Microsoft.Extensions.Logging;
using Ursa.Controls;

namespace CTUScheduler.Presentation.Services.UserInteractionService.Implementations.Ursa.Notifications;

public class UrsaInteractionToast(IViewContextService viewContextService, ILogger<UrsaInteractionToast> logger)
    : UrsaInteractionManagerBase<WindowToastManager>(viewContextService, logger), IToastService
{
    protected override WindowToastManager CreateManager(TopLevel context) =>
        new (context) { MaxItems = 5 };

    protected override void UninstallManager(WindowToastManager manager) => manager.Uninstall();

    protected override void InvokeShow(WindowToastManager manager, NotificationType type, object content,
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