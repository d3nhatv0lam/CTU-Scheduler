using Avalonia.Controls;
using Avalonia.ReactiveUI;
using CTUScheduler.ViewModels;
using ReactiveUI;
using System.Reactive.Disposables;

namespace CTUScheduler.Views;

public partial class MainWindow : ReactiveWindow<MainViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
