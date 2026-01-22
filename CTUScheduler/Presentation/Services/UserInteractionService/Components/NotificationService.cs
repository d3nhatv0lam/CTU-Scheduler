using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using WindowNotificationManager = Ursa.Controls.WindowNotificationManager;

namespace CTUScheduler.Presentation.Services.UserInteractionService.Components;

public class NotificationService : INotificationService, IDisposable
{
    private WindowNotificationManager? _notificationManager;

    public void Initialize(TopLevel? context)
    {
        _notificationManager?.Uninstall();
        _notificationManager = null;
        if (context is null) return;
        
        _notificationManager = new(context)
        {
            MaxItems = 5,
        };

        foreach (var _ in Enumerable.Range(0, 10))
        {
            _notificationManager.Show(content: "test nè", NotificationType.Success, expiration: TimeSpan.FromSeconds(10)
            );
        }
    }

    public void Dispose()
    {
        _notificationManager?.Uninstall();
        _notificationManager = null;
    }
}