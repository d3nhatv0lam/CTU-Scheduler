using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.ViewModels.HomePage;
using ReactiveUI;
using System.Diagnostics;

namespace CTUScheduler.Presentation.Views.HomePage;

public partial class HomePageView : ReactiveUserControl<HomePageViewModel>
{
    public HomePageView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.RegistrationInfo, v => v.txtRegistrationInfo.Text, info => $"Học kỳ: {info.AcademicYear}\n{info.Semester}");
        });
    }
}