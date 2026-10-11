using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using CTUScheduler.Presentation.Services.ViewContext.Interfaces;

namespace CTUScheduler.Presentation.Services.ViewContext.Behaviors;

public sealed class ViewContextBehavior : Behavior<Control>
{
    protected override void OnAttachedToVisualTree()
    {
        base.OnAttachedToVisualTree();
        UpdateViewContext();
        // var topLevel = TopLevel.GetTopLevel(AssociatedObject);
        // if (topLevel is null)
        //     return;
        //
        // if (AssociatedObject?.DataContext is IViewContext context)
        // {
        //     context.ViewContext.SetTopLevel(topLevel);
        // }
        // else
        // {
        //     throw new InvalidOperationException(
        //         $"Behavior must be attached to a control with a DataContext that implements {typeof(IViewContext).FullName}");
        // }
    }

    protected override void OnDetachedFromVisualTree()
    {
        ResetViewContext();

        base.OnDetachedFromVisualTree();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == StyledElement.DataContextProperty)
        {
            UpdateViewContext();
        }
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