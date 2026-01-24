using System;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using CTUScheduler.Presentation.Services.ViewContext.Interfaces;

namespace CTUScheduler.Presentation.Services.ViewContext.Behaviors;

public sealed class ViewContextBehavior: Behavior<Control>
{
    protected override void OnAttachedToVisualTree()
    {
        base.OnAttachedToVisualTree();
        
        var topLevel = TopLevel.GetTopLevel(AssociatedObject);
        if (topLevel is null)
            return;
        
        if (AssociatedObject?.DataContext is IViewContext context)
        {
            context.ViewContext.SetTopLevel(topLevel);
        }
        else
        {
            throw new InvalidOperationException(
                $"Behavior must be attached to a control with a DataContext that implements {typeof(IViewContext).FullName}");
        }
    }

    protected override void OnDetachedFromVisualTree()
    {
        if (AssociatedObject?.DataContext is IViewContext context)
        {
            context.ViewContext.SetTopLevel(null);
        }
        
        base.OnDetachedFromVisualTree();
    }
}