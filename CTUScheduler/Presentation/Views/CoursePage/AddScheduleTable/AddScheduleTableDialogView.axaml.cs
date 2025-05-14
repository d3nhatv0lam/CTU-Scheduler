using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.ViewModels.CoursePage.AddScheduleTable;
using ReactiveUI;

namespace CTUScheduler.Presentation.Views.CoursePage.AddScheduleTable;

public partial class AddScheduleTableDialogView : ReactiveUserControl<AddScheduleTableDialogViewModel>
{
    public AddScheduleTableDialogView()
    {
        InitializeComponent();

        this.WhenActivated(disposeables =>
        {

        });
    }
}