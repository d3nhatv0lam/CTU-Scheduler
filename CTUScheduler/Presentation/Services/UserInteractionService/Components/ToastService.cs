using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Ursa.Controls;

namespace CTUScheduler.Presentation.Services.UserInteractionService.Components;

public class ToastService: IToastService
{
    private WindowToastManager? _toastManager = null;
    
    public void Initialize(TopLevel? context)
    {
        _toastManager?.Uninstall();
        
        _toastManager = new WindowToastManager(context)
        {
            MaxItems = 10,
            
        };

        foreach (var _ in Enumerable.Range(0, 10))
        {
            _toastManager.Show("hello", NotificationType.Warning, TimeSpan.FromSeconds(10));
        }
    }
}