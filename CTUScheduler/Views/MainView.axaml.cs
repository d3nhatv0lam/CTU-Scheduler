using Avalonia.Controls;
using Avalonia.ReactiveUI;
using CTUScheduler.ViewModels;
using ReactiveUI;
using System.Reactive.Disposables;

namespace CTUScheduler.Views;

public partial class MainView : ReactiveUserControl<MainViewModel>
{
    public MainView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, x => x.Router, x => x.RoutedViewHost.Router).DisposeWith(disposables);
        });
    }
}
