using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.Shells.MainShell.ViewModels;

namespace CTUScheduler.Presentation.Shells.MainShell.Views;

public partial class LogoutDialogView : ReactiveUserControl<LogoutDialogViewModel>
{
    public LogoutDialogView()
    {
        InitializeComponent();
    }
}