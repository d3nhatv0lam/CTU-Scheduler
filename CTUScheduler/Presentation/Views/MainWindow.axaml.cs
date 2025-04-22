using Avalonia.Controls;
using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.ViewModels;
using ReactiveUI;
using System.Reactive.Disposables;

namespace CTUScheduler.Presentation.Views;

public partial class MainWindow : ReactiveWindow<MainViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
