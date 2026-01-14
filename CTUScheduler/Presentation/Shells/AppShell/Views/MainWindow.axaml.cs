using System.Reactive.Disposables.Fluent;
using CTUScheduler.Presentation.Shells.AppShell.ViewModels;
using ReactiveUI;
using ReactiveUI.Avalonia;
using Ursa.ReactiveUIExtension;

namespace CTUScheduler.Presentation.Shells.AppShell.Views;

public partial class MainWindow : ReactiveUrsaWindow<MainViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
        this.WhenActivated(disposables => { });
    }
}
