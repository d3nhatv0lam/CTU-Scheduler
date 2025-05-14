using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.ViewModels.CoursePage.AddScheduleTable;
using ReactiveUI;

namespace CTUScheduler.Presentation.Views.CoursePage.AddScheduleTable;

public partial class SelectionView : ReactiveUserControl<SelectionViewModel>
{
    public SelectionView()
    {
        InitializeComponent();

        this.WhenActivated(disposeables =>
        {
            this.BindCommand(ViewModel, vm => vm.HandmadeSelectionCommand, v => v.btnHandmadeSelection);
        });
    }
}