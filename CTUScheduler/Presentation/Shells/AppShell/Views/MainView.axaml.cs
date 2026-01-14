using System.Reactive.Disposables.Fluent;
using CTUScheduler.Presentation.Shells.AppShell.ViewModels;
using ReactiveUI;
using Ursa.ReactiveUIExtension;

namespace CTUScheduler.Presentation.Shells.AppShell.Views;

public partial class MainView : ReactiveUrsaView<MainViewModel>
{
    public MainView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, 
                x => x.Router, 
                x => x.RoutedViewHost.Router!)
                .DisposeWith(disposables);
        });
    }
}