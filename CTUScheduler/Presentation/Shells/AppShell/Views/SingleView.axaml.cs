using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CTUScheduler.Presentation.Shells.AppShell.ViewModels;
using Ursa.ReactiveUIExtension;

namespace CTUScheduler.Presentation.Shells.AppShell.Views;

public partial class SingleView : ReactiveUrsaView<MainViewModel>
{
    public SingleView()
    {
        InitializeComponent();
    }
}