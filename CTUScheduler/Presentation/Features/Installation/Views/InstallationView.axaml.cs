using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI.Avalonia;
using CTUScheduler.Presentation.Features.Installation.ViewModels;

namespace CTUScheduler.Presentation.Features.Installation.Views;

public partial class InstallationView : ReactiveWindow<InstallationViewModel>
{
    public InstallationView()
    {
        InitializeComponent();
    }
}