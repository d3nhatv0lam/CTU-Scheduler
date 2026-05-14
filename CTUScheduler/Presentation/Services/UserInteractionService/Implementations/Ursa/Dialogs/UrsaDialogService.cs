using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
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
        var control = GetControl(viewModel, options);

        if (control is null)
            throw new InvalidOperationException($"Không tìm thấy View cho {typeof(TViewModel).Name}");

        var action = options.OnClosed;
        if (action is not null)
        {
            control.Unloaded += (_, _) => action();
        }

        OverlayDialog.ShowStandard(control, viewModel, hostId: options.HostId, options: MapOptions(in options));
    }

    public Task<TResult?> ShowModal<TViewModel, TResult>(TViewModel viewModel, in DialogOptions options = default)
        where TViewModel : class
    {
        var control = GetControl(viewModel, options);

        if (control is null)
            return Task.FromException<TResult?>(
                new InvalidOperationException($"Không tìm thấy View cho {typeof(TViewModel).Name}"));


        var action = options.OnClosed;
        if (action is not null)
        {
            control.Unloaded += (_, _) => action();
        }

        return OverlayDialog.ShowCustomAsync<TResult>(control, viewModel, hostId: options.HostId,
            options: MapOptions(in options));
    }


    private Control? GetControl<TViewModel>(TViewModel viewModel, in DialogOptions options)
    {
        var dataTemplate = Application.Current?.DataTemplates.FirstOrDefault(t => t.Match(viewModel));

        var control = dataTemplate is not null
            ? dataTemplate.Build(viewModel) as Control
            : _viewLocator.ResolveView(viewModel) as Control;

        if (control is null) return null;

        if (options.SizeMode == DialogSizeMode.Absolute)
        {
            return new Border
            {
                Child = control,
                Width = options.Width ?? double.NaN,
                Height = options.Height ?? double.NaN
            };
        }

        if (options.SizeMode == DialogSizeMode.Responsive)
        {
            var horizontalMargin = options.ResponsiveHorizontalMargin;
            var verticalMargin = options.ResponsiveVerticalMargin;
            var percentage = options.ResponsivePercentage;

            return new Border
            {
                Child = control,
                [!Layoutable.HeightProperty] = new Binding("Bounds.Height")
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor)
                    {
                        AncestorType = typeof(OverlayDialogHost)
                    },
                    Converter = new FuncValueConverter<double, double>(h =>
                        Math.Max(0, (h * percentage) - verticalMargin))
                },
                [!Layoutable.WidthProperty] = new Binding("Bounds.Width")
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor)
                    {
                        AncestorType = typeof(OverlayDialogHost)
                    },
                    Converter = new FuncValueConverter<double, double>(w =>
                        Math.Max(0, (w * percentage) - horizontalMargin))
                }
            };
        }

        return control;
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