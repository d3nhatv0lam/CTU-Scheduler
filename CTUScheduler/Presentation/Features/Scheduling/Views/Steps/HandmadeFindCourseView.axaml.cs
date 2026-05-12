using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using Avalonia;
using Avalonia.Input;
using ReactiveUI.Avalonia;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels.Steps;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.Views.Steps;

public partial class HandmadeFindCourseView : ReactiveUserControl<FindCourseViewModel>
{
    public HandmadeFindCourseView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.OneWayBind(this.ViewModel, vm => vm.SearchedCourse, v => v.txtCourseInfo.Text, 
                (course) => course == null ? "Danh mục học phần" : $"Danh mục học phần: {course.Name_VN} (Tín chỉ: {course.Credits})")
                .DisposeWith(disposables);
        });
    }
}