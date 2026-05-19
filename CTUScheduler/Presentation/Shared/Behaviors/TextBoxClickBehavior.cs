using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactions.Custom;
using Avalonia.Xaml.Interactivity;

namespace CTUScheduler.Presentation.Shared.Behaviors;

public class TextBoxClickBehavior: AttachedToVisualTreeBehavior<TextBox>
{
    public static readonly StyledProperty<ICommand> ClickCommandProperty =
        AvaloniaProperty.Register<TextBoxClickBehavior, ICommand>(nameof(ClickCommand));

    public ICommand ClickCommand
    {
        get => GetValue(ClickCommandProperty);
        set => SetValue(ClickCommandProperty, value);
    }

    protected override IDisposable OnAttachedToVisualTreeOverride()
    {
        if (AssociatedObject != null)
        {
            AssociatedObject.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, handledEventsToo: true);
            return new DisposableAction(() => 
                AssociatedObject.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed));
        }
        return new DisposableAction(() => { });
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ClickCommand?.CanExecute(null) == true)
            ClickCommand.Execute(null);
    }
}

