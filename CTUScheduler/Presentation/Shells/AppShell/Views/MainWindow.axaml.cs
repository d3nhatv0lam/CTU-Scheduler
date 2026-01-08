using ReactiveUI.Avalonia;
using CTUScheduler.Presentation.Shells.AppShell.ViewModels;
using ReactiveUI;

namespace CTUScheduler.Presentation.Shells.AppShell.Views;

public partial class MainWindow : ReactiveWindow<MainViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            
        });
    }
}
