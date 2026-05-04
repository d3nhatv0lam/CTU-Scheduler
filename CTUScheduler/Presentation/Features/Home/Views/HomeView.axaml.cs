using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables.Fluent;
using Avalonia.Controls;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration;
using ReactiveUI.Avalonia;
using CTUScheduler.Presentation.Features.Home.ViewModels;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Home.Views;

public partial class HomeView : ReactiveUserControl<HomeViewModel>
{
    public HomeView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.RegistrationInfo, v => v.txtRegistrationInfo.Text,
                    info => info == null
                        ? "Không lấy được thông tin!"
                        : $"Học kỳ: {info.Semester ?? "?"}\n{info.AcademicYear}-{info.AcademicYear + 1}\nSố tín chỉ tối đa: {info.MaxCreditPerSemester}")
                .DisposeWith(disposables);

            this.OneWayBind<HomeViewModel, HomeView, List<GroupItem>, IEnumerable>(ViewModel,
                vm => vm.RegistrationInfo.Groups, v => v.dtgridGroupList.ItemsSource,
                groups => groups ?? Enumerable.Empty<GroupItem>()).DisposeWith(disposables);
        });
    }
}