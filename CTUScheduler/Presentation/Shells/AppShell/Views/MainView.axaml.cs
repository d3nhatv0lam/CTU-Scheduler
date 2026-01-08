using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using ReactiveUI.Avalonia;
using CTUScheduler.Presentation.Shells.AppShell.ViewModels;
using ReactiveUI;

namespace CTUScheduler.Presentation.Shells.AppShell.Views;

public partial class MainView : ReactiveUserControl<MainViewModel>
{
    public MainView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.OneWayBind<MainViewModel, MainView, RoutingState, RoutingState>(ViewModel, x => x.Router, x => x.RoutedViewHost.Router).DisposeWith(disposables);
        });
    }
}
