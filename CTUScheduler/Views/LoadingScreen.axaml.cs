using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using CTUScheduler.ViewModels;
using ReactiveUI;
using System.Reactive.Disposables;

namespace CTUScheduler.Views;

public partial class LoadingScreen : ReactiveWindow<LoadingScreenViewModel>
{
    public LoadingScreen()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.BindCommand(ViewModel, x => x.CloseAppCommand, x => x.CloseAppButton).DisposeWith(disposables);
            this.OneWayBind(ViewModel, x => x.Message, x => x.MessageTextBlock.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, x => x.Version, x => x.AppVersionTextBlock.Text, version => $"Version: {version}").DisposeWith(disposables);
        });
    }
}