using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;
using CTUScheduler.Presentation.ViewModels.HomePage;
using ReactiveUI;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Text.RegularExpressions;

namespace CTUScheduler.Presentation.Views.HomePage;

public partial class HomePageView : ReactiveUserControl<HomePageViewModel>
{
    public HomePageView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.RegistrationInfo, v => v.txtRegistrationInfo.Text, info => info == null ? 
                            "Không lấy được thông tin!" : 
                            $"Học kỳ: {info.Semester ?? "?"}\n{info.AcademicYear}-{info.AcademicYear+1}\nSố tín chỉ tối đa: {info.MaxCreditPerSemester}").DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.RegistrationInfo, v => v.txtRegistrationTime.Text, info => info == null ?
                            "Không lấy được thông tin!" :
                            info.Period).DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.RegistrationInfo.UserPeriod, v => v.dtgridUserRegistrationTime.ItemsSource, userRegistrationTime => userRegistrationTime ?? Enumerable.Empty<PeriodItem>()).DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.RegistrationInfo.Groups, v => v.dtgridGroupList.ItemsSource, groups => groups ?? Enumerable.Empty<GroupItem>()).DisposeWith(disposables);
        });
    }
}