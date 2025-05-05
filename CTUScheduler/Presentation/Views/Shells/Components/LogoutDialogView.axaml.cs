using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.ViewModels.Shells.Components;

namespace CTUScheduler.Presentation.Views.Shells.Components;

public partial class LogoutDialogView : ReactiveUserControl<LogoutDialogViewModel>
{
    public LogoutDialogView()
    {
        InitializeComponent();
    }
}