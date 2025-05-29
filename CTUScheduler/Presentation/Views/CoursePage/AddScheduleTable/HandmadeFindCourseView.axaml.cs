using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.ViewModels.CoursePage.AddScheduleTable;
using ReactiveUI;
using System.Diagnostics;
using System.Reactive.Disposables;

namespace CTUScheduler.Presentation.Views.CoursePage.AddScheduleTable;

public partial class HandmadeFindCourseView : ReactiveUserControl<HandmadeFindCourseViewModel>
{
    public HandmadeFindCourseView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(this.ViewModel, vm => vm.Course, v => v.txtCourseInfo.Text, 
                (course) => course == null ? "Danh mục học phần" : $"Danh mục học phần: {course.Name_VN} (Tín chỉ: {course.Credit})")
                .DisposeWith(disposables);
        });
    }
}