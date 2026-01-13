using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CTUScheduler.Presentation.Features.SplashScreen.ViewModels;
using Ursa.ReactiveUIExtension;

namespace CTUScheduler.Presentation.Features.SplashScreen.Views;

public partial class TestSplashWindow : ReactiveUrsaWindow<TestSpashWindowViewModel>
{
    public TestSplashWindow()
    {
        InitializeComponent();
    }
}