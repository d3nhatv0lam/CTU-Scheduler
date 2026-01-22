using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using Splat;

namespace CTUScheduler.Presentation.Services.UserInteractionService.Behaviors;

public sealed class UserInteractionBehavior : Behavior<Control>
{
    // public static readonly StyledProperty<IUserInteractionService?> ServiceProperty =
    //     AvaloniaProperty.Register<UserInteractionBehavior, IUserInteractionService?>(
    //         nameof(Service));
    //
    // public IUserInteractionService? Service
    // {
    //     get => GetValue(ServiceProperty);
    //     set => SetValue(ServiceProperty, value);
    // }
    
    protected override void OnAttachedToVisualTree()
    {
        base.OnAttachedToVisualTree();
        
        var topLevel = TopLevel.GetTopLevel(AssociatedObject);
        if (topLevel is null)
            return;
        
        if (AssociatedObject?.DataContext is IUserInteractionContext context)
        {
            context.UserInteraction.Initialize(topLevel);
        }
        else
        {
            throw new InvalidOperationException(
                "Behavior must be attached to a control with a DataContext that implements IUserInteractionContext");
        }
    }

    protected override void OnDetachedFromVisualTree()
    {
        if (AssociatedObject?.DataContext is IUserInteractionContext context)
        {
            context.UserInteraction.Initialize(null);
        }
        
        base.OnDetachedFromVisualTree();
    }
}