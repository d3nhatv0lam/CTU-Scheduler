using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.Scheduling.ViewModels;
using ReactiveUI;

namespace CTUScheduler.Presentation.Scheduling.Views;

public partial class HandmadeFindCourseView : ReactiveUserControl<HandmadeFindCourseViewModel>
{
    public HandmadeFindCourseView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.OneWayBind(this.ViewModel, vm => vm.SearchedCourse, v => v.txtCourseInfo.Text, 
                (course) => course == null ? "Danh mục học phần" : $"Danh mục học phần: {course.Name_VN} (Tín chỉ: {course.Credit})")
                .DisposeWith(disposables);
        });
    }
}