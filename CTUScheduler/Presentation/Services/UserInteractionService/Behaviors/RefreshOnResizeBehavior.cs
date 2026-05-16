using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using Ursa.Controls;

namespace CTUScheduler.Presentation.Services.UserInteractionService.Behaviors;

public class RefreshOnResizeBehavior : Behavior<OverlayDialogHost>
{
    private bool _isUpdating;
    private IDisposable? _subscription;

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is null) return;
        AssociatedObject.AttachedToVisualTree += OnAttachedToTree;
        AssociatedObject.DetachedFromVisualTree += OnDetachedFromTree;
    }

    protected override void OnDetaching()
    {
        Cleanup();
        if (AssociatedObject is not null)
        {
            AssociatedObject.AttachedToVisualTree -= OnAttachedToTree;
            AssociatedObject.DetachedFromVisualTree -= OnDetachedFromTree;
        }

        base.OnDetaching();
    }

    private void OnAttachedToTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        var top = TopLevel.GetTopLevel(AssociatedObject);

        if (top is null) return;


        _subscription = top.GetObservable(Visual.BoundsProperty)
            .Subscribe(_ => { ForceUrsaLayoutUpdateAsync(); });
    }

    private async void ForceUrsaLayoutUpdateAsync()
    {
        if (AssociatedObject is null || _isUpdating) return;
        try
        {
            _isUpdating = true;

            var old = AssociatedObject.Margin;

            AssociatedObject.Margin = new Thickness(old.Left, old.Top + 0.5d, old.Right, old.Bottom);

            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

            AssociatedObject.Margin = old;
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void OnDetachedFromTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        Cleanup();
    }

    private void Cleanup()
    {
        _subscription?.Dispose();
        _subscription = null;
    }
}