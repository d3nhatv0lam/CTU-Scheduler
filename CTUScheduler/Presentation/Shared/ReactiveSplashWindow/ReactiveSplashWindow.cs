using Avalonia;
using ReactiveUI;
using Ursa.Controls;

namespace CTUScheduler.Presentation.Shared.ReactiveSplashWindow;

public abstract class ReactiveSplashWindow<TViewModel> : SplashWindow, IViewFor<TViewModel>
    where TViewModel : class
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1002")]
    public static readonly StyledProperty<TViewModel?> ViewModelProperty = AvaloniaProperty
        .Register<ReactiveSplashWindow<TViewModel>, TViewModel?>(nameof(ViewModel));

    public ReactiveSplashWindow()
    {
        // Kích hoạt cơ chế WhenActivated của ReactiveUI
        this.WhenActivated(disposables => { });
    }

    public TViewModel? ViewModel
    {
        get => GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (TViewModel?)value;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        if (change.Property == DataContextProperty)
        {
            if (ReferenceEquals(change.OldValue, ViewModel) &&
                (change.NewValue is null or TViewModel))
            {
                SetCurrentValue(ViewModelProperty, change.NewValue);
            }
        }
        else if (change.Property == ViewModelProperty)
        {
            if (ReferenceEquals(change.OldValue, DataContext))
            {
                SetCurrentValue(DataContextProperty, change.NewValue);
            }
        }
    }
}