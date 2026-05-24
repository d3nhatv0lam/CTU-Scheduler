using System;
using System.Runtime.CompilerServices;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using Ursa.Controls;

namespace CTUScheduler.Presentation.Services.UserInteractionService.Behaviors;

public class RefreshOnResizeBehavior : Behavior<OverlayDialogHost>
{
    private bool _isUpdating;
    private IDisposable? _subscription;
    private readonly ConditionalWeakTable<Visual, BoxedSize> _lastSizes = new();

    private class BoxedSize
    {
        public Size Size { get; set; }
    }

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
        if (AssociatedObject is null) return;

        var top = TopLevel.GetTopLevel(AssociatedObject);
        if (top is null) return;

        _subscription = top.GetObservable(Visual.BoundsProperty)
            .Subscribe(_ => { ForceUrsaLayoutUpdateAsync(); });

        AssociatedObject.LayoutUpdated += OnHostLayoutUpdated;
    }

    private void OnHostLayoutUpdated(object? sender, EventArgs e)
    {
        if (AssociatedObject is null || _isUpdating) return;

        bool childSizeChanged = false;
        foreach (var child in AssociatedObject.GetVisualChildren())
        {
            var currentSize = child.Bounds.Size;
            if (_lastSizes.TryGetValue(child, out var boxed))
            {
                if (boxed.Size != currentSize)
                {
                    boxed.Size = currentSize;
                    childSizeChanged = true;
                }
            }
            else
            {
                _lastSizes.Add(child, new BoxedSize { Size = currentSize });
                if (currentSize.Width > 0 && currentSize.Height > 0)
                {
                    childSizeChanged = true;
                }
            }
        }

        if (childSizeChanged)
        {
            // Tự động trigger margin để căn giữa lại khi đổi mode hoặc back về selection view
            ForceUrsaLayoutUpdateAsync();
        }
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
        if (AssociatedObject is not null)
        {
            AssociatedObject.LayoutUpdated -= OnHostLayoutUpdated;
        }
    }
}