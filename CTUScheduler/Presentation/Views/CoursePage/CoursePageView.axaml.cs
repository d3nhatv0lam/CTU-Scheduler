using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.ViewModels.CoursePage;

namespace CTUScheduler.Presentation.Views.CoursePage;

public partial class CoursePageView : ReactiveUserControl<CoursePageViewModel>
{
    public CoursePageView()
    {
        InitializeComponent();
    }
}