using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using CTUScheduler.ViewModels;

namespace CTUScheduler.Views;

public partial class MainHomeView : ReactiveUserControl<MainHomeViewModel>
{
    public MainHomeView()
    {
        InitializeComponent();
    }
}