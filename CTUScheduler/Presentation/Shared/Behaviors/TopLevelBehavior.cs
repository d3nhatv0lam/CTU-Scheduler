using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using CTUScheduler.Presentation.Services.UserInteractionService;
using Splat;

namespace CTUScheduler.Presentation.Shared.Behaviors;

public class TopLevelBehavior : Behavior<TopLevel>
{
    protected override void OnAttachedToVisualTree()
    {
        base.OnAttachedToVisualTree();

        if (AssociatedObject is null) return;

        var userInteraction = Locator.Current.GetService<IUserInteractionService>();
        userInteraction?.Initialize(this.AssociatedObject);
    }

    protected override void OnDetachedFromVisualTree()
    {
        base.OnDetachedFromVisualTree();

        var userInteraction = Locator.Current.GetService<IUserInteractionService>();
            userInteraction?.Initialize(null);
        
    }
}