using Avalonia.Controls;
using System.Reactive.Disposables.Fluent;
using CTUScheduler.Presentation.Shells.AppShell.ViewModels;
using ReactiveUI;
using ReactiveUI.Avalonia;
using Ursa.ReactiveUIExtension;

namespace CTUScheduler.Presentation.Shells.AppShell.Views;

public partial class MainWindow : ReactiveUrsaWindow<MainViewModel>
{
    private bool _isClosingConfirmed;

    public MainWindow()
    {
        InitializeComponent();
        this.WhenActivated(disposables => { });
    }

    public MainWindow(MainViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        if (_isClosingConfirmed)
        {
            base.OnClosing(e);
            return;
        }

        e.Cancel = true;

        if (ViewModel != null && await ViewModel.ConfirmExitAsync())
        {
            _isClosingConfirmed = true;
            Close();
        }
    }
}
