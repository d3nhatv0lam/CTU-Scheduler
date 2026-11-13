using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using CTUScheduler.Presentation.Services.ViewContext.Interfaces;
using DynamicData;

namespace CTUScheduler.Presentation.Services.ViewContext.Behaviors;

public sealed class ViewContextBehavior : Behavior<Control>
{
    private IDisposable? _dataContextSubscription;

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject is null) return;

        _dataContextSubscription = AssociatedObject
            .GetObservable(StyledElement.DataContextProperty)
            .Subscribe(_ => UpdateViewContext());

        AssociatedObject.AttachedToVisualTree += OnAssociatedObjectAttachedToVisualTree;
        AssociatedObject.DetachedFromVisualTree += OnAssociatedObjectDetachedFromVisualTree;
        UpdateViewContext();
    }

    protected override void OnDetaching()
    {
        _dataContextSubscription?.Dispose();
        _dataContextSubscription = null;

        if (AssociatedObject is not null)
        {
            AssociatedObject.AttachedToVisualTree -= OnAssociatedObjectAttachedToVisualTree;
            AssociatedObject.DetachedFromVisualTree -= OnAssociatedObjectDetachedFromVisualTree;
        }

        ResetViewContext();

        base.OnDetaching();
    }

    private void OnAssociatedObjectAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        ResetViewContext();
    }

    private void OnAssociatedObjectDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        UpdateViewContext();
    }
    
    private void UpdateViewContext()
    {
        var topLevel = TopLevel.GetTopLevel(AssociatedObject);
        if (topLevel is null)
            return;
        if (AssociatedObject?.DataContext is IViewContext context)
        {
            context.ViewContext.SetTopLevel(topLevel);
        }
    }

    private void ResetViewContext()
    {
        if (AssociatedObject?.DataContext is IViewContext context)
        {
            context.ViewContext.SetTopLevel(null);
        }
    }
}