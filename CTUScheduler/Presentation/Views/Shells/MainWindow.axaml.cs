using Avalonia.Controls;
using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.ViewModels.Shells;
using ReactiveUI;
using System.Reactive.Disposables;

namespace CTUScheduler.Presentation.Views.Shells;

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
