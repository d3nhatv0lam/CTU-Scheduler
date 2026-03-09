using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using CTUScheduler.Presentation.Services.UserInteractionService.Models.Dialogs;
using CTUScheduler.Presentation.Services.ViewContext.Interfaces;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Ursa.Controls;
// Alias để tránh trùng tên
using DialogOptions =
    CTUScheduler.Presentation.Services.UserInteractionService.Models.Dialogs.DialogOptions;

namespace CTUScheduler.Presentation.Services.UserInteractionService.Implementations.Ursa.Dialogs;

public class UrsaDialogService : IDialogService
{
    private readonly IViewContextService _viewContextService;
    private readonly ILogger<UrsaDialogService> _logger;
    private readonly IViewLocator _viewLocator;

    public UrsaDialogService(IViewContextService viewContextService, IViewLocator locator,
        ILogger<UrsaDialogService> logger)
    {
        _viewContextService = viewContextService;
        _logger = logger;
        _viewLocator = locator;
    }

    public Task ShowAlert(string title, string message)
    {
        throw new System.NotImplementedException();
    }

    public Task<bool> ShowConfirm(string title, string message)
    {
        throw new System.NotImplementedException();
    }

    public void Show<TViewModel>(TViewModel viewModel, in DialogOptions options = default) where TViewModel : class
    {
        var view = _viewLocator.ResolveView(viewModel);

        if (view is not Control control)
            throw new InvalidOperationException($"Không tìm thấy View cho {typeof(TViewModel).Name}");

        var action = options.OnClosed;
        if (action is not null)
        {
            control.Unloaded += (sender, args) => action();
        }

        OverlayDialog.Show(control, viewModel, hostId: options.HostId, options: MapOptions(in options));
    }

    public Task<TResult?> ShowModal<TViewModel, TResult>(TViewModel viewModel, in DialogOptions options = default)
        where TViewModel : class
    {
        var view = _viewLocator.ResolveView(viewModel);
        if (view is not Control control)
            throw new InvalidOperationException($"Không tìm thấy View cho {typeof(TViewModel).Name}");
        
        var action = options.OnClosed;
        if (action is not null)
        {
            control.Unloaded += (sender, args) => action();
        }

        return OverlayDialog.ShowCustomModal<TResult>(control, viewModel, hostId: options.HostId,
            options: MapOptions(in options));
    }


    private OverlayDialogOptions MapOptions(in DialogOptions options)
    {
        return new OverlayDialogOptions
        {
            Title = options.Title ?? "Notification",
            FullScreen = options.FullScreen,

            // Map Vị trí
            HorizontalAnchor = MapHorizontal(options.HorizontalAlignment),
            VerticalAnchor = MapVertical(options.VerticalAlignment),
            HorizontalOffset = options.HorizontalOffset,
            VerticalOffset = options.VerticalOffset,

            // Map Nút bấm
            Buttons = MapButtons(options.Buttons),

            // Map Hành vi
            IsCloseButtonVisible = options.IsCloseButtonVisible,
            CanLightDismiss = options.CanLightDismiss,
            CanDragMove = options.CanDragMove,
            CanResize = options.CanResize,

            // Style
            StyleClass = options.StyleClass,

            // Context (SSOT)
            TopLevelHashCode = options.HostId == null
                ? _viewContextService.CurrentTopLevel?.GetHashCode()
                : null
        };
    }

    private static HorizontalPosition MapHorizontal(DialogHorizontalAlignment align) => align switch
    {
        DialogHorizontalAlignment.Left => HorizontalPosition.Left,
        DialogHorizontalAlignment.Right => HorizontalPosition.Right,
        _ => HorizontalPosition.Center
    };

    private static VerticalPosition MapVertical(DialogVerticalAlignment align) => align switch
    {
        DialogVerticalAlignment.Top => VerticalPosition.Top,
        DialogVerticalAlignment.Bottom => VerticalPosition.Bottom,
        _ => VerticalPosition.Center
    };

    private static DialogButton MapButtons(DialogButtons buttons) => (DialogButton)buttons;
}