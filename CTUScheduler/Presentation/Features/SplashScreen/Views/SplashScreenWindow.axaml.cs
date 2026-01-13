using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using ReactiveUI.Avalonia;
using CTUScheduler.Presentation.Features.SplashScreen.ViewModels;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.SplashScreen.Views;

public partial class SplashScreenWindow : ReactiveWindow<SplashScreenViewModel>
{
    public SplashScreenWindow()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.BindCommand(ViewModel, x => x.CloseAppCommand, x => x.CloseAppButton).DisposeWith(disposables);
            this.OneWayBind(ViewModel, x => x.Message, x => x.MessageTextBlock.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, x => x.Version, x => x.AppVersionTextBlock.Text, version => $"Version: {version}").DisposeWith(disposables);
            Disposable.Create(ViewModel,vm => vm?.Dispose()).DisposeWith(disposables);
        });
    }

}