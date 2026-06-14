using System.Reactive.Disposables.Fluent;
using ReactiveUI.Avalonia;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels.Steps;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.Views.Steps;

public partial class FindCourseView : ReactiveUserControl<FindCourseViewModel>
{
    public FindCourseView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
        });
    }
}