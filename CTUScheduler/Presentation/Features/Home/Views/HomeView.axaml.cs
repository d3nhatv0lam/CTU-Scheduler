using System.Reactive.Disposables.Fluent;
using ReactiveUI.Avalonia;
using CTUScheduler.Presentation.Features.Home.ViewModels;
using ReactiveUI;
using CTUScheduler.Presentation.Shared;

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
                        ? UiText.FetchFailed
                        : $"Học kỳ: {info.Semester ?? "---"}\nNăm học: {(info.AcademicYear?.ToString() ?? UiText.Unknown)}-{((info.AcademicYear ?? 0) != 0 ? (info.AcademicYear + 1).ToString() : UiText.Unknown)}\nSố tín chỉ tối đa: {info.MaxCreditPerSemester?.ToString() ?? UiText.Unknown}")
                .DisposeWith(disposables);

        });
    }
}