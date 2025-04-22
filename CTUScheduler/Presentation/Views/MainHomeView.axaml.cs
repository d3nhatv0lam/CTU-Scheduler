using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.ViewModels;

namespace CTUScheduler.Presentation.Views;

public partial class MainHomeView : ReactiveUserControl<MainHomeViewModel>
{
    public MainHomeView()
    {
        InitializeComponent();
    }
}